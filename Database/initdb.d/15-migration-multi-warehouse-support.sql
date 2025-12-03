-- Migration: Multi-Warehouse Support (WH-004)
-- This migration extends the database schema to support multiple warehouses per company
-- Enables large distilleries with multiple facilities to manage all inventory in one system

-- ============================================================================
-- 1. Create warehouses table (enhanced version of rickhouse)
-- ============================================================================

CREATE TYPE warehouse_type AS ENUM ('Rickhouse', 'Palletized', 'Tank_Farm', 'Outdoor');
CREATE TYPE warehouse_transfer_status AS ENUM ('Pending', 'In_Transit', 'Completed', 'Cancelled');

CREATE TABLE public.warehouses (
    id SERIAL PRIMARY KEY,
    company_id INTEGER NOT NULL REFERENCES public.company(id),
    name VARCHAR(200) NOT NULL,
    warehouse_type warehouse_type NOT NULL DEFAULT 'Rickhouse',
    -- Address fields
    address_line1 VARCHAR(255),
    address_line2 VARCHAR(255),
    city VARCHAR(100),
    state VARCHAR(100),
    postal_code VARCHAR(20),
    country VARCHAR(100) DEFAULT 'USA',
    -- Capacity and dimensions
    total_capacity INTEGER NOT NULL DEFAULT 0,  -- Number of barrel positions
    length_feet DECIMAL(10,2),
    width_feet DECIMAL(10,2),
    height_feet DECIMAL(10,2),
    -- Status and metadata
    is_active BOOLEAN NOT NULL DEFAULT true,
    notes TEXT,
    -- Audit fields
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by_user_id INTEGER REFERENCES public.users(id),

    -- Constraints
    CONSTRAINT uq_warehouses_company_name UNIQUE (company_id, name)
);

-- Create indexes for warehouses
CREATE INDEX idx_warehouses_company_id ON public.warehouses(company_id);
CREATE INDEX idx_warehouses_is_active ON public.warehouses(is_active);
CREATE INDEX idx_warehouses_type ON public.warehouses(warehouse_type);

COMMENT ON TABLE public.warehouses IS 'Storage facilities (rickhouses, palletized warehouses, tank farms) for barrel inventory';
COMMENT ON COLUMN public.warehouses.total_capacity IS 'Maximum number of barrel positions in this warehouse';
COMMENT ON COLUMN public.warehouses.warehouse_type IS 'Type of storage: Rickhouse (traditional), Palletized, Tank_Farm, or Outdoor';

-- ============================================================================
-- 2. Migrate existing rickhouse data to warehouses table
-- ============================================================================

-- Copy existing rickhouses to new warehouses table
INSERT INTO public.warehouses (company_id, name, warehouse_type, address_line1, created_at)
SELECT company_id, name, 'Rickhouse', address, CURRENT_TIMESTAMP
FROM public.rickhouse;

-- ============================================================================
-- 3. Update barrels table: Add warehouse_id FK with index
-- ============================================================================

-- Add warehouse_id column (nullable initially for migration)
ALTER TABLE public.barrel ADD COLUMN warehouse_id INTEGER;

-- Create index on warehouse_id for performance
CREATE INDEX idx_barrel_warehouse_id ON public.barrel(warehouse_id);

-- Backfill warehouse_id from existing rickhouse relationship
-- Maps each barrel to the corresponding warehouse based on rickhouse
UPDATE public.barrel b
SET warehouse_id = (
    SELECT w.id
    FROM public.warehouses w
    INNER JOIN public.rickhouse r ON r.company_id = w.company_id AND r.name = w.name
    WHERE r.id = b.rickhouse_id
    LIMIT 1
);

-- For any barrels without a mapped warehouse, assign to first warehouse of company
UPDATE public.barrel b
SET warehouse_id = (
    SELECT w.id
    FROM public.warehouses w
    WHERE w.company_id = b.company_id
    ORDER BY w.id
    LIMIT 1
)
WHERE warehouse_id IS NULL;

-- Make warehouse_id NOT NULL after backfill
ALTER TABLE public.barrel ALTER COLUMN warehouse_id SET NOT NULL;

-- Add foreign key constraint
ALTER TABLE public.barrel
ADD CONSTRAINT fk_barrel_warehouse
FOREIGN KEY (warehouse_id) REFERENCES public.warehouses(id);

-- ============================================================================
-- 4. Update orders table: Add fulfillment_warehouse_id FK
-- ============================================================================

ALTER TABLE public.orders ADD COLUMN fulfillment_warehouse_id INTEGER;

-- Create index for fulfillment warehouse lookups
CREATE INDEX idx_orders_fulfillment_warehouse_id ON public.orders(fulfillment_warehouse_id);

-- Add foreign key constraint
ALTER TABLE public.orders
ADD CONSTRAINT fk_orders_fulfillment_warehouse
FOREIGN KEY (fulfillment_warehouse_id) REFERENCES public.warehouses(id);

COMMENT ON COLUMN public.orders.fulfillment_warehouse_id IS 'Warehouse that will fulfill this order (source of barrels)';

-- ============================================================================
-- 5. Create inter_warehouse_transfers table
-- ============================================================================

CREATE TABLE public.inter_warehouse_transfers (
    id SERIAL PRIMARY KEY,
    from_warehouse_id INTEGER NOT NULL REFERENCES public.warehouses(id),
    to_warehouse_id INTEGER NOT NULL REFERENCES public.warehouses(id),
    transfer_date DATE NOT NULL,
    barrels_count INTEGER NOT NULL DEFAULT 0,
    proof_gallons DECIMAL(12,2),
    status warehouse_transfer_status NOT NULL DEFAULT 'Pending',
    initiated_by_user_id INTEGER REFERENCES public.users(id),
    completed_at TIMESTAMPTZ,
    notes TEXT,
    -- Audit fields
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,

    -- Constraints
    CONSTRAINT chk_different_warehouses CHECK (from_warehouse_id != to_warehouse_id),
    CONSTRAINT chk_barrels_count_positive CHECK (barrels_count >= 0),
    CONSTRAINT chk_proof_gallons_positive CHECK (proof_gallons IS NULL OR proof_gallons >= 0)
);

-- Create indexes for inter_warehouse_transfers
CREATE INDEX idx_inter_warehouse_transfers_from ON public.inter_warehouse_transfers(from_warehouse_id);
CREATE INDEX idx_inter_warehouse_transfers_to ON public.inter_warehouse_transfers(to_warehouse_id);
CREATE INDEX idx_inter_warehouse_transfers_status ON public.inter_warehouse_transfers(status);
CREATE INDEX idx_inter_warehouse_transfers_date ON public.inter_warehouse_transfers(transfer_date);
CREATE INDEX idx_inter_warehouse_transfers_initiated_by ON public.inter_warehouse_transfers(initiated_by_user_id);

COMMENT ON TABLE public.inter_warehouse_transfers IS 'Tracks bulk transfers of barrels between warehouses';

-- ============================================================================
-- 6. Create barrel_transfers table (links barrels to transfers)
-- ============================================================================

CREATE TABLE public.barrel_transfers (
    id SERIAL PRIMARY KEY,
    barrel_id INTEGER NOT NULL REFERENCES public.barrel(id),
    transfer_id INTEGER NOT NULL REFERENCES public.inter_warehouse_transfers(id) ON DELETE CASCADE,
    transferred_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,

    -- Constraints
    CONSTRAINT uq_barrel_transfer UNIQUE (barrel_id, transfer_id)
);

-- Create indexes for barrel_transfers
CREATE INDEX idx_barrel_transfers_barrel_id ON public.barrel_transfers(barrel_id);
CREATE INDEX idx_barrel_transfers_transfer_id ON public.barrel_transfers(transfer_id);

COMMENT ON TABLE public.barrel_transfers IS 'Links individual barrels to inter-warehouse transfers';

-- ============================================================================
-- 7. Create warehouse_capacity_snapshots table (for daily trending)
-- ============================================================================

CREATE TABLE public.warehouse_capacity_snapshots (
    id SERIAL PRIMARY KEY,
    warehouse_id INTEGER NOT NULL REFERENCES public.warehouses(id) ON DELETE CASCADE,
    snapshot_date DATE NOT NULL,
    total_capacity INTEGER NOT NULL,
    occupied_positions INTEGER NOT NULL DEFAULT 0,
    occupancy_percentage DECIMAL(5,2) NOT NULL DEFAULT 0.00,
    -- Audit fields
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,

    -- Constraints
    CONSTRAINT uq_warehouse_snapshot_date UNIQUE (warehouse_id, snapshot_date),
    CONSTRAINT chk_occupancy_range CHECK (occupancy_percentage >= 0 AND occupancy_percentage <= 100)
);

-- Create indexes for warehouse_capacity_snapshots
CREATE INDEX idx_warehouse_capacity_snapshots_warehouse_id ON public.warehouse_capacity_snapshots(warehouse_id);
CREATE INDEX idx_warehouse_capacity_snapshots_date ON public.warehouse_capacity_snapshots(snapshot_date);
CREATE INDEX idx_warehouse_capacity_snapshots_occupancy ON public.warehouse_capacity_snapshots(occupancy_percentage);

COMMENT ON TABLE public.warehouse_capacity_snapshots IS 'Daily snapshots of warehouse occupancy for capacity trending and reporting';

-- ============================================================================
-- 8. Create views for warehouse utilization
-- ============================================================================

-- View: Current warehouse occupancy
CREATE OR REPLACE VIEW public.vw_warehouse_current_occupancy AS
SELECT
    w.id AS warehouse_id,
    w.company_id,
    w.name AS warehouse_name,
    w.warehouse_type,
    w.total_capacity,
    COUNT(b.id) AS occupied_positions,
    CASE
        WHEN w.total_capacity > 0 THEN ROUND((COUNT(b.id)::DECIMAL / w.total_capacity) * 100, 2)
        ELSE 0
    END AS occupancy_percentage,
    w.is_active
FROM public.warehouses w
LEFT JOIN public.barrel b ON b.warehouse_id = w.id
GROUP BY w.id, w.company_id, w.name, w.warehouse_type, w.total_capacity, w.is_active;

COMMENT ON VIEW public.vw_warehouse_current_occupancy IS 'Real-time view of warehouse occupancy levels';

-- View: Transfer history summary
CREATE OR REPLACE VIEW public.vw_warehouse_transfer_summary AS
SELECT
    t.id AS transfer_id,
    fw.name AS from_warehouse_name,
    tw.name AS to_warehouse_name,
    t.transfer_date,
    t.barrels_count,
    t.proof_gallons,
    t.status,
    u.name AS initiated_by,
    t.completed_at,
    t.notes
FROM public.inter_warehouse_transfers t
INNER JOIN public.warehouses fw ON t.from_warehouse_id = fw.id
INNER JOIN public.warehouses tw ON t.to_warehouse_id = tw.id
LEFT JOIN public.users u ON t.initiated_by_user_id = u.id;

COMMENT ON VIEW public.vw_warehouse_transfer_summary IS 'Summary view of inter-warehouse transfers with warehouse names';

-- View: Company warehouse summary
CREATE OR REPLACE VIEW public.vw_company_warehouse_summary AS
SELECT
    c.id AS company_id,
    c.company_name,
    COUNT(DISTINCT w.id) AS warehouse_count,
    SUM(w.total_capacity) AS total_capacity,
    COUNT(b.id) AS total_barrels,
    CASE
        WHEN SUM(w.total_capacity) > 0 THEN ROUND((COUNT(b.id)::DECIMAL / SUM(w.total_capacity)) * 100, 2)
        ELSE 0
    END AS overall_occupancy_percentage
FROM public.company c
LEFT JOIN public.warehouses w ON w.company_id = c.id AND w.is_active = true
LEFT JOIN public.barrel b ON b.warehouse_id = w.id
GROUP BY c.id, c.company_name;

COMMENT ON VIEW public.vw_company_warehouse_summary IS 'Aggregate warehouse metrics per company';

-- ============================================================================
-- 9. Create stored procedure to capture daily snapshots
-- ============================================================================

CREATE OR REPLACE FUNCTION capture_warehouse_capacity_snapshot()
RETURNS void AS $$
BEGIN
    INSERT INTO public.warehouse_capacity_snapshots (
        warehouse_id,
        snapshot_date,
        total_capacity,
        occupied_positions,
        occupancy_percentage
    )
    SELECT
        warehouse_id,
        CURRENT_DATE,
        total_capacity,
        occupied_positions,
        occupancy_percentage
    FROM public.vw_warehouse_current_occupancy
    ON CONFLICT (warehouse_id, snapshot_date)
    DO UPDATE SET
        total_capacity = EXCLUDED.total_capacity,
        occupied_positions = EXCLUDED.occupied_positions,
        occupancy_percentage = EXCLUDED.occupancy_percentage;
END;
$$ LANGUAGE plpgsql;

COMMENT ON FUNCTION capture_warehouse_capacity_snapshot IS 'Captures current occupancy snapshot for all warehouses. Run daily via cron.';

-- ============================================================================
-- 10. Create stored procedure to complete warehouse transfer
-- ============================================================================

CREATE OR REPLACE FUNCTION complete_warehouse_transfer(p_transfer_id INTEGER)
RETURNS void AS $$
DECLARE
    v_to_warehouse_id INTEGER;
    v_transfer_status warehouse_transfer_status;
BEGIN
    -- Get transfer details
    SELECT to_warehouse_id, status INTO v_to_warehouse_id, v_transfer_status
    FROM public.inter_warehouse_transfers
    WHERE id = p_transfer_id;

    -- Validate transfer exists and is in correct status
    IF v_to_warehouse_id IS NULL THEN
        RAISE EXCEPTION 'Transfer with ID % not found', p_transfer_id;
    END IF;

    IF v_transfer_status != 'In_Transit' THEN
        RAISE EXCEPTION 'Transfer must be In_Transit to complete. Current status: %', v_transfer_status;
    END IF;

    -- Update barrel locations
    UPDATE public.barrel b
    SET warehouse_id = v_to_warehouse_id
    FROM public.barrel_transfers bt
    WHERE bt.barrel_id = b.id AND bt.transfer_id = p_transfer_id;

    -- Update transfer status
    UPDATE public.inter_warehouse_transfers
    SET status = 'Completed',
        completed_at = CURRENT_TIMESTAMP,
        updated_at = CURRENT_TIMESTAMP
    WHERE id = p_transfer_id;
END;
$$ LANGUAGE plpgsql;

COMMENT ON FUNCTION complete_warehouse_transfer IS 'Completes an inter-warehouse transfer by updating barrel locations and transfer status';

-- ============================================================================
-- 11. Create trigger to update barrels count on transfer
-- ============================================================================

CREATE OR REPLACE FUNCTION update_transfer_barrels_count()
RETURNS TRIGGER AS $$
BEGIN
    IF TG_OP = 'INSERT' THEN
        UPDATE public.inter_warehouse_transfers
        SET barrels_count = (
            SELECT COUNT(*) FROM public.barrel_transfers WHERE transfer_id = NEW.transfer_id
        ),
        updated_at = CURRENT_TIMESTAMP
        WHERE id = NEW.transfer_id;
    ELSIF TG_OP = 'DELETE' THEN
        UPDATE public.inter_warehouse_transfers
        SET barrels_count = (
            SELECT COUNT(*) FROM public.barrel_transfers WHERE transfer_id = OLD.transfer_id
        ),
        updated_at = CURRENT_TIMESTAMP
        WHERE id = OLD.transfer_id;
    END IF;
    RETURN NULL;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_update_transfer_barrels_count
AFTER INSERT OR DELETE ON public.barrel_transfers
FOR EACH ROW
EXECUTE FUNCTION update_transfer_barrels_count();

-- ============================================================================
-- 12. Create trigger to update updated_at timestamp
-- ============================================================================

CREATE OR REPLACE FUNCTION update_warehouse_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_warehouses_updated_at
BEFORE UPDATE ON public.warehouses
FOR EACH ROW
EXECUTE FUNCTION update_warehouse_updated_at();

CREATE TRIGGER trg_inter_warehouse_transfers_updated_at
BEFORE UPDATE ON public.inter_warehouse_transfers
FOR EACH ROW
EXECUTE FUNCTION update_warehouse_updated_at();

-- ============================================================================
-- 13. Grant permissions (adjust as needed for your environment)
-- ============================================================================

-- Grant access to postgres user
GRANT ALL ON TABLE public.warehouses TO postgres;
GRANT ALL ON TABLE public.inter_warehouse_transfers TO postgres;
GRANT ALL ON TABLE public.barrel_transfers TO postgres;
GRANT ALL ON TABLE public.warehouse_capacity_snapshots TO postgres;
GRANT USAGE, SELECT ON SEQUENCE public.warehouses_id_seq TO postgres;
GRANT USAGE, SELECT ON SEQUENCE public.inter_warehouse_transfers_id_seq TO postgres;
GRANT USAGE, SELECT ON SEQUENCE public.barrel_transfers_id_seq TO postgres;
GRANT USAGE, SELECT ON SEQUENCE public.warehouse_capacity_snapshots_id_seq TO postgres;

-- ============================================================================
-- Migration complete
-- ============================================================================
