--
-- Migration: QuickBooks Online and accounting integration tables
-- Date: 2025-02-18
-- Description: Adds accounting_integrations, accounting_sync_logs, and chart_of_accounts_mapping tables with indexes for QuickBooks Online integration
--

CREATE TABLE IF NOT EXISTS public.accounting_integrations (
    id BIGSERIAL PRIMARY KEY,
    company_id INTEGER NOT NULL,
    provider TEXT NOT NULL,
    access_token_encrypted TEXT,
    refresh_token_encrypted TEXT,
    realm_id TEXT,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_accounting_integrations_company
        FOREIGN KEY (company_id) REFERENCES public.company(id)
);

CREATE INDEX IF NOT EXISTS idx_accounting_integrations_company_id
    ON public.accounting_integrations(company_id);

CREATE TABLE IF NOT EXISTS public.accounting_sync_logs (
    id BIGSERIAL PRIMARY KEY,
    company_id INTEGER NOT NULL,
    entity_type TEXT NOT NULL,
    entity_id TEXT,
    sync_status TEXT NOT NULL,
    error_message TEXT,
    synced_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_accounting_sync_logs_company
        FOREIGN KEY (company_id) REFERENCES public.company(id)
);

CREATE INDEX IF NOT EXISTS idx_accounting_sync_logs_company_id
    ON public.accounting_sync_logs(company_id);

CREATE INDEX IF NOT EXISTS idx_accounting_sync_logs_sync_status
    ON public.accounting_sync_logs(sync_status);

CREATE INDEX IF NOT EXISTS idx_accounting_sync_logs_synced_at
    ON public.accounting_sync_logs(synced_at);

CREATE TABLE IF NOT EXISTS public.chart_of_accounts_mapping (
    id BIGSERIAL PRIMARY KEY,
    company_id INTEGER NOT NULL,
    caskr_account_type TEXT NOT NULL,
    qbo_account_id TEXT NOT NULL,
    qbo_account_name TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_chart_of_accounts_mapping_company
        FOREIGN KEY (company_id) REFERENCES public.company(id)
);

CREATE INDEX IF NOT EXISTS idx_chart_of_accounts_mapping_company_id
    ON public.chart_of_accounts_mapping(company_id);
