--
-- Migration: TTB audit logs for compliance tracking
-- Date: 2025-12-06
-- Description: Creates ttb_audit_logs table for tracking all changes to TTB compliance data.
--              Provides immutable audit trail for TTB inspections.
--

CREATE TABLE IF NOT EXISTS public.ttb_audit_logs (
    id SERIAL PRIMARY KEY,
    company_id INTEGER NOT NULL,
    entity_type VARCHAR(50) NOT NULL,
    entity_id INTEGER NOT NULL,
    action VARCHAR(20) NOT NULL,
    changed_by_user_id INTEGER NOT NULL,
    change_timestamp TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    old_values JSONB,
    new_values JSONB,
    ip_address VARCHAR(45),
    user_agent TEXT,
    change_description TEXT,
    CONSTRAINT fk_ttb_audit_logs_company
        FOREIGN KEY (company_id) REFERENCES public.companies(id) ON DELETE CASCADE,
    CONSTRAINT fk_ttb_audit_logs_user
        FOREIGN KEY (changed_by_user_id) REFERENCES public.users(id) ON DELETE RESTRICT,
    CONSTRAINT chk_ttb_audit_logs_action
        CHECK (action IN ('Create', 'Update', 'Delete')),
    CONSTRAINT chk_ttb_audit_logs_entity_type
        CHECK (entity_type IN ('TtbTransaction', 'TtbMonthlyReport', 'TtbGaugeRecord', 'TtbTaxDetermination'))
);

-- Index for company-scoped queries
CREATE INDEX IF NOT EXISTS idx_ttb_audit_logs_company_id
    ON public.ttb_audit_logs(company_id);

-- Index for filtering by entity type
CREATE INDEX IF NOT EXISTS idx_ttb_audit_logs_entity_type
    ON public.ttb_audit_logs(entity_type);

-- Index for finding changes to a specific entity
CREATE INDEX IF NOT EXISTS idx_ttb_audit_logs_entity_id
    ON public.ttb_audit_logs(entity_id);

-- Index for chronological queries and date range filtering
CREATE INDEX IF NOT EXISTS idx_ttb_audit_logs_timestamp
    ON public.ttb_audit_logs(change_timestamp);

-- Index for finding changes made by a specific user
CREATE INDEX IF NOT EXISTS idx_ttb_audit_logs_user_id
    ON public.ttb_audit_logs(changed_by_user_id);

-- Composite index for common audit trail queries (entity type + id + timestamp)
CREATE INDEX IF NOT EXISTS idx_ttb_audit_logs_entity_lookup
    ON public.ttb_audit_logs(entity_type, entity_id, change_timestamp DESC);

COMMENT ON TABLE public.ttb_audit_logs IS 'Immutable audit log for all TTB compliance data changes';
COMMENT ON COLUMN public.ttb_audit_logs.entity_type IS 'Type of entity changed: TtbTransaction, TtbMonthlyReport, TtbGaugeRecord, or TtbTaxDetermination';
COMMENT ON COLUMN public.ttb_audit_logs.action IS 'The action performed: Create, Update, or Delete';
COMMENT ON COLUMN public.ttb_audit_logs.old_values IS 'JSON snapshot of entity values before change (null for Create)';
COMMENT ON COLUMN public.ttb_audit_logs.new_values IS 'JSON snapshot of entity values after change (null for Delete)';
COMMENT ON COLUMN public.ttb_audit_logs.change_description IS 'Human-readable description of the change for display';
