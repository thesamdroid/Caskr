--
-- Migration: Add extended fields to orders table
-- Date: 2025-12-05
-- Description: Adds invoice_id, fulfillment_warehouse_id, customer_id, and Salesforce CRM fields to orders table
--

-- Add invoice reference
ALTER TABLE public.orders
ADD COLUMN IF NOT EXISTS invoice_id INTEGER;

-- Add fulfillment warehouse reference
ALTER TABLE public.orders
ADD COLUMN IF NOT EXISTS fulfillment_warehouse_id INTEGER;

-- Add customer reference (for CRM integration)
ALTER TABLE public.orders
ADD COLUMN IF NOT EXISTS customer_id BIGINT;

-- Add Salesforce CRM fields
ALTER TABLE public.orders
ADD COLUMN IF NOT EXISTS salesforce_opportunity_id VARCHAR(18);

ALTER TABLE public.orders
ADD COLUMN IF NOT EXISTS salesforce_last_sync_at TIMESTAMP WITH TIME ZONE;

ALTER TABLE public.orders
ADD COLUMN IF NOT EXISTS order_date TIMESTAMP WITH TIME ZONE;

ALTER TABLE public.orders
ADD COLUMN IF NOT EXISTS total_amount DECIMAL(18,2);

ALTER TABLE public.orders
ADD COLUMN IF NOT EXISTS order_notes TEXT;

-- Add foreign key constraints (only if they don't already exist)
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.table_constraints
        WHERE constraint_name = 'fk_orders_invoice' AND table_name = 'orders'
    ) THEN
        ALTER TABLE public.orders
        ADD CONSTRAINT fk_orders_invoice
        FOREIGN KEY (invoice_id) REFERENCES public.invoices(id) ON DELETE SET NULL;
    END IF;
EXCEPTION
    WHEN undefined_table THEN
        NULL; -- invoices table may not exist yet
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.table_constraints
        WHERE constraint_name = 'fk_orders_fulfillment_warehouse' AND table_name = 'orders'
    ) THEN
        ALTER TABLE public.orders
        ADD CONSTRAINT fk_orders_fulfillment_warehouse
        FOREIGN KEY (fulfillment_warehouse_id) REFERENCES public.warehouses(id) ON DELETE SET NULL;
    END IF;
EXCEPTION
    WHEN undefined_table THEN
        NULL; -- warehouses table may not exist yet
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.table_constraints
        WHERE constraint_name = 'fk_orders_customer' AND table_name = 'orders'
    ) THEN
        ALTER TABLE public.orders
        ADD CONSTRAINT fk_orders_customer
        FOREIGN KEY (customer_id) REFERENCES public.customers(id) ON DELETE SET NULL;
    END IF;
EXCEPTION
    WHEN undefined_table THEN
        NULL; -- customers table may not exist yet
END $$;

-- Add indexes for common queries
CREATE INDEX IF NOT EXISTS idx_orders_invoice_id ON public.orders(invoice_id);
CREATE INDEX IF NOT EXISTS idx_orders_fulfillment_warehouse_id ON public.orders(fulfillment_warehouse_id);
CREATE INDEX IF NOT EXISTS idx_orders_customer_id ON public.orders(customer_id);
CREATE INDEX IF NOT EXISTS idx_orders_salesforce_opportunity_id ON public.orders(salesforce_opportunity_id);

-- Add comments for documentation
COMMENT ON COLUMN public.orders.invoice_id IS 'Reference to the invoice for this order';
COMMENT ON COLUMN public.orders.fulfillment_warehouse_id IS 'Warehouse that will fulfill this order (source of barrels)';
COMMENT ON COLUMN public.orders.customer_id IS 'Reference to the customer record (for CRM integration)';
COMMENT ON COLUMN public.orders.salesforce_opportunity_id IS 'Salesforce Opportunity ID (18 character format)';
COMMENT ON COLUMN public.orders.salesforce_last_sync_at IS 'Timestamp of last Salesforce sync';
COMMENT ON COLUMN public.orders.order_date IS 'Order date from Salesforce Opportunity CloseDate';
COMMENT ON COLUMN public.orders.total_amount IS 'Total amount from Salesforce Opportunity Amount';
COMMENT ON COLUMN public.orders.order_notes IS 'Order notes from Salesforce Opportunity Name/Description';
