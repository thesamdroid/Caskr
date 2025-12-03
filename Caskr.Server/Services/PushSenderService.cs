using Caskr.server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Caskr.Server.Services;

/// <summary>
/// Payload for a push notification
/// </summary>
public class PushNotificationPayload
{
    public string Title { get; set; } = "Caskr";
    public string Body { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Badge { get; set; }
    public string? Tag { get; set; }
    public NotificationType Type { get; set; }
    public long? EntityId { get; set; }
    public string? Url { get; set; }
    public Dictionary<string, string>? Data { get; set; }
    public List<NotificationAction>? Actions { get; set; }
}

/// <summary>
/// Action button for a notification
/// </summary>
public class NotificationAction
{
    public string Action { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Icon { get; set; }
}

/// <summary>
/// Service for sending push notifications
/// </summary>
public interface IPushSenderService
{
    /// <summary>
    /// Send a notification to a specific user
    /// </summary>
    Task<int> SendToUserAsync(int userId, PushNotificationPayload payload);

    /// <summary>
    /// Send a notification to multiple users
    /// </summary>
    Task<int> SendToUsersAsync(IEnumerable<int> userIds, PushNotificationPayload payload);

    /// <summary>
    /// Send a notification to a specific subscription
    /// </summary>
    Task<bool> SendToSubscriptionAsync(PushSubscription subscription, PushNotificationPayload payload);

    /// <summary>
    /// Send a test notification
    /// </summary>
    Task<bool> SendTestNotificationAsync(int userId);
}

public class PushSenderService : IPushSenderService
{
    private readonly CaskrDbContext _context;
    private readonly IPushNotificationService _pushNotificationService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PushSenderService> _logger;
    private readonly HttpClient _httpClient;

    private readonly string _vapidSubject;
    private readonly string _vapidPublicKey;
    private readonly string _vapidPrivateKey;

    public PushSenderService(
        CaskrDbContext context,
        IPushNotificationService pushNotificationService,
        IConfiguration configuration,
        ILogger<PushSenderService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _pushNotificationService = pushNotificationService;
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("PushService");

        _vapidSubject = configuration["PushNotifications:VapidSubject"]
            ?? "mailto:admin@caskr.co";
        _vapidPublicKey = configuration["PushNotifications:VapidPublicKey"]
            ?? string.Empty;
        _vapidPrivateKey = configuration["PushNotifications:VapidPrivateKey"]
            ?? string.Empty;
    }

    public async Task<int> SendToUserAsync(int userId, PushNotificationPayload payload)
    {
        // Check if user should receive this notification
        if (!await _pushNotificationService.ShouldNotifyUserAsync(userId, payload.Type))
        {
            _logger.LogDebug(
                "Skipping notification for user {UserId} based on preferences",
                userId);
            return 0;
        }

        var subscriptions = await _pushNotificationService.GetUserSubscriptionsAsync(userId);

        if (subscriptions.Count == 0)
        {
            _logger.LogDebug("No active subscriptions for user {UserId}", userId);
            return 0;
        }

        var sentCount = 0;
        foreach (var subscription in subscriptions)
        {
            try
            {
                var success = await SendToSubscriptionAsync(subscription, payload);
                if (success)
                {
                    sentCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to send notification to subscription {SubscriptionId}",
                    subscription.Id);
            }
        }

        _logger.LogInformation(
            "Sent {Count}/{Total} notifications to user {UserId}",
            sentCount, subscriptions.Count, userId);

        return sentCount;
    }

    public async Task<int> SendToUsersAsync(IEnumerable<int> userIds, PushNotificationPayload payload)
    {
        var totalSent = 0;

        foreach (var userId in userIds)
        {
            totalSent += await SendToUserAsync(userId, payload);
        }

        return totalSent;
    }

    public async Task<bool> SendToSubscriptionAsync(
        PushSubscription subscription,
        PushNotificationPayload payload)
    {
        if (string.IsNullOrEmpty(_vapidPrivateKey))
        {
            _logger.LogWarning("VAPID keys not configured, skipping push notification");
            return false;
        }

        try
        {
            // Prepare the payload
            var payloadJson = JsonSerializer.Serialize(new
            {
                title = payload.Title,
                body = payload.Body,
                icon = payload.Icon ?? "/icons/icon-192x192.png",
                badge = payload.Badge ?? "/icons/icon-72x72.png",
                tag = payload.Tag,
                data = new
                {
                    type = payload.Type.ToString(),
                    entityId = payload.EntityId,
                    url = payload.Url,
                    customData = payload.Data
                },
                actions = payload.Actions?.ConvertAll(a => new
                {
                    action = a.Action,
                    title = a.Title,
                    icon = a.Icon
                })
            });

            var payloadBytes = Encoding.UTF8.GetBytes(payloadJson);

            // Encrypt the payload using Web Push encryption
            var encryptedPayload = EncryptPayload(
                payloadBytes,
                subscription.P256dhKey,
                subscription.AuthKey);

            // Create VAPID headers
            var vapidHeaders = CreateVapidHeaders(subscription.Endpoint);

            // Send the request
            using var request = new HttpRequestMessage(HttpMethod.Post, subscription.Endpoint);
            request.Headers.Add("TTL", "86400"); // 24 hours
            request.Headers.Add("Urgency", "normal");

            foreach (var header in vapidHeaders)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            request.Content = new ByteArrayContent(encryptedPayload);
            request.Content.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            request.Content.Headers.Add("Content-Encoding", "aes128gcm");

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Created)
            {
                await _pushNotificationService.MarkSubscriptionUsedAsync(subscription.Id);
                return true;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Gone)
            {
                // Subscription is no longer valid
                _logger.LogInformation(
                    "Subscription {SubscriptionId} returned 410 Gone, deactivating",
                    subscription.Id);
                await _pushNotificationService.DeactivateSubscriptionAsync(subscription.Id);
                return false;
            }
            else
            {
                _logger.LogWarning(
                    "Push notification to {SubscriptionId} failed with status {StatusCode}",
                    subscription.Id, response.StatusCode);
                await _pushNotificationService.MarkSubscriptionFailedAsync(subscription.Id);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Exception sending push notification to subscription {SubscriptionId}",
                subscription.Id);
            await _pushNotificationService.MarkSubscriptionFailedAsync(subscription.Id);
            return false;
        }
    }

    public async Task<bool> SendTestNotificationAsync(int userId)
    {
        var payload = new PushNotificationPayload
        {
            Title = "Test Notification",
            Body = "If you see this, push notifications are working!",
            Type = NotificationType.General,
            Tag = "test",
            Url = "/"
        };

        var sent = await SendToUserAsync(userId, payload);
        return sent > 0;
    }

    /// <summary>
    /// Encrypt payload using Web Push encryption (aes128gcm)
    /// </summary>
    private byte[] EncryptPayload(byte[] payload, string p256dhKey, string authKey)
    {
        // Note: In production, you would use a proper Web Push library like WebPush.Net
        // This is a simplified placeholder that shows the structure

        try
        {
            // Decode the keys from Base64URL
            var userPublicKey = Base64UrlDecode(p256dhKey);
            var userAuth = Base64UrlDecode(authKey);

            // For proper implementation, use ECDiffieHellman for key agreement
            // and AES-GCM for encryption per Web Push spec

            // Placeholder: For now, we'll use a simple approach
            // In production, integrate WebPush.Net library for proper encryption
            using var aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            // Generate a random IV
            var iv = new byte[16];
            RandomNumberGenerator.Fill(iv);

            // Derive a key (simplified - use proper HKDF in production)
            using var sha256 = SHA256.Create();
            var keyMaterial = sha256.ComputeHash(userAuth.Concat(userPublicKey).ToArray());

            aes.Key = keyMaterial.Take(16).ToArray();
            aes.IV = iv;

            using var encryptor = aes.CreateEncryptor();
            var encrypted = encryptor.TransformFinalBlock(payload, 0, payload.Length);

            // Combine IV and encrypted data
            var result = new byte[iv.Length + encrypted.Length];
            Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
            Buffer.BlockCopy(encrypted, 0, result, iv.Length, encrypted.Length);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt push payload");
            throw;
        }
    }

    /// <summary>
    /// Create VAPID authorization headers
    /// </summary>
    private Dictionary<string, string> CreateVapidHeaders(string endpoint)
    {
        // Note: In production, use a proper Web Push library
        // This is a simplified placeholder

        var audience = new Uri(endpoint).GetLeftPart(UriPartial.Authority);
        var expiration = DateTimeOffset.UtcNow.AddHours(12).ToUnixTimeSeconds();

        // JWT claim
        var header = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(new
        {
            typ = "JWT",
            alg = "ES256"
        }));

        var payload = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(new
        {
            aud = audience,
            exp = expiration,
            sub = _vapidSubject
        }));

        // In production, sign with ES256 using the private key
        // For now, create a placeholder signature
        var signature = Base64UrlEncode(new byte[64]);

        var jwt = $"{header}.{payload}.{signature}";

        return new Dictionary<string, string>
        {
            { "Authorization", $"vapid t={jwt}, k={_vapidPublicKey}" }
        };
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var output = input
            .Replace('-', '+')
            .Replace('_', '/');

        switch (output.Length % 4)
        {
            case 2: output += "=="; break;
            case 3: output += "="; break;
        }

        return Convert.FromBase64String(output);
    }

    private static string Base64UrlEncode(byte[] input)
    {
        return Convert.ToBase64String(input)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
