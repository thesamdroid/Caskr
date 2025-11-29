--
-- Migration: TTB gauge records for barrel tracking
-- Date: 2025-11-29
-- Description: Adds ttb_gauge_records table for tracking proof, temperature, and volume measurements
--              required for TTB compliance and monthly reporting
--

CREATE TABLE IF NOT EXISTS public.ttb_gauge_records (
    id BIGSERIAL PRIMARY KEY,
    barrel_id INTEGER NOT NULL,
    gauge_date TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    gauge_type VARCHAR(32) NOT NULL,
    proof DECIMAL(5,2) NOT NULL,
    temperature DECIMAL(5,2) NOT NULL,
    wine_gallons DECIMAL(10,2) NOT NULL,
    proof_gallons DECIMAL(10,2) NOT NULL,
    gauged_by_user_id INTEGER,
    notes TEXT,
    CONSTRAINT fk_ttb_gauge_records_barrel
        FOREIGN KEY (barrel_id) REFERENCES public.barrel(id) ON DELETE CASCADE,
    CONSTRAINT fk_ttb_gauge_records_user
        FOREIGN KEY (gauged_by_user_id) REFERENCES public.users(id) ON DELETE SET NULL,
    CONSTRAINT chk_ttb_gauge_records_gauge_type
        CHECK (gauge_type IN ('Fill', 'Storage', 'Removal')),
    CONSTRAINT chk_ttb_gauge_records_proof
        CHECK (proof >= 0 AND proof <= 200),
    CONSTRAINT chk_ttb_gauge_records_temperature
        CHECK (temperature >= -40 AND temperature <= 150),
    CONSTRAINT chk_ttb_gauge_records_wine_gallons
        CHECK (wine_gallons > 0),
    CONSTRAINT chk_ttb_gauge_records_proof_gallons
        CHECK (proof_gallons >= 0)
);

-- Index for finding all gauge records for a specific barrel
CREATE INDEX IF NOT EXISTS idx_ttb_gauge_records_barrel_id
    ON public.ttb_gauge_records(barrel_id);

-- Index for finding gauge records by date range (for monthly reports)
CREATE INDEX IF NOT EXISTS idx_ttb_gauge_records_gauge_date
    ON public.ttb_gauge_records(gauge_date);

-- Index for finding gauge records by type and date (for report filtering)
CREATE INDEX IF NOT EXISTS idx_ttb_gauge_records_type_date
    ON public.ttb_gauge_records(gauge_type, gauge_date);

-- Composite index for company-wide queries (joins through barrel)
CREATE INDEX IF NOT EXISTS idx_ttb_gauge_records_barrel_date
    ON public.ttb_gauge_records(barrel_id, gauge_date DESC);

COMMENT ON TABLE public.ttb_gauge_records IS 'TTB gauge records tracking proof, temperature, and volume measurements for barrels';
COMMENT ON COLUMN public.ttb_gauge_records.gauge_type IS 'Type of gauge: Fill (initial), Storage (inventory check), or Removal (final)';
COMMENT ON COLUMN public.ttb_gauge_records.proof IS 'Proof of spirits (0-200)';
COMMENT ON COLUMN public.ttb_gauge_records.temperature IS 'Temperature in Fahrenheit at time of gauging';
COMMENT ON COLUMN public.ttb_gauge_records.wine_gallons IS 'Total volume in wine gallons';
COMMENT ON COLUMN public.ttb_gauge_records.proof_gallons IS 'Calculated proof gallons (wine_gallons * proof / 100 * temperature_correction_factor)';
