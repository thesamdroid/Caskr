--
-- Migration: Add performance indexes across all tables
-- Date: 2025-11-10
-- Description: Adds indexes on foreign keys and frequently queried columns for better query performance
--

-- Orders table indexes
CREATE INDEX IF NOT EXISTS idx_orders_company_id ON public.orders(company_id);
CREATE INDEX IF NOT EXISTS idx_orders_owner_id ON public.orders(owner_id);
CREATE INDEX IF NOT EXISTS idx_orders_status_id ON public.orders(status_id);
CREATE INDEX IF NOT EXISTS idx_orders_spirit_type_id ON public.orders(spirit_type_id);
CREATE INDEX IF NOT EXISTS idx_orders_batch_id ON public.orders(batch_id);
CREATE INDEX IF NOT EXISTS idx_orders_created_date ON public.orders(created_date DESC);
CREATE INDEX IF NOT EXISTS idx_orders_updated_date ON public.orders(updated_date DESC);
CREATE INDEX IF NOT EXISTS idx_orders_company_status ON public.orders(company_id, status_id);

-- Tasks table indexes
CREATE INDEX IF NOT EXISTS idx_tasks_order_id ON public.tasks(order_id);
CREATE INDEX IF NOT EXISTS idx_tasks_assignee_id ON public.tasks(assignee_id);
CREATE INDEX IF NOT EXISTS idx_tasks_is_complete ON public.tasks(is_complete);
CREATE INDEX IF NOT EXISTS idx_tasks_due_date ON public.tasks(due_date);
CREATE INDEX IF NOT EXISTS idx_tasks_assignee_incomplete ON public.tasks(assignee_id, is_complete) WHERE is_complete = false;

-- Barrels table indexes
CREATE INDEX IF NOT EXISTS idx_barrels_company_id ON public.barrel(company_id);
CREATE INDEX IF NOT EXISTS idx_barrels_order_id ON public.barrel(order_id);
CREATE INDEX IF NOT EXISTS idx_barrels_rickhouse_id ON public.barrel(rickhouse_id);
CREATE INDEX IF NOT EXISTS idx_barrels_batch_id ON public.barrel(batch_id);
CREATE INDEX IF NOT EXISTS idx_barrels_sku ON public.barrel(sku);
CREATE INDEX IF NOT EXISTS idx_barrels_company_sku ON public.barrel(company_id, sku);

-- Products table indexes
CREATE INDEX IF NOT EXISTS idx_products_owner_id ON public.products(owner_id);
CREATE INDEX IF NOT EXISTS idx_products_created_date ON public.products(created_date DESC);

-- Company table indexes (already added in previous migration)
-- CREATE INDEX IF NOT EXISTS idx_company_is_active ON public.company(is_active);
-- CREATE INDEX IF NOT EXISTS idx_company_name ON public.company(company_name);

-- Rickhouse table indexes
CREATE INDEX IF NOT EXISTS idx_rickhouse_company_id ON public.rickhouse(company_id);

-- Mash Bill table indexes
CREATE INDEX IF NOT EXISTS idx_mash_bill_company_id ON public.mash_bill(company_id);

-- Batch table indexes
CREATE INDEX IF NOT EXISTS idx_batch_company_id ON public.batch(company_id);
CREATE INDEX IF NOT EXISTS idx_batch_mash_bill_id ON public.batch(mash_bill_id);

-- Component table indexes
CREATE INDEX IF NOT EXISTS idx_component_batch_id ON public.component(batch_id);

-- Status Task table indexes
CREATE INDEX IF NOT EXISTS idx_status_task_status_id ON public.status_task(status_id);

-- Add comments documenting the indexes
COMMENT ON INDEX idx_orders_company_status IS 'Composite index for filtering orders by company and status';
COMMENT ON INDEX idx_tasks_assignee_incomplete IS 'Partial index for finding incomplete tasks by assignee';
COMMENT ON INDEX idx_barrels_company_sku IS 'Composite index for barrel lookup by company and SKU';
