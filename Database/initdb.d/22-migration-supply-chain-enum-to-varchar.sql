--
-- Migration: Convert Supply Chain PostgreSQL enums to VARCHAR
-- Date: 2025-12-05
-- Description: Converts PostgreSQL enum types to VARCHAR for better EF Core compatibility
--

-- Convert supplier_type column from enum to VARCHAR
ALTER TABLE public.suppliers
    ALTER COLUMN supplier_type TYPE VARCHAR(50) USING supplier_type::text;

-- Convert purchase_order_status column from enum to VARCHAR
ALTER TABLE public.purchase_orders
    ALTER COLUMN status TYPE VARCHAR(50) USING status::text;

-- Convert payment_status column from enum to VARCHAR
ALTER TABLE public.purchase_orders
    ALTER COLUMN payment_status TYPE VARCHAR(50) USING payment_status::text;

-- Convert receipt_item_condition column from enum to VARCHAR
ALTER TABLE public.inventory_receipt_items
    ALTER COLUMN condition TYPE VARCHAR(50) USING condition::text;

-- Drop the enum types (they are no longer needed)
DROP TYPE IF EXISTS supplier_type;
DROP TYPE IF EXISTS purchase_order_status;
DROP TYPE IF EXISTS payment_status;
DROP TYPE IF EXISTS receipt_item_condition;

-- Add comments
COMMENT ON COLUMN public.suppliers.supplier_type IS 'Category of supplier: Grain, Cooperage, Bottles, Labels, Chemicals, Equipment, or Other (stored as VARCHAR for EF Core compatibility)';
COMMENT ON COLUMN public.purchase_orders.status IS 'Current state of the PO: Draft, Sent, Confirmed, Partial_Received, Received, or Cancelled (stored as VARCHAR for EF Core compatibility)';
COMMENT ON COLUMN public.purchase_orders.payment_status IS 'Payment state: Unpaid, Partial, or Paid (stored as VARCHAR for EF Core compatibility)';
COMMENT ON COLUMN public.inventory_receipt_items.condition IS 'Quality status of received goods: Good, Damaged, or Partial (stored as VARCHAR for EF Core compatibility)';
