--
-- Migration: Create saved_reports table
-- Date: 2025-12-05
-- Description: Adds saved_reports table for storing user report configurations
--

CREATE TABLE IF NOT EXISTS public.saved_reports (
    id SERIAL PRIMARY KEY,
    company_id INTEGER NOT NULL,
    user_id INTEGER NOT NULL,
    report_template_id INTEGER NOT NULL,
    name VARCHAR(100) NOT NULL,
    description VARCHAR(500),
    filter_values TEXT,
    is_favorite BOOLEAN NOT NULL DEFAULT FALSE,
    last_run_at TIMESTAMPTZ,
    run_count INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ,
    CONSTRAINT fk_saved_reports_company FOREIGN KEY (company_id) REFERENCES public.company(id) ON DELETE CASCADE,
    CONSTRAINT fk_saved_reports_user FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE CASCADE,
    CONSTRAINT fk_saved_reports_template FOREIGN KEY (report_template_id) REFERENCES public.report_templates(id) ON DELETE CASCADE
);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_saved_reports_company_user ON public.saved_reports(company_id, user_id);
CREATE INDEX IF NOT EXISTS idx_saved_reports_template ON public.saved_reports(report_template_id);
CREATE INDEX IF NOT EXISTS idx_saved_reports_favorite ON public.saved_reports(is_favorite) WHERE is_favorite = TRUE;

-- Comments
COMMENT ON TABLE public.saved_reports IS 'Stores user-saved report configurations with pre-selected filters';
COMMENT ON COLUMN public.saved_reports.filter_values IS 'JSON object containing saved filter values';
COMMENT ON COLUMN public.saved_reports.is_favorite IS 'Whether this saved report is marked as a favorite for quick access';
COMMENT ON COLUMN public.saved_reports.run_count IS 'Number of times this saved report has been executed';
