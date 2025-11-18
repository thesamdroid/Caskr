--
-- Migration: TTB compliance tracking tables
-- Date: 2025-02-20
-- Description: Adds ttb_monthly_reports, ttb_inventory_snapshots, and ttb_transactions tables with indexes and constraints
--

CREATE TABLE IF NOT EXISTS public.ttb_monthly_reports (
    id BIGSERIAL PRIMARY KEY,
    company_id INTEGER NOT NULL,
    report_month SMALLINT NOT NULL,
    report_year INTEGER NOT NULL,
    status VARCHAR(32) NOT NULL DEFAULT 'Draft',
    generated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    submitted_at TIMESTAMPTZ,
    ttb_confirmation_number VARCHAR(150),
    pdf_path TEXT,
    created_by_user_id INTEGER,
    CONSTRAINT fk_ttb_monthly_reports_company
        FOREIGN KEY (company_id) REFERENCES public.company(id),
    CONSTRAINT fk_ttb_monthly_reports_creator
        FOREIGN KEY (created_by_user_id) REFERENCES public.users(id),
    CONSTRAINT chk_ttb_monthly_reports_month
        CHECK (report_month BETWEEN 1 AND 12),
    CONSTRAINT chk_ttb_monthly_reports_status
        CHECK (status IN ('Draft', 'Submitted', 'Approved'))
);

CREATE INDEX IF NOT EXISTS idx_ttb_monthly_reports_company_month_year
    ON public.ttb_monthly_reports(company_id, report_month, report_year);

CREATE TABLE IF NOT EXISTS public.ttb_inventory_snapshots (
    id BIGSERIAL PRIMARY KEY,
    company_id INTEGER NOT NULL,
    snapshot_date DATE NOT NULL,
    product_type TEXT NOT NULL,
    spirits_type VARCHAR(64) NOT NULL,
    proof_gallons DECIMAL(12,2) NOT NULL DEFAULT 0,
    wine_gallons DECIMAL(12,2) NOT NULL DEFAULT 0,
    tax_status VARCHAR(32) NOT NULL,
    CONSTRAINT fk_ttb_inventory_snapshots_company
        FOREIGN KEY (company_id) REFERENCES public.company(id),
    CONSTRAINT chk_ttb_inventory_snapshots_spirits_type
        CHECK (spirits_type IN ('Under190Proof', 'Neutral', 'Wine')),
    CONSTRAINT chk_ttb_inventory_snapshots_tax_status
        CHECK (tax_status IN ('Bonded', 'TaxPaid', 'Export'))
);

CREATE INDEX IF NOT EXISTS idx_ttb_inventory_snapshots_company_date
    ON public.ttb_inventory_snapshots(company_id, snapshot_date);

CREATE TABLE IF NOT EXISTS public.ttb_transactions (
    id BIGSERIAL PRIMARY KEY,
    company_id INTEGER NOT NULL,
    transaction_date DATE NOT NULL,
    transaction_type VARCHAR(64) NOT NULL,
    product_type TEXT NOT NULL,
    spirits_type VARCHAR(64) NOT NULL,
    proof_gallons DECIMAL(12,2) NOT NULL DEFAULT 0,
    wine_gallons DECIMAL(12,2) NOT NULL DEFAULT 0,
    source_entity_type VARCHAR(64),
    source_entity_id INTEGER,
    related_entity_id INTEGER,
    notes TEXT,
    CONSTRAINT fk_ttb_transactions_company
        FOREIGN KEY (company_id) REFERENCES public.company(id),
    CONSTRAINT chk_ttb_transactions_transaction_type
        CHECK (transaction_type IN ('Production', 'Transfer_In', 'Transfer_Out', 'Loss', 'Gain', 'TaxDetermination', 'Destruction')),
    CONSTRAINT chk_ttb_transactions_spirits_type
        CHECK (spirits_type IN ('Under190Proof', 'Neutral', 'Wine')),
    CONSTRAINT chk_ttb_transactions_source_entity_type
        CHECK (source_entity_type IS NULL OR source_entity_type IN ('Batch', 'Barrel', 'Transfer'))
);

CREATE INDEX IF NOT EXISTS idx_ttb_transactions_company_date
    ON public.ttb_transactions(company_id, transaction_date);
