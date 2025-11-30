--
-- Migration: Webhook subscriptions and delivery tracking
-- Date: 2025-11-30
-- Description: Adds webhook_subscriptions and webhook_deliveries tables for real-time event notifications to external systems
--

-- Webhook subscriptions table
CREATE TABLE IF NOT EXISTS public.webhook_subscriptions (
    id BIGSERIAL PRIMARY KEY,
    company_id INTEGER NOT NULL,
    name VARCHAR(200) NOT NULL,
    target_url VARCHAR(500) NOT NULL,
    event_types JSONB NOT NULL DEFAULT '[]',
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    secret_key VARCHAR(100) NOT NULL,
    created_by_user_id INTEGER NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_webhook_subscriptions_company
        FOREIGN KEY (company_id) REFERENCES public.company(id),
    CONSTRAINT fk_webhook_subscriptions_created_by
        FOREIGN KEY (created_by_user_id) REFERENCES public.users(id)
);

-- Indexes for webhook_subscriptions
CREATE INDEX IF NOT EXISTS idx_webhook_subscriptions_company_id
    ON public.webhook_subscriptions(company_id);

CREATE INDEX IF NOT EXISTS idx_webhook_subscriptions_is_active
    ON public.webhook_subscriptions(is_active);

CREATE INDEX IF NOT EXISTS idx_webhook_subscriptions_created_by
    ON public.webhook_subscriptions(created_by_user_id);

-- GIN index for efficient JSONB array containment queries
CREATE INDEX IF NOT EXISTS idx_webhook_subscriptions_event_types
    ON public.webhook_subscriptions USING GIN (event_types);

-- Webhook deliveries table
CREATE TABLE IF NOT EXISTS public.webhook_deliveries (
    id BIGSERIAL PRIMARY KEY,
    subscription_id BIGINT NOT NULL,
    event_type VARCHAR(100) NOT NULL,
    event_id INTEGER NOT NULL,
    payload JSONB NOT NULL,
    delivery_status VARCHAR(20) NOT NULL DEFAULT 'Pending',
    http_status_code INTEGER,
    response_body TEXT,
    retry_count INTEGER NOT NULL DEFAULT 0,
    next_retry_at TIMESTAMPTZ,
    delivered_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_webhook_deliveries_subscription
        FOREIGN KEY (subscription_id) REFERENCES public.webhook_subscriptions(id) ON DELETE CASCADE,
    CONSTRAINT chk_webhook_deliveries_status
        CHECK (delivery_status IN ('Pending', 'Success', 'Failed', 'Retrying'))
);

-- Indexes for webhook_deliveries
CREATE INDEX IF NOT EXISTS idx_webhook_deliveries_subscription_id
    ON public.webhook_deliveries(subscription_id);

CREATE INDEX IF NOT EXISTS idx_webhook_deliveries_status
    ON public.webhook_deliveries(delivery_status);

CREATE INDEX IF NOT EXISTS idx_webhook_deliveries_next_retry
    ON public.webhook_deliveries(next_retry_at)
    WHERE delivery_status IN ('Pending', 'Retrying');

CREATE INDEX IF NOT EXISTS idx_webhook_deliveries_created_at
    ON public.webhook_deliveries(created_at);

-- Composite index for the worker query pattern
CREATE INDEX IF NOT EXISTS idx_webhook_deliveries_worker_query
    ON public.webhook_deliveries(delivery_status, next_retry_at, created_at)
    WHERE delivery_status IN ('Pending', 'Retrying');

-- Comment on tables for documentation
COMMENT ON TABLE public.webhook_subscriptions IS 'Stores webhook endpoint configurations for real-time event notifications';
COMMENT ON TABLE public.webhook_deliveries IS 'Logs all webhook delivery attempts for audit and retry purposes';

COMMENT ON COLUMN public.webhook_subscriptions.event_types IS 'JSONB array of event types this subscription listens to (e.g., ["barrel.created", "order.completed"])';
COMMENT ON COLUMN public.webhook_subscriptions.secret_key IS 'HMAC-SHA256 secret for signing webhook payloads';
COMMENT ON COLUMN public.webhook_deliveries.delivery_status IS 'Pending: awaiting delivery, Success: delivered, Failed: max retries exceeded, Retrying: scheduled for retry';
COMMENT ON COLUMN public.webhook_deliveries.next_retry_at IS 'Scheduled time for next delivery attempt (NULL for Pending/Success/Failed final states)';
