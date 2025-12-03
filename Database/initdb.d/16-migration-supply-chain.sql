--
-- Migration: Supply Chain Management Schema
-- Date: 2025-12-03
-- Task ID: SCM-001
-- Description: Adds tables for supply chain management including suppliers,
--              purchase orders, inventory receiving workflow, supplier product
--              catalog, and pricing history tracking.
--

-- ============================================================================
-- 1. Create ENUM Types for Supply Chain
-- ============================================================================

CREATE TYPE supplier_type AS ENUM (
    'Grain',
    'Cooperage',
    'Bottles',
    'Labels',
    'Chemicals',
    'Equipment',
    'Other'
);

CREATE TYPE purchase_order_status AS ENUM (
    'Draft',
    'Sent',
    'Confirmed',
    'Partial_Received',
    'Received',
    'Cancelled'
);

CREATE TYPE payment_status AS ENUM (
    'Unpaid',
    'Partial',
    'Paid'
);

CREATE TYPE receipt_item_condition AS ENUM (
    'Good',
    'Damaged',
    'Partial'
);

-- ============================================================================
-- 2. Suppliers Table
-- ============================================================================
-- Core table for managing supplier information
CREATE TABLE IF NOT EXISTS public.suppliers (
    id SERIAL PRIMARY KEY,
    company_id INTEGER NOT NULL,
    supplier_name VARCHAR(200) NOT NULL,
    supplier_type supplier_type NOT NULL DEFAULT 'Other',
    contact_person VARCHAR(200),
    email VARCHAR(255),
    phone VARCHAR(50),
    address TEXT,
    website VARCHAR(500),
    payment_terms VARCHAR(100),  -- e.g., 'Net 30', 'COD', 'Net 60'
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    notes TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_suppliers_company
        FOREIGN KEY (company_id) REFERENCES public.company(id),
    CONSTRAINT uq_suppliers_company_name
        UNIQUE (company_id, supplier_name)
);

-- Indexes for suppliers
CREATE INDEX IF NOT EXISTS idx_suppliers_company_id
    ON public.suppliers(company_id);

CREATE INDEX IF NOT EXISTS idx_suppliers_supplier_type
    ON public.suppliers(supplier_type);

CREATE INDEX IF NOT EXISTS idx_suppliers_is_active
    ON public.suppliers(is_active);

CREATE INDEX IF NOT EXISTS idx_suppliers_company_supplier_id
    ON public.suppliers(company_id, id);

COMMENT ON TABLE public.suppliers IS 'Supplier management table for tracking vendors who provide materials, equipment, and services to the distillery';
COMMENT ON COLUMN public.suppliers.supplier_type IS 'Category of supplier: Grain, Cooperage, Bottles, Labels, Chemicals, Equipment, or Other';
COMMENT ON COLUMN public.suppliers.payment_terms IS 'Standard payment terms with this supplier (e.g., Net 30, COD, Net 60)';

-- ============================================================================
-- 3. Supplier Products Table
-- ============================================================================
-- Catalog of products each supplier offers
CREATE TABLE IF NOT EXISTS public.supplier_products (
    id SERIAL PRIMARY KEY,
    supplier_id INTEGER NOT NULL,
    product_name VARCHAR(200) NOT NULL,
    product_category VARCHAR(100),
    sku VARCHAR(100),
    unit_of_measure VARCHAR(50),
    current_price DECIMAL(10, 2),
    currency VARCHAR(3) NOT NULL DEFAULT 'USD',
    lead_time_days INTEGER,
    minimum_order_quantity INTEGER,
    notes TEXT,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_supplier_products_supplier
        FOREIGN KEY (supplier_id) REFERENCES public.suppliers(id) ON DELETE CASCADE,
    CONSTRAINT uq_supplier_products_supplier_sku
        UNIQUE (supplier_id, sku),
    CONSTRAINT chk_supplier_products_price_positive
        CHECK (current_price IS NULL OR current_price >= 0),
    CONSTRAINT chk_supplier_products_lead_time_positive
        CHECK (lead_time_days IS NULL OR lead_time_days >= 0),
    CONSTRAINT chk_supplier_products_moq_positive
        CHECK (minimum_order_quantity IS NULL OR minimum_order_quantity >= 0)
);

-- Indexes for supplier_products
CREATE INDEX IF NOT EXISTS idx_supplier_products_supplier_id
    ON public.supplier_products(supplier_id);

CREATE INDEX IF NOT EXISTS idx_supplier_products_product_category
    ON public.supplier_products(product_category);

CREATE INDEX IF NOT EXISTS idx_supplier_products_sku
    ON public.supplier_products(sku)
    WHERE sku IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_supplier_products_is_active
    ON public.supplier_products(is_active);

COMMENT ON TABLE public.supplier_products IS 'Catalog of products each supplier offers, including pricing and ordering information';
COMMENT ON COLUMN public.supplier_products.sku IS 'Supplier-assigned product SKU/part number';
COMMENT ON COLUMN public.supplier_products.unit_of_measure IS 'Unit of measure for pricing and ordering (e.g., each, case, lb, kg, gallon)';
COMMENT ON COLUMN public.supplier_products.lead_time_days IS 'Expected number of days between ordering and delivery';
COMMENT ON COLUMN public.supplier_products.minimum_order_quantity IS 'Minimum quantity required per order';

-- ============================================================================
-- 4. Purchase Orders Table
-- ============================================================================
-- Main purchase order header table
CREATE TABLE IF NOT EXISTS public.purchase_orders (
    id SERIAL PRIMARY KEY,
    company_id INTEGER NOT NULL,
    supplier_id INTEGER NOT NULL,
    po_number VARCHAR(100) NOT NULL,
    order_date DATE NOT NULL,
    expected_delivery_date DATE,
    status purchase_order_status NOT NULL DEFAULT 'Draft',
    total_amount DECIMAL(12, 2),
    currency VARCHAR(3) NOT NULL DEFAULT 'USD',
    payment_status payment_status NOT NULL DEFAULT 'Unpaid',
    notes TEXT,
    created_by_user_id INTEGER,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_purchase_orders_company
        FOREIGN KEY (company_id) REFERENCES public.company(id),
    CONSTRAINT fk_purchase_orders_supplier
        FOREIGN KEY (supplier_id) REFERENCES public.suppliers(id),
    CONSTRAINT fk_purchase_orders_created_by
        FOREIGN KEY (created_by_user_id) REFERENCES public.users(id),
    CONSTRAINT uq_purchase_orders_po_number
        UNIQUE (po_number),
    CONSTRAINT chk_purchase_orders_total_positive
        CHECK (total_amount IS NULL OR total_amount >= 0)
);

-- Indexes for purchase_orders
CREATE INDEX IF NOT EXISTS idx_purchase_orders_company_id
    ON public.purchase_orders(company_id);

CREATE INDEX IF NOT EXISTS idx_purchase_orders_supplier_id
    ON public.purchase_orders(supplier_id);

CREATE INDEX IF NOT EXISTS idx_purchase_orders_po_number
    ON public.purchase_orders(po_number);

CREATE INDEX IF NOT EXISTS idx_purchase_orders_order_date
    ON public.purchase_orders(order_date);

CREATE INDEX IF NOT EXISTS idx_purchase_orders_status
    ON public.purchase_orders(status);

CREATE INDEX IF NOT EXISTS idx_purchase_orders_payment_status
    ON public.purchase_orders(payment_status);

CREATE INDEX IF NOT EXISTS idx_purchase_orders_company_supplier
    ON public.purchase_orders(company_id, supplier_id);

CREATE INDEX IF NOT EXISTS idx_purchase_orders_expected_delivery
    ON public.purchase_orders(expected_delivery_date)
    WHERE expected_delivery_date IS NOT NULL;

COMMENT ON TABLE public.purchase_orders IS 'Purchase order headers for tracking orders placed with suppliers';
COMMENT ON COLUMN public.purchase_orders.po_number IS 'Unique purchase order number for reference and tracking';
COMMENT ON COLUMN public.purchase_orders.status IS 'Current state of the PO: Draft, Sent, Confirmed, Partial_Received, Received, or Cancelled';
COMMENT ON COLUMN public.purchase_orders.payment_status IS 'Payment state: Unpaid, Partial, or Paid';

-- ============================================================================
-- 5. Purchase Order Items Table
-- ============================================================================
-- Line items in a purchase order
CREATE TABLE IF NOT EXISTS public.purchase_order_items (
    id SERIAL PRIMARY KEY,
    purchase_order_id INTEGER NOT NULL,
    supplier_product_id INTEGER NOT NULL,
    quantity DECIMAL(10, 2) NOT NULL,
    unit_price DECIMAL(10, 2) NOT NULL,
    total_price DECIMAL(10, 2) NOT NULL,
    received_quantity DECIMAL(10, 2) NOT NULL DEFAULT 0,
    notes TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_purchase_order_items_po
        FOREIGN KEY (purchase_order_id) REFERENCES public.purchase_orders(id) ON DELETE CASCADE,
    CONSTRAINT fk_purchase_order_items_product
        FOREIGN KEY (supplier_product_id) REFERENCES public.supplier_products(id),
    CONSTRAINT chk_poi_quantity_positive
        CHECK (quantity > 0),
    CONSTRAINT chk_poi_unit_price_positive
        CHECK (unit_price >= 0),
    CONSTRAINT chk_poi_total_price_positive
        CHECK (total_price >= 0),
    CONSTRAINT chk_poi_received_quantity_valid
        CHECK (received_quantity >= 0)
);

-- Indexes for purchase_order_items
CREATE INDEX IF NOT EXISTS idx_purchase_order_items_po_id
    ON public.purchase_order_items(purchase_order_id);

CREATE INDEX IF NOT EXISTS idx_purchase_order_items_product_id
    ON public.purchase_order_items(supplier_product_id);

COMMENT ON TABLE public.purchase_order_items IS 'Line items within a purchase order specifying products, quantities, and pricing';
COMMENT ON COLUMN public.purchase_order_items.received_quantity IS 'Quantity that has been received so far (may be less than ordered for partial shipments)';

-- ============================================================================
-- 6. Inventory Receipts Table
-- ============================================================================
-- Records receiving of goods
CREATE TABLE IF NOT EXISTS public.inventory_receipts (
    id SERIAL PRIMARY KEY,
    purchase_order_id INTEGER NOT NULL,
    receipt_date DATE NOT NULL,
    received_by_user_id INTEGER,
    notes TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_inventory_receipts_po
        FOREIGN KEY (purchase_order_id) REFERENCES public.purchase_orders(id) ON DELETE CASCADE,
    CONSTRAINT fk_inventory_receipts_received_by
        FOREIGN KEY (received_by_user_id) REFERENCES public.users(id)
);

-- Indexes for inventory_receipts
CREATE INDEX IF NOT EXISTS idx_inventory_receipts_po_id
    ON public.inventory_receipts(purchase_order_id);

CREATE INDEX IF NOT EXISTS idx_inventory_receipts_receipt_date
    ON public.inventory_receipts(receipt_date);

CREATE INDEX IF NOT EXISTS idx_inventory_receipts_received_by
    ON public.inventory_receipts(received_by_user_id)
    WHERE received_by_user_id IS NOT NULL;

COMMENT ON TABLE public.inventory_receipts IS 'Records of goods received against purchase orders. Multiple receipts can be linked to one PO for partial deliveries.';
COMMENT ON COLUMN public.inventory_receipts.receipt_date IS 'Date the goods were physically received';
COMMENT ON COLUMN public.inventory_receipts.received_by_user_id IS 'Staff member who received and verified the delivery';

-- ============================================================================
-- 7. Inventory Receipt Items Table
-- ============================================================================
-- Details of what was received in each receipt
CREATE TABLE IF NOT EXISTS public.inventory_receipt_items (
    id SERIAL PRIMARY KEY,
    inventory_receipt_id INTEGER NOT NULL,
    purchase_order_item_id INTEGER NOT NULL,
    received_quantity DECIMAL(10, 2) NOT NULL,
    condition receipt_item_condition NOT NULL DEFAULT 'Good',
    notes TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_inventory_receipt_items_receipt
        FOREIGN KEY (inventory_receipt_id) REFERENCES public.inventory_receipts(id) ON DELETE CASCADE,
    CONSTRAINT fk_inventory_receipt_items_poi
        FOREIGN KEY (purchase_order_item_id) REFERENCES public.purchase_order_items(id),
    CONSTRAINT chk_iri_received_quantity_positive
        CHECK (received_quantity > 0)
);

-- Indexes for inventory_receipt_items
CREATE INDEX IF NOT EXISTS idx_inventory_receipt_items_receipt_id
    ON public.inventory_receipt_items(inventory_receipt_id);

CREATE INDEX IF NOT EXISTS idx_inventory_receipt_items_poi_id
    ON public.inventory_receipt_items(purchase_order_item_id);

CREATE INDEX IF NOT EXISTS idx_inventory_receipt_items_condition
    ON public.inventory_receipt_items(condition);

COMMENT ON TABLE public.inventory_receipt_items IS 'Individual line items received against a purchase order, with condition tracking';
COMMENT ON COLUMN public.inventory_receipt_items.condition IS 'Quality status of received goods: Good, Damaged, or Partial';

-- ============================================================================
-- 8. Supplier Price History Table
-- ============================================================================
-- Track price changes for cost analysis
CREATE TABLE IF NOT EXISTS public.supplier_price_history (
    id SERIAL PRIMARY KEY,
    supplier_product_id INTEGER NOT NULL,
    effective_date DATE NOT NULL,
    price DECIMAL(10, 2) NOT NULL,
    changed_by_user_id INTEGER,
    change_reason TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_supplier_price_history_product
        FOREIGN KEY (supplier_product_id) REFERENCES public.supplier_products(id) ON DELETE CASCADE,
    CONSTRAINT fk_supplier_price_history_changed_by
        FOREIGN KEY (changed_by_user_id) REFERENCES public.users(id),
    CONSTRAINT chk_sph_price_positive
        CHECK (price >= 0)
);

-- Indexes for supplier_price_history
CREATE INDEX IF NOT EXISTS idx_supplier_price_history_product_id
    ON public.supplier_price_history(supplier_product_id);

CREATE INDEX IF NOT EXISTS idx_supplier_price_history_effective_date
    ON public.supplier_price_history(effective_date);

CREATE INDEX IF NOT EXISTS idx_supplier_price_history_product_date
    ON public.supplier_price_history(supplier_product_id, effective_date DESC);

COMMENT ON TABLE public.supplier_price_history IS 'Historical price tracking for supplier products to enable cost trend analysis and procurement planning';
COMMENT ON COLUMN public.supplier_price_history.effective_date IS 'Date when this price became effective';
COMMENT ON COLUMN public.supplier_price_history.change_reason IS 'Reason for the price change (e.g., contract renewal, market adjustment)';

-- ============================================================================
-- 9. Trigger Functions for Updated Timestamps
-- ============================================================================

-- Function to update updated_at timestamp
CREATE OR REPLACE FUNCTION update_supply_chain_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Triggers for updated_at on supply chain tables
CREATE TRIGGER trg_suppliers_updated_at
    BEFORE UPDATE ON public.suppliers
    FOR EACH ROW
    EXECUTE FUNCTION update_supply_chain_updated_at();

CREATE TRIGGER trg_supplier_products_updated_at
    BEFORE UPDATE ON public.supplier_products
    FOR EACH ROW
    EXECUTE FUNCTION update_supply_chain_updated_at();

CREATE TRIGGER trg_purchase_orders_updated_at
    BEFORE UPDATE ON public.purchase_orders
    FOR EACH ROW
    EXECUTE FUNCTION update_supply_chain_updated_at();

CREATE TRIGGER trg_purchase_order_items_updated_at
    BEFORE UPDATE ON public.purchase_order_items
    FOR EACH ROW
    EXECUTE FUNCTION update_supply_chain_updated_at();

-- ============================================================================
-- 10. Trigger to Update Purchase Order Item Received Quantity
-- ============================================================================

CREATE OR REPLACE FUNCTION update_poi_received_quantity()
RETURNS TRIGGER AS $$
BEGIN
    IF TG_OP = 'INSERT' OR TG_OP = 'UPDATE' THEN
        UPDATE public.purchase_order_items poi
        SET received_quantity = (
            SELECT COALESCE(SUM(iri.received_quantity), 0)
            FROM public.inventory_receipt_items iri
            WHERE iri.purchase_order_item_id = NEW.purchase_order_item_id
        ),
        updated_at = CURRENT_TIMESTAMP
        WHERE poi.id = NEW.purchase_order_item_id;
        RETURN NEW;
    ELSIF TG_OP = 'DELETE' THEN
        UPDATE public.purchase_order_items poi
        SET received_quantity = (
            SELECT COALESCE(SUM(iri.received_quantity), 0)
            FROM public.inventory_receipt_items iri
            WHERE iri.purchase_order_item_id = OLD.purchase_order_item_id
        ),
        updated_at = CURRENT_TIMESTAMP
        WHERE poi.id = OLD.purchase_order_item_id;
        RETURN OLD;
    END IF;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_update_poi_received_quantity
    AFTER INSERT OR UPDATE OR DELETE ON public.inventory_receipt_items
    FOR EACH ROW
    EXECUTE FUNCTION update_poi_received_quantity();

-- ============================================================================
-- 11. Trigger to Update Purchase Order Status Based on Receiving
-- ============================================================================

CREATE OR REPLACE FUNCTION update_po_status_on_receipt()
RETURNS TRIGGER AS $$
DECLARE
    v_po_id INTEGER;
    v_total_ordered DECIMAL(10, 2);
    v_total_received DECIMAL(10, 2);
BEGIN
    -- Get the purchase order ID
    IF TG_OP = 'DELETE' THEN
        SELECT ir.purchase_order_id INTO v_po_id
        FROM public.inventory_receipts ir
        WHERE ir.id = OLD.inventory_receipt_id;
    ELSE
        SELECT ir.purchase_order_id INTO v_po_id
        FROM public.inventory_receipts ir
        WHERE ir.id = NEW.inventory_receipt_id;
    END IF;

    -- Calculate totals
    SELECT
        COALESCE(SUM(poi.quantity), 0),
        COALESCE(SUM(poi.received_quantity), 0)
    INTO v_total_ordered, v_total_received
    FROM public.purchase_order_items poi
    WHERE poi.purchase_order_id = v_po_id;

    -- Update PO status based on receiving progress
    UPDATE public.purchase_orders
    SET status = CASE
        WHEN v_total_received >= v_total_ordered THEN 'Received'::purchase_order_status
        WHEN v_total_received > 0 THEN 'Partial_Received'::purchase_order_status
        ELSE status  -- Keep current status if nothing received
    END,
    updated_at = CURRENT_TIMESTAMP
    WHERE id = v_po_id
    AND status NOT IN ('Draft', 'Cancelled');  -- Don't update draft or cancelled POs

    RETURN NULL;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_update_po_status_on_receipt
    AFTER INSERT OR UPDATE OR DELETE ON public.inventory_receipt_items
    FOR EACH ROW
    EXECUTE FUNCTION update_po_status_on_receipt();

-- ============================================================================
-- 12. Trigger to Log Price Changes
-- ============================================================================

CREATE OR REPLACE FUNCTION log_supplier_product_price_change()
RETURNS TRIGGER AS $$
BEGIN
    IF OLD.current_price IS DISTINCT FROM NEW.current_price THEN
        INSERT INTO public.supplier_price_history (
            supplier_product_id,
            effective_date,
            price,
            change_reason
        ) VALUES (
            NEW.id,
            CURRENT_DATE,
            NEW.current_price,
            'Price updated from ' || COALESCE(OLD.current_price::TEXT, 'NULL') || ' to ' || COALESCE(NEW.current_price::TEXT, 'NULL')
        );
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_log_supplier_product_price_change
    AFTER UPDATE ON public.supplier_products
    FOR EACH ROW
    EXECUTE FUNCTION log_supplier_product_price_change();

-- ============================================================================
-- 13. Views for Supply Chain Reporting
-- ============================================================================

-- View: Open Purchase Orders Summary
CREATE OR REPLACE VIEW public.vw_open_purchase_orders AS
SELECT
    po.id AS purchase_order_id,
    po.company_id,
    po.po_number,
    s.supplier_name,
    s.supplier_type,
    po.order_date,
    po.expected_delivery_date,
    po.status,
    po.total_amount,
    po.currency,
    po.payment_status,
    COUNT(poi.id) AS line_item_count,
    SUM(poi.quantity) AS total_quantity_ordered,
    SUM(poi.received_quantity) AS total_quantity_received,
    u.name AS created_by
FROM public.purchase_orders po
INNER JOIN public.suppliers s ON po.supplier_id = s.id
LEFT JOIN public.purchase_order_items poi ON poi.purchase_order_id = po.id
LEFT JOIN public.users u ON po.created_by_user_id = u.id
WHERE po.status NOT IN ('Received', 'Cancelled')
GROUP BY po.id, po.company_id, po.po_number, s.supplier_name, s.supplier_type,
         po.order_date, po.expected_delivery_date, po.status, po.total_amount,
         po.currency, po.payment_status, u.name;

COMMENT ON VIEW public.vw_open_purchase_orders IS 'Summary of purchase orders that are not yet fully received or cancelled';

-- View: Supplier Spend Analysis
CREATE OR REPLACE VIEW public.vw_supplier_spend_analysis AS
SELECT
    s.id AS supplier_id,
    s.company_id,
    s.supplier_name,
    s.supplier_type,
    COUNT(DISTINCT po.id) AS total_orders,
    SUM(po.total_amount) AS total_spend,
    AVG(po.total_amount) AS avg_order_value,
    MIN(po.order_date) AS first_order_date,
    MAX(po.order_date) AS last_order_date
FROM public.suppliers s
LEFT JOIN public.purchase_orders po ON po.supplier_id = s.id
    AND po.status NOT IN ('Draft', 'Cancelled')
WHERE s.is_active = TRUE
GROUP BY s.id, s.company_id, s.supplier_name, s.supplier_type;

COMMENT ON VIEW public.vw_supplier_spend_analysis IS 'Spending analysis by supplier for procurement optimization';

-- View: Pending Deliveries
CREATE OR REPLACE VIEW public.vw_pending_deliveries AS
SELECT
    po.id AS purchase_order_id,
    po.company_id,
    po.po_number,
    s.supplier_name,
    po.expected_delivery_date,
    po.status,
    CASE
        WHEN po.expected_delivery_date < CURRENT_DATE THEN 'Overdue'
        WHEN po.expected_delivery_date = CURRENT_DATE THEN 'Due Today'
        WHEN po.expected_delivery_date <= CURRENT_DATE + INTERVAL '7 days' THEN 'Due This Week'
        ELSE 'Upcoming'
    END AS delivery_urgency,
    poi.id AS line_item_id,
    sp.product_name,
    poi.quantity AS ordered_quantity,
    poi.received_quantity,
    (poi.quantity - poi.received_quantity) AS pending_quantity
FROM public.purchase_orders po
INNER JOIN public.suppliers s ON po.supplier_id = s.id
INNER JOIN public.purchase_order_items poi ON poi.purchase_order_id = po.id
INNER JOIN public.supplier_products sp ON poi.supplier_product_id = sp.id
WHERE po.status IN ('Sent', 'Confirmed', 'Partial_Received')
    AND poi.quantity > poi.received_quantity;

COMMENT ON VIEW public.vw_pending_deliveries IS 'Shows pending deliveries with urgency indicators for proactive delivery management';

-- ============================================================================
-- 14. Grant Permissions
-- ============================================================================

GRANT ALL ON TABLE public.suppliers TO postgres;
GRANT ALL ON TABLE public.supplier_products TO postgres;
GRANT ALL ON TABLE public.purchase_orders TO postgres;
GRANT ALL ON TABLE public.purchase_order_items TO postgres;
GRANT ALL ON TABLE public.inventory_receipts TO postgres;
GRANT ALL ON TABLE public.inventory_receipt_items TO postgres;
GRANT ALL ON TABLE public.supplier_price_history TO postgres;

GRANT USAGE, SELECT ON SEQUENCE public.suppliers_id_seq TO postgres;
GRANT USAGE, SELECT ON SEQUENCE public.supplier_products_id_seq TO postgres;
GRANT USAGE, SELECT ON SEQUENCE public.purchase_orders_id_seq TO postgres;
GRANT USAGE, SELECT ON SEQUENCE public.purchase_order_items_id_seq TO postgres;
GRANT USAGE, SELECT ON SEQUENCE public.inventory_receipts_id_seq TO postgres;
GRANT USAGE, SELECT ON SEQUENCE public.inventory_receipt_items_id_seq TO postgres;
GRANT USAGE, SELECT ON SEQUENCE public.supplier_price_history_id_seq TO postgres;

-- ============================================================================
-- Migration Complete
-- ============================================================================
