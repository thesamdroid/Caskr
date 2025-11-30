# Webhook System Design

## Overview

The Caskr Webhook System enables real-time notifications to external systems when significant events occur within the platform. This allows customers to integrate Caskr with their own systems, triggering automated workflows, syncing data to analytics platforms, or sending notifications to communication tools like Slack.

## Architecture

### Database Schema

#### webhook_subscriptions Table
Stores webhook endpoint configurations for each company.

| Column | Type | Description |
|--------|------|-------------|
| id | BIGSERIAL | Primary key |
| company_id | INTEGER | FK to company table |
| name | VARCHAR(200) | Human-readable subscription name |
| target_url | VARCHAR(500) | HTTPS endpoint to receive webhooks |
| event_types | JSONB | Array of subscribed event types |
| is_active | BOOLEAN | Whether subscription is enabled |
| secret_key | VARCHAR(100) | HMAC secret for signature verification |
| created_by_user_id | INTEGER | FK to users table |
| created_at | TIMESTAMPTZ | Creation timestamp |
| updated_at | TIMESTAMPTZ | Last update timestamp |

#### webhook_deliveries Table
Logs all webhook delivery attempts for audit and retry purposes.

| Column | Type | Description |
|--------|------|-------------|
| id | BIGSERIAL | Primary key |
| subscription_id | BIGINT | FK to webhook_subscriptions |
| event_type | VARCHAR(100) | Type of event being delivered |
| event_id | INTEGER | ID of the entity that triggered the event |
| payload | JSONB | Full webhook payload |
| delivery_status | VARCHAR(20) | Pending/Success/Failed/Retrying |
| http_status_code | INTEGER | Response status code |
| response_body | TEXT | Response body (truncated if large) |
| retry_count | INTEGER | Number of retry attempts |
| next_retry_at | TIMESTAMPTZ | Scheduled time for next retry |
| delivered_at | TIMESTAMPTZ | Successful delivery timestamp |
| created_at | TIMESTAMPTZ | Creation timestamp |

### Event Types

The following events can trigger webhook notifications:

| Event Type | Description | Trigger Point |
|------------|-------------|---------------|
| `barrel.created` | New barrel added to inventory | BarrelsService.AddBarrelAsync |
| `barrel.updated` | Barrel details modified | BarrelsService.UpdateBarrelAsync |
| `barrel.deleted` | Barrel removed from system | BarrelsService.DeleteBarrelAsync |
| `barrel.moved` | Barrel relocated to new rickhouse | BarrelsService.MoveBarrelAsync |
| `batch.created` | New batch started | BatchesService.CreateBatchAsync |
| `batch.completed` | Batch production finished | BatchesService.CompleteBatchAsync |
| `order.created` | New order placed | OrdersService.AddOrderAsync |
| `order.completed` | Order fulfilled | OrdersService.CompleteOrderAsync |
| `task.created` | New task assigned | TasksService.CreateTaskAsync |
| `task.completed` | Task marked complete | TasksService.CompleteTaskAsync |
| `transfer.created` | Transfer initiated | TransfersService.CreateTransferAsync |
| `ttb_report.submitted` | TTB report submitted | TtbReportService.SubmitReportAsync |

### Webhook Payload Format

All webhooks follow a standardized JSON structure:

```json
{
  "event_type": "barrel.created",
  "event_id": 1234,
  "timestamp": "2024-11-14T10:30:00Z",
  "data": {
    "id": 1234,
    "sku": "BAR-001",
    "status": "Filled",
    "rickhouse_id": 5,
    "batch_id": 42,
    "company_id": 1
  },
  "company_id": 1
}
```

### Security

#### HMAC Signature
Each webhook delivery includes a cryptographic signature for verification:

1. **Secret Generation**: When a subscription is created, a random 32-byte secret key is generated
2. **Signature Calculation**: Payload is signed using HMAC-SHA256 with the secret
3. **Header Inclusion**: Signature included in HTTP header: `X-Caskr-Signature: sha256=<hex-encoded-signature>`

#### Signature Verification (Receiver Side)
```csharp
// Example verification code for webhook receivers
public bool VerifySignature(string payload, string signature, string secret)
{
    using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
    var expectedSignature = "sha256=" + Convert.ToHexString(
        hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))
    ).ToLower();
    return signature == expectedSignature;
}
```

### Retry Logic

Failed deliveries are retried with exponential backoff:

| Retry | Delay | Cumulative Time |
|-------|-------|-----------------|
| 1 | 1 minute | 1 minute |
| 2 | 5 minutes | 6 minutes |
| 3 | 15 minutes | 21 minutes |
| 4 | 1 hour | 1 hour 21 minutes |
| 5 | 6 hours | 7 hours 21 minutes |

After 5 failed retries:
- Delivery marked as `Failed`
- Alert email sent to subscription creator
- Subscription remains active for future events

### Rate Limiting

To prevent abuse and protect target endpoints:
- Maximum 100 webhooks per subscription per minute
- Excess deliveries queued for later processing
- Rate limit tracked per subscription, not globally

## Service Architecture

### IWebhookService Interface
```csharp
public interface IWebhookService
{
    Task TriggerEventAsync(string eventType, int eventId, object eventData, int companyId);
    Task<WebhookSubscription> CreateSubscriptionAsync(int companyId, WebhookSubscriptionRequest request, int userId);
    Task DeactivateSubscriptionAsync(int subscriptionId);
    Task<IEnumerable<WebhookSubscription>> GetSubscriptionsAsync(int companyId);
}
```

### WebhookDeliveryWorker
Background service that:
1. Polls for pending/retrying deliveries every 30 seconds
2. Processes up to 100 deliveries per batch
3. Executes HTTP POST with timeout of 10 seconds
4. Updates delivery status based on response
5. Calculates next retry time for failures

### Domain Event Integration
Webhook triggers are integrated into existing services:

```csharp
// Example: BarrelsService after barrel creation
public async Task<Barrel> AddBarrelAsync(Barrel barrel)
{
    var created = await _barrelsRepository.AddAsync(barrel);
    await _webhookService.TriggerEventAsync(
        WebhookEventTypes.BarrelCreated,
        created.Id,
        created,
        created.CompanyId
    );
    return created;
}
```

## HTTP Client Configuration

- **Timeout**: 10 seconds per request
- **Retry Policy**: Polly library for transient failure handling
- **Headers**:
  - `Content-Type: application/json`
  - `X-Caskr-Signature: sha256=<signature>`
  - `User-Agent: Caskr-Webhooks/1.0`

## Testing

Use [webhook.site](https://webhook.site) for easy testing:
1. Get a unique URL from webhook.site
2. Create a subscription with that URL
3. Trigger events in Caskr
4. View received payloads on webhook.site

## Monitoring & Logging

All webhook operations are logged:
- Subscription creation/modification
- Delivery attempts (success/failure)
- Retry scheduling
- Rate limit hits

Log format includes:
- Subscription ID
- Event type
- HTTP status code
- Response time
- Error details (if applicable)

## Future Enhancements

- Webhook management UI
- Bulk retry for failed deliveries
- Webhook analytics dashboard
- Custom header support
- Webhook templating for payload transformation
