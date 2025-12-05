using System.Net.Http.Headers;
using System.Text;
using Caskr.server.Models;
using Caskr.server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Caskr.Server.Services.BackgroundJobs;

/// <summary>
/// Background service that processes pending webhook deliveries with retry logic.
/// </summary>
public sealed class WebhookDeliveryWorker : IHostedService, IDisposable
{
    private const int MaxRetryCount = 5;
    private const int BatchSize = 100;
    private const int HttpTimeoutSeconds = 10;
    private const int MaxResponseBodyLength = 1000;

    // Retry delays in minutes: 1, 5, 15, 60, 360 (6 hours)
    private static readonly int[] RetryDelayMinutes = [1, 5, 15, 60, 360];
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(30);

    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WebhookDeliveryWorker> _logger;
    private CancellationTokenSource? _stoppingCts;
    private Task? _executingTask;

    public WebhookDeliveryWorker(
        IServiceScopeFactory serviceScopeFactory,
        IHttpClientFactory httpClientFactory,
        ILogger<WebhookDeliveryWorker> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Webhook delivery worker starting.");
        _stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _executingTask = ExecuteAsync(_stoppingCts.Token);
        return _executingTask.IsCompleted ? _executingTask : Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_executingTask is null)
        {
            return;
        }

        _logger.LogInformation("Webhook delivery worker stopping.");
        _stoppingCts?.Cancel();

        await Task.WhenAny(_executingTask, Task.Delay(Timeout.Infinite, cancellationToken));
    }

    public void Dispose()
    {
        _stoppingCts?.Cancel();
        _stoppingCts?.Dispose();
    }

    private async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingDeliveriesAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in webhook delivery worker.");
            }

            try
            {
                await Task.Delay(PollingInterval, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task ProcessPendingDeliveriesAsync(CancellationToken cancellationToken)
    {
        await using var scope = _serviceScopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CaskrDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var now = DateTime.UtcNow;

        // Query pending and retrying deliveries that are due for processing
        var pendingDeliveries = await dbContext.WebhookDeliveries
            .Include(d => d.Subscription)
                .ThenInclude(s => s.CreatedByUser)
            .Where(d => (d.DeliveryStatus == WebhookDeliveryStatus.Pending ||
                        d.DeliveryStatus == WebhookDeliveryStatus.Retrying) &&
                       (d.NextRetryAt == null || d.NextRetryAt <= now))
            .OrderBy(d => d.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (pendingDeliveries.Count == 0)
        {
            return;
        }

        _logger.LogInformation("Processing {Count} pending webhook deliveries.", pendingDeliveries.Count);

        foreach (var delivery in pendingDeliveries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Skip if subscription is no longer active
            if (!delivery.Subscription.IsActive)
            {
                delivery.DeliveryStatus = WebhookDeliveryStatus.Failed;
                delivery.ResponseBody = "Subscription is no longer active";
                await dbContext.SaveChangesAsync(cancellationToken);
                continue;
            }

            await ProcessDeliveryAsync(delivery, dbContext, emailService, cancellationToken);
        }
    }

    private async Task ProcessDeliveryAsync(
        WebhookDelivery delivery,
        CaskrDbContext dbContext,
        IEmailService emailService,
        CancellationToken cancellationToken)
    {
        var subscription = delivery.Subscription;
        var startTime = DateTime.UtcNow;

        try
        {
            // Calculate signature
            var signature = WebhookService.CalculateSignature(delivery.Payload, subscription.SecretKey);

            // Create HTTP request
            using var httpClient = _httpClientFactory.CreateClient("WebhookClient");
            httpClient.Timeout = TimeSpan.FromSeconds(HttpTimeoutSeconds);

            using var request = new HttpRequestMessage(HttpMethod.Post, subscription.TargetUrl);
            request.Content = new StringContent(delivery.Payload, Encoding.UTF8, "application/json");
            request.Headers.Add("X-Caskr-Signature", signature);
            request.Headers.Add("X-Caskr-Event", delivery.EventType);
            request.Headers.Add("X-Caskr-Delivery-Id", delivery.Id.ToString());
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("Caskr-Webhooks", "1.0"));

            // Send the request
            using var response = await httpClient.SendAsync(request, cancellationToken);

            var statusCode = (int)response.StatusCode;
            delivery.HttpStatusCode = statusCode;

            // Read response body (truncated)
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            delivery.ResponseBody = responseBody.Length > MaxResponseBodyLength
                ? responseBody[..MaxResponseBodyLength] + "...[truncated]"
                : responseBody;

            if (response.IsSuccessStatusCode)
            {
                // Success!
                delivery.DeliveryStatus = WebhookDeliveryStatus.Success;
                delivery.DeliveredAt = DateTime.UtcNow;
                delivery.NextRetryAt = null;

                _logger.LogInformation(
                    "Webhook delivery {DeliveryId} to {Url} succeeded with status {StatusCode} in {Duration}ms",
                    delivery.Id,
                    subscription.TargetUrl,
                    statusCode,
                    (DateTime.UtcNow - startTime).TotalMilliseconds);
            }
            else
            {
                // Failure - schedule retry or mark as failed
                await HandleFailureAsync(delivery, $"HTTP {statusCode}: {delivery.ResponseBody}", emailService);

                _logger.LogWarning(
                    "Webhook delivery {DeliveryId} to {Url} failed with status {StatusCode}. Retry {RetryCount}/{MaxRetries}",
                    delivery.Id,
                    subscription.TargetUrl,
                    statusCode,
                    delivery.RetryCount,
                    MaxRetryCount);
            }
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            await HandleFailureAsync(delivery, $"Request timed out after {HttpTimeoutSeconds} seconds", emailService);

            _logger.LogWarning(
                "Webhook delivery {DeliveryId} to {Url} timed out. Retry {RetryCount}/{MaxRetries}",
                delivery.Id,
                subscription.TargetUrl,
                delivery.RetryCount,
                MaxRetryCount);
        }
        catch (HttpRequestException ex)
        {
            await HandleFailureAsync(delivery, $"Network error: {ex.Message}", emailService);

            _logger.LogWarning(
                ex,
                "Webhook delivery {DeliveryId} to {Url} failed with network error. Retry {RetryCount}/{MaxRetries}",
                delivery.Id,
                subscription.TargetUrl,
                delivery.RetryCount,
                MaxRetryCount);
        }
        catch (Exception ex)
        {
            await HandleFailureAsync(delivery, $"Unexpected error: {ex.Message}", emailService);

            _logger.LogError(
                ex,
                "Webhook delivery {DeliveryId} to {Url} failed with unexpected error. Retry {RetryCount}/{MaxRetries}",
                delivery.Id,
                subscription.TargetUrl,
                delivery.RetryCount,
                MaxRetryCount);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task HandleFailureAsync(WebhookDelivery delivery, string errorMessage, IEmailService emailService)
    {
        delivery.RetryCount++;
        delivery.ResponseBody = errorMessage;

        if (delivery.RetryCount >= MaxRetryCount)
        {
            // Max retries exceeded - mark as permanently failed
            delivery.DeliveryStatus = WebhookDeliveryStatus.Failed;
            delivery.NextRetryAt = null;

            _logger.LogError(
                "Webhook delivery {DeliveryId} permanently failed after {MaxRetries} retries. " +
                "Subscription {SubscriptionId} may need attention.",
                delivery.Id,
                MaxRetryCount,
                delivery.SubscriptionId);

            // Send alert email to subscription creator
            await SendFailureAlertEmailAsync(delivery, errorMessage, emailService);
        }
        else
        {
            // Schedule retry with exponential backoff
            delivery.DeliveryStatus = WebhookDeliveryStatus.Retrying;
            var delayMinutes = RetryDelayMinutes[Math.Min(delivery.RetryCount - 1, RetryDelayMinutes.Length - 1)];
            delivery.NextRetryAt = DateTime.UtcNow.AddMinutes(delayMinutes);

            _logger.LogInformation(
                "Webhook delivery {DeliveryId} scheduled for retry {RetryCount} at {NextRetryAt}",
                delivery.Id,
                delivery.RetryCount,
                delivery.NextRetryAt);
        }
    }

    private async Task SendFailureAlertEmailAsync(WebhookDelivery delivery, string errorMessage, IEmailService emailService)
    {
        var subscription = delivery.Subscription;
        var creatorEmail = subscription.CreatedByUser?.Email;

        if (string.IsNullOrEmpty(creatorEmail))
        {
            _logger.LogWarning(
                "Cannot send failure alert for webhook delivery {DeliveryId}: no creator email available",
                delivery.Id);
            return;
        }

        var subject = $"Webhook Delivery Failed: {subscription.Name}";
        var body = $@"Your webhook subscription ""{subscription.Name}"" has permanently failed after {MaxRetryCount} delivery attempts.

Subscription Details:
- Name: {subscription.Name}
- Target URL: {subscription.TargetUrl}
- Event Type: {delivery.EventType}

Last Error: {errorMessage}

Please check your webhook endpoint and update the subscription if necessary.

This is an automated message from Caskr.";

        try
        {
            await emailService.SendEmailAsync(creatorEmail, subject, body);
            _logger.LogInformation(
                "Sent failure alert email to {Email} for webhook delivery {DeliveryId}",
                creatorEmail,
                delivery.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send failure alert email to {Email} for webhook delivery {DeliveryId}",
                creatorEmail,
                delivery.Id);
        }
    }
}
