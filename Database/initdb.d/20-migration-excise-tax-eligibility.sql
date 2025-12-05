--
-- Migration: Add excise tax eligibility fields to company table
-- Date: 2025-12-05
-- Description: Adds annual_production_proof_gallons, is_eligible_for_reduced_excise_tax_rate,
--              and excise_tax_eligibility_notes columns for Craft Beverage Modernization Act tracking
--

-- Add annual production proof gallons column
ALTER TABLE public.company
ADD COLUMN IF NOT EXISTS annual_production_proof_gallons DECIMAL(12,2);

-- Add eligibility flag (defaults to true per model)
ALTER TABLE public.company
ADD COLUMN IF NOT EXISTS is_eligible_for_reduced_excise_tax_rate BOOLEAN DEFAULT true NOT NULL;

-- Add eligibility notes
ALTER TABLE public.company
ADD COLUMN IF NOT EXISTS excise_tax_eligibility_notes TEXT;

-- Add comments for documentation
COMMENT ON COLUMN public.company.annual_production_proof_gallons IS 'Annual production limit in proof gallons for determining excise tax eligibility';
COMMENT ON COLUMN public.company.is_eligible_for_reduced_excise_tax_rate IS 'Whether the company is eligible for reduced excise tax rate under Craft Beverage Modernization Act';
COMMENT ON COLUMN public.company.excise_tax_eligibility_notes IS 'Reason for excise tax rate eligibility or ineligibility';
