--
-- Migration: TTB form type support for processing and storage reports
-- Date: 2025-11-29
-- Description: Adds form_type column and expands status validation for TTB monthly reports
--

ALTER TABLE public.ttb_monthly_reports
    ADD COLUMN IF NOT EXISTS form_type VARCHAR(16) NOT NULL DEFAULT '5110_28';

UPDATE public.ttb_monthly_reports
SET form_type = COALESCE(form_type, '5110_28');

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
        CHECK (status IN ('Draft', 'Submitted', 'Approved', 'Rejected'));

DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM information_schema.table_constraints
        WHERE constraint_name = 'chk_ttb_monthly_reports_form_type'
          AND table_name = 'ttb_monthly_reports'
    ) THEN
        ALTER TABLE public.ttb_monthly_reports
            DROP CONSTRAINT chk_ttb_monthly_reports_form_type;
    END IF;
END $$;

ALTER TABLE public.ttb_monthly_reports
    ADD CONSTRAINT chk_ttb_monthly_reports_form_type
        CHECK (form_type IN ('5110_28', '5110_40'));
