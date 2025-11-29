-- Add auto-generation schedule settings for TTB compliance reports
ALTER TABLE IF EXISTS public.company
    ADD COLUMN IF NOT EXISTS auto_generate_ttb_reports boolean DEFAULT false,
    ADD COLUMN IF NOT EXISTS ttb_auto_report_cadence text DEFAULT 'Monthly',
    ADD COLUMN IF NOT EXISTS ttb_auto_report_hour_utc integer DEFAULT 6,
    ADD COLUMN IF NOT EXISTS ttb_auto_report_day_of_month smallint DEFAULT 1,
    ADD COLUMN IF NOT EXISTS ttb_auto_report_day_of_week text DEFAULT 'Monday';

ALTER TABLE IF EXISTS public.company
    ALTER COLUMN auto_generate_ttb_reports SET NOT NULL,
    ALTER COLUMN ttb_auto_report_cadence SET NOT NULL,
    ALTER COLUMN ttb_auto_report_hour_utc SET NOT NULL,
    ALTER COLUMN ttb_auto_report_day_of_month SET NOT NULL,
    ALTER COLUMN ttb_auto_report_day_of_week SET NOT NULL;

ALTER TABLE IF EXISTS public.users
    ADD COLUMN IF NOT EXISTS is_ttb_contact boolean DEFAULT false;

ALTER TABLE IF EXISTS public.users
    ALTER COLUMN is_ttb_contact SET NOT NULL;
