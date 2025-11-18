--
-- Migration: Accounting sync preferences
-- Date: 2025-03-06
-- Description: Adds accounting_sync_preferences table to store automation options for QuickBooks integrations
--
CREATE TABLE IF NOT EXISTS public.accounting_sync_preferences (
    id BIGSERIAL PRIMARY KEY,
    company_id INTEGER NOT NULL,
    provider TEXT NOT NULL,
    auto_sync_invoices BOOLEAN NOT NULL DEFAULT FALSE,
    auto_sync_cogs BOOLEAN NOT NULL DEFAULT FALSE,
    sync_frequency TEXT NOT NULL DEFAULT 'Manual',
    last_sync_at TIMESTAMPTZ NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_accounting_sync_preferences_company
        FOREIGN KEY (company_id) REFERENCES public.company(id)
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_accounting_sync_preferences_company_provider
    ON public.accounting_sync_preferences(company_id, provider);

CREATE INDEX IF NOT EXISTS idx_accounting_sync_preferences_company_id
    ON public.accounting_sync_preferences(company_id);
