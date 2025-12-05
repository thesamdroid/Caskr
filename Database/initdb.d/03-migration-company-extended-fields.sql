--
-- Migration: Add extended fields to company table
-- Date: 2025-11-10
-- Description: Adds address, contact, TTB permit, and audit fields to company table
--

-- Add address fields
ALTER TABLE public.company
ADD COLUMN IF NOT EXISTS address_line1 VARCHAR(255);

ALTER TABLE public.company
ADD COLUMN IF NOT EXISTS address_line2 VARCHAR(255);

ALTER TABLE public.company
ADD COLUMN IF NOT EXISTS city VARCHAR(100);

ALTER TABLE public.company
ADD COLUMN IF NOT EXISTS state VARCHAR(100);

ALTER TABLE public.company
ADD COLUMN IF NOT EXISTS postal_code VARCHAR(20);

ALTER TABLE public.company
ADD COLUMN IF NOT EXISTS country VARCHAR(100);

-- Add contact fields
ALTER TABLE public.company
ADD COLUMN IF NOT EXISTS phone_number VARCHAR(50);

ALTER TABLE public.company
ADD COLUMN IF NOT EXISTS website VARCHAR(255);

-- Add TTB permit number for distillery licensing
ALTER TABLE public.company
ADD COLUMN IF NOT EXISTS ttb_permit_number VARCHAR(50);

-- Add active status flag
ALTER TABLE public.company
ADD COLUMN IF NOT EXISTS is_active BOOLEAN DEFAULT true NOT NULL;

-- Add updated_at timestamp with default value
ALTER TABLE public.company
ADD COLUMN IF NOT EXISTS updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP;

-- Backfill updated_at for existing rows that might have NULL
UPDATE public.company SET updated_at = created_date WHERE updated_at IS NULL;

-- Create indexes for common queries
CREATE INDEX IF NOT EXISTS idx_company_is_active ON public.company(is_active);
CREATE INDEX IF NOT EXISTS idx_company_name ON public.company(company_name);

-- Add comments to document the fields
COMMENT ON COLUMN public.company.ttb_permit_number IS 'TTB (Alcohol and Tobacco Tax and Trade Bureau) permit number for distillery';
COMMENT ON COLUMN public.company.is_active IS 'Indicates whether the company account is active';
COMMENT ON COLUMN public.company.updated_at IS 'Timestamp of the last update to the company record';
