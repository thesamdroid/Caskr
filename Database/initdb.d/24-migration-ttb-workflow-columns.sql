--
-- Migration: TTB workflow tracking columns
-- Date: 2025-12-06
-- Description: Adds workflow-related columns to ttb_monthly_reports for review/approval process
--

-- Add validation result columns
ALTER TABLE public.ttb_monthly_reports
    ADD COLUMN IF NOT EXISTS validation_errors TEXT,
    ADD COLUMN IF NOT EXISTS validation_warnings TEXT;

-- Add workflow tracking columns
ALTER TABLE public.ttb_monthly_reports
    ADD COLUMN IF NOT EXISTS submitted_for_review_by_user_id INTEGER,
    ADD COLUMN IF NOT EXISTS submitted_for_review_at TIMESTAMPTZ,
    ADD COLUMN IF NOT EXISTS reviewed_by_user_id INTEGER,
    ADD COLUMN IF NOT EXISTS reviewed_at TIMESTAMPTZ,
    ADD COLUMN IF NOT EXISTS approved_by_user_id INTEGER,
    ADD COLUMN IF NOT EXISTS approved_at TIMESTAMPTZ,
    ADD COLUMN IF NOT EXISTS review_notes TEXT;

-- Add foreign keys for user references
ALTER TABLE public.ttb_monthly_reports
    ADD CONSTRAINT IF NOT EXISTS fk_ttb_monthly_reports_submitted_for_review_by
        FOREIGN KEY (submitted_for_review_by_user_id) REFERENCES public.users(id);

ALTER TABLE public.ttb_monthly_reports
    ADD CONSTRAINT IF NOT EXISTS fk_ttb_monthly_reports_reviewed_by
        FOREIGN KEY (reviewed_by_user_id) REFERENCES public.users(id);

ALTER TABLE public.ttb_monthly_reports
    ADD CONSTRAINT IF NOT EXISTS fk_ttb_monthly_reports_approved_by
        FOREIGN KEY (approved_by_user_id) REFERENCES public.users(id);

-- Update status constraint to include workflow statuses
DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM information_schema.table_constraints
        WHERE constraint_name = 'chk_ttb_monthly_reports_status'
          AND table_name = 'ttb_monthly_reports'
    ) THEN
        ALTER TABLE public.ttb_monthly_reports
            DROP CONSTRAINT chk_ttb_monthly_reports_status;
    END IF;
END $$;

ALTER TABLE public.ttb_monthly_reports
    ADD CONSTRAINT chk_ttb_monthly_reports_status
        CHECK (status IN ('Draft', 'PendingReview', 'Approved', 'Rejected', 'Submitted', 'Archived', 'ValidationFailed'));

-- Add comments
COMMENT ON COLUMN public.ttb_monthly_reports.validation_errors IS 'JSON array of validation errors found during report generation';
COMMENT ON COLUMN public.ttb_monthly_reports.validation_warnings IS 'JSON array of validation warnings found during report generation';
COMMENT ON COLUMN public.ttb_monthly_reports.submitted_for_review_by_user_id IS 'User who submitted report for review';
COMMENT ON COLUMN public.ttb_monthly_reports.submitted_for_review_at IS 'When report was submitted for review';
COMMENT ON COLUMN public.ttb_monthly_reports.reviewed_by_user_id IS 'Compliance manager who reviewed the report';
COMMENT ON COLUMN public.ttb_monthly_reports.reviewed_at IS 'When report was reviewed';
COMMENT ON COLUMN public.ttb_monthly_reports.approved_by_user_id IS 'User who approved report for TTB submission';
COMMENT ON COLUMN public.ttb_monthly_reports.approved_at IS 'When report was approved';
COMMENT ON COLUMN public.ttb_monthly_reports.review_notes IS 'Notes from reviewer during approval/rejection';
