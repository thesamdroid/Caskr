--
-- Seed Data: Standard Report Templates (REP-003)
-- Date: 2025-11-30
-- Description: 30 pre-configured standard report templates across Financial, Inventory, Production, and Compliance categories
--

-- First, create the report_templates table if it doesn't exist
CREATE TABLE IF NOT EXISTS public.report_templates (
    id BIGSERIAL PRIMARY KEY,
    company_id INTEGER NOT NULL,
    name VARCHAR(100) NOT NULL,
    description VARCHAR(500),
    data_sources JSONB NOT NULL DEFAULT '[]',
    columns JSONB NOT NULL DEFAULT '[]',
    filters JSONB,
    groupings JSONB,
    sort_order JSONB,
    default_page_size INTEGER NOT NULL DEFAULT 50,
    is_active BOOLEAN NOT NULL DEFAULT true,
    is_system_template BOOLEAN NOT NULL DEFAULT false,
    created_by_user_id INTEGER NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ,
    CONSTRAINT fk_report_templates_company
        FOREIGN KEY (company_id) REFERENCES public.company(id) ON DELETE CASCADE,
    CONSTRAINT fk_report_templates_created_by
        FOREIGN KEY (created_by_user_id) REFERENCES public.users(id) ON DELETE RESTRICT
);

-- Create indexes if they don't exist
CREATE INDEX IF NOT EXISTS idx_report_templates_company_id ON public.report_templates(company_id);
CREATE INDEX IF NOT EXISTS idx_report_templates_created_by ON public.report_templates(created_by_user_id);
CREATE INDEX IF NOT EXISTS idx_report_templates_is_active ON public.report_templates(is_active);

-- Insert standard report templates for all existing companies
-- Uses a system user (id=125) and loops through all companies

DO $$
DECLARE
    v_company_id INTEGER;
    v_user_id INTEGER := 125; -- Super Admin user
BEGIN
    -- Loop through each company to create standard templates
    FOR v_company_id IN SELECT id FROM public.company LOOP

        -- ==========================================
        -- FINANCIAL REPORTS (10)
        -- ==========================================

        -- 1. Inventory Valuation by Batch
        INSERT INTO public.report_templates (
            company_id, name, description, data_sources, columns, filters, groupings, sort_order,
            default_page_size, is_active, is_system_template, created_by_user_id
        ) VALUES (
            v_company_id,
            'Inventory Valuation by Batch',
            'Total value of aging inventory grouped by batch, showing barrel counts and estimated values',
            '["barrels", "batches", "orders"]',
            '["batches.id as batch_id", "orders.name as order_name", "COUNT(barrels.id) as barrel_count", "SUM(orders.quantity) as total_quantity"]',
            '{"filter": "barrels.company_id = @companyId", "defaultParameters": {}}',
            '["batches.id", "orders.name"]',
            '[{"column": "batches.id", "direction": "asc"}]',
            50, true, true, v_user_id
        ) ON CONFLICT DO NOTHING;

        -- 2. Cost of Goods Sold by Month
        INSERT INTO public.report_templates (
            company_id, name, description, data_sources, columns, filters, groupings, sort_order,
            default_page_size, is_active, is_system_template, created_by_user_id
        ) VALUES (
            v_company_id,
            'Cost of Goods Sold by Month',
            'Monthly COGS analysis from invoice line items showing sold products and costs',
            '["invoices", "invoice_line_items"]',
            '["DATE_TRUNC(''month'', invoices.invoice_date) as month", "SUM(invoice_line_items.quantity * invoice_line_items.unit_price) as total_cogs", "COUNT(DISTINCT invoices.id) as invoice_count"]',
            '{"filter": "invoices.company_id = @companyId AND invoices.invoice_date >= @startDate AND invoices.invoice_date <= @endDate", "defaultParameters": {"startDate": "2024-01-01", "endDate": "2024-12-31"}}',
            '["DATE_TRUNC(''month'', invoices.invoice_date)"]',
            '[{"column": "month", "direction": "asc"}]',
            12, true, true, v_user_id
        ) ON CONFLICT DO NOTHING;

        -- 3. Revenue by Product Type
        INSERT INTO public.report_templates (
            company_id, name, description, data_sources, columns, filters, groupings, sort_order,
            default_page_size, is_active, is_system_template, created_by_user_id
        ) VALUES (
            v_company_id,
            'Revenue by Product Type',
            'Total revenue breakdown by spirit type showing sales performance per category',
            '["orders", "spirit_types", "invoices"]',
            '["spirit_types.name as spirit_type", "COUNT(orders.id) as order_count", "SUM(orders.quantity) as total_units", "SUM(invoices.total_amount) as total_revenue"]',
            '{"filter": "orders.company_id = @companyId AND invoices.invoice_date >= @startDate", "defaultParameters": {"startDate": "2024-01-01"}}',
            '["spirit_types.name"]',
            '[{"column": "total_revenue", "direction": "desc"}]',
            20, true, true, v_user_id
        ) ON CONFLICT DO NOTHING;

        -- 4. Profit Margin by Batch
        INSERT INTO public.report_templates (
            company_id, name, description, data_sources, columns, filters, groupings, sort_order,
            default_page_size, is_active, is_system_template, created_by_user_id
        ) VALUES (
            v_company_id,
            'Profit Margin by Batch',
            'Profit margin analysis per batch comparing production costs to sale revenue',
            '["batches", "orders", "invoices"]',
            '["batches.id as batch_id", "orders.name as order_name", "SUM(invoices.total_amount) as revenue", "SUM(invoices.subtotal_amount) as subtotal", "COUNT(DISTINCT invoices.id) as sales_count"]',
            '{"filter": "batches.company_id = @companyId", "defaultParameters": {}}',
            '["batches.id", "orders.name"]',
            '[{"column": "revenue", "direction": "desc"}]',
            50, true, true, v_user_id
        ) ON CONFLICT DO NOTHING;

        -- 5. Work in Progress Valuation
        INSERT INTO public.report_templates (
            company_id, name, description, data_sources, columns, filters, groupings, sort_order,
            default_page_size, is_active, is_system_template, created_by_user_id
        ) VALUES (
            v_company_id,
            'Work in Progress Valuation',
            'Value of barrels currently in production or aging process',
            '["barrels", "batches", "orders", "rickhouses"]',
            '["orders.name as order_name", "rickhouses.name as rickhouse", "COUNT(barrels.id) as barrel_count", "batches.id as batch_id"]',
            '{"filter": "barrels.company_id = @companyId", "defaultParameters": {}}',
            '["orders.name", "rickhouses.name", "batches.id"]',
            '[{"column": "barrel_count", "direction": "desc"}]',
            50, true, true, v_user_id
        ) ON CONFLICT DO NOTHING;

        -- 6. Barrel Cost Analysis
        INSERT INTO public.report_templates (
            company_id, name, description, data_sources, columns, filters, groupings, sort_order,
            default_page_size, is_active, is_system_template, created_by_user_id
        ) VALUES (
            v_company_id,
            'Barrel Cost Analysis',
            'Cooper costs analysis over time tracking barrel acquisition and storage expenses',
            '["barrels", "rickhouses", "orders"]',
            '["rickhouses.name as rickhouse", "COUNT(barrels.id) as barrel_count", "orders.name as order_name"]',
            '{"filter": "barrels.company_id = @companyId", "defaultParameters": {}}',
            '["rickhouses.name", "orders.name"]',
            '[{"column": "barrel_count", "direction": "desc"}]',
            50, true, true, v_user_id
        ) ON CONFLICT DO NOTHING;

        -- 7. Aging Inventory Summary
        INSERT INTO public.report_templates (
            company_id, name, description, data_sources, columns, filters, groupings, sort_order,
            default_page_size, is_active, is_system_template, created_by_user_id
        ) VALUES (
            v_company_id,
            'Aging Inventory Summary',
            'Inventory value breakdown by age bracket showing barrel distribution over time',
            '["barrels", "orders", "rickhouses"]',
            '["barrels.sku as barrel_sku", "orders.name as order_name", "rickhouses.name as rickhouse", "orders.created_date as fill_date"]',
            '{"filter": "barrels.company_id = @companyId", "defaultParameters": {}}',
            NULL,
            '[{"column": "fill_date", "direction": "asc"}]',
            100, true, true, v_user_id
        ) ON CONFLICT DO NOTHING;

        -- 8. Excise Tax Liability Report
        INSERT INTO public.report_templates (
            company_id, name, description, data_sources, columns, filters, groupings, sort_order,
            default_page_size, is_active, is_system_template, created_by_user_id
        ) VALUES (
            v_company_id,
            'Excise Tax Liability Report',
            'Federal excise tax liability summary based on tax determinations and proof gallons',
            '["ttb_tax_determinations", "orders"]',
            '["ttb_tax_determinations.determination_date as date", "orders.name as order_name", "ttb_tax_determinations.proof_gallons as proof_gallons", "ttb_tax_determinations.tax_rate as tax_rate", "ttb_tax_determinations.tax_amount as tax_amount", "ttb_tax_determinations.paid_date as paid_date"]',
            '{"filter": "ttb_tax_determinations.company_id = @companyId AND ttb_tax_determinations.determination_date >= @startDate", "defaultParameters": {"startDate": "2024-01-01"}}',
            NULL,
            '[{"column": "date", "direction": "desc"}]',
            50, true, true, v_user_id
        ) ON CONFLICT DO NOTHING;

        -- 9. Monthly Production Costs
        INSERT INTO public.report_templates (
            company_id, name, description, data_sources, columns, filters, groupings, sort_order,
            default_page_size, is_active, is_system_template, created_by_user_id
        ) VALUES (
            v_company_id,
            'Monthly Production Costs',
            'Monthly breakdown of production-related costs and resource utilization',
            '["orders", "batches"]',
            '["DATE_TRUNC(''month'', orders.created_date) as month", "COUNT(DISTINCT batches.id) as batch_count", "SUM(orders.quantity) as total_quantity", "COUNT(DISTINCT orders.id) as order_count"]',
            '{"filter": "orders.company_id = @companyId AND orders.created_date >= @startDate", "defaultParameters": {"startDate": "2024-01-01"}}',
            '["DATE_TRUNC(''month'', orders.created_date)"]',
            '[{"column": "month", "direction": "asc"}]',
            12, true, true, v_user_id
        ) ON CONFLICT DO NOTHING;

        -- 10. Customer Revenue Ranking
        INSERT INTO public.report_templates (
            company_id, name, description, data_sources, columns, filters, groupings, sort_order,
            default_page_size, is_active, is_system_template, created_by_user_id
        ) VALUES (
            v_company_id,
            'Customer Revenue Ranking',
            'Top customers ranked by total revenue generated with order counts',
            '["invoices"]',
            '["invoices.customer_name as customer", "COUNT(invoices.id) as order_count", "SUM(invoices.total_amount) as total_revenue", "AVG(invoices.total_amount) as avg_order_value"]',
            '{"filter": "invoices.company_id = @companyId AND invoices.invoice_date >= @startDate", "defaultParameters": {"startDate": "2024-01-01"}}',
            '["invoices.customer_name"]',
            '[{"column": "total_revenue", "direction": "desc"}]',
            50, true, true, v_user_id
        ) ON CONFLICT DO NOTHING;

        -- ==========================================
        -- INVENTORY REPORTS (10)
        -- ==========================================

        -- 11. Current Barrel Inventory by Status
        INSERT INTO public.report_templates (
            company_id, name, description, data_sources, columns, filters, groupings, sort_order,
            default_page_size, is_active, is_system_template, created_by_user_id
        ) VALUES (
            v_company_id,
            'Current Barrel Inventory by Status',
            'Complete barrel inventory grouped by current order status',
            '["barrels", "orders", "status"]',
            '["status.name as status", "COUNT(barrels.id) as barrel_count", "orders.name as order_name"]',
            '{"filter": "barrels.company_id = @companyId", "defaultParameters": {}}',
            '["status.name", "orders.name"]',
            '[{"column": "barrel_count", "direction": "desc"}]',
            50, true, true, v_user_id
        ) ON CONFLICT DO NOTHING;

        -- 12. Barrel Aging Report
        INSERT INTO public.report_templates (
            company_id, name, description, data_sources, columns, filters, groupings, sort_order,
            default_page_size, is_active, is_system_template, created_by_user_id
        ) VALUES (
            v_company_id,
            'Barrel Aging Report',
            'Barrels grouped by age range showing distribution across aging periods',
            '["barrels", "orders", "rickhouses"]',
            '["barrels.sku as barrel_sku", "barrels.id as barrel_id", "orders.name as order_name", "orders.created_date as fill_date", "rickhouses.name as location"]',
            '{"filter": "barrels.company_id = @companyId", "defaultParameters": {}}',
            NULL,
            '[{"column": "fill_date", "direction": "asc"}]',
            100, true, true, v_user_id
        ) ON CONFLICT DO NOTHING;

        -- 13. Warehouse Utilization by Location
        INSERT INTO public.report_templates (
            company_id, name, description, data_sources, columns, filters, groupings, sort_order,
            default_page_size, is_active, is_system_template, created_by_user_id
        ) VALUES (
            v_company_id,
            'Warehouse Utilization by Location',
            'Rickhouse capacity utilization showing barrel counts per location',
            '["rickhouses", "barrels"]',
            '["rickhouses.name as rickhouse", "rickhouses.address as address", "COUNT(barrels.id) as barrel_count"]',
            '{"filter": "rickhouses.company_id = @companyId", "defaultParameters": {}}',
            '["rickhouses.name", "rickhouses.address"]',
            '[{"column": "barrel_count", "direction": "desc"}]',
            20, true, true, v_user_id
        ) ON CONFLICT DO NOTHING;

        -- 14. Low Stock Alert
        INSERT INTO public.report_templates (
            company_id, name, description, data_sources, columns, filters, groupings, sort_order,
            default_page_size, is_active, is_system_template, created_by_user_id
        ) VALUES (
            v_company_id,
            'Low Stock Alert',
            'Products and batches with inventory below configurable threshold levels',
            '["batches", "orders", "barrels"]',
            '["batches.id as batch_id", "orders.name as order_name", "COUNT(barrels.id) as barrel_count"]',
            '{"filter": "batches.company_id = @companyId", "defaultParameters": {"threshold": 10}}',
            '["batches.id", "orders.name"]',
            '[{"column": "barrel_count", "direction": "asc"}]',
            50, true, true, v_user_id
        ) ON CONFLICT DO NOTHING;

        -- 15. Barrel Movement History
        INSERT INTO public.report_templates (
            company_id, name, description, data_sources, columns, filters, groupings, sort_order,
            default_page_size, is_active, is_system_template, created_by_user_id
        ) VALUES (
            v_company_id,
            'Barrel Movement History',
            'Historical log of barrel transfers and location changes',
            '["ttb_transactions", "barrels"]',
            '["ttb_transactions.transaction_date as date", "ttb_transactions.transaction_type as type", "ttb_transactions.source_entity_type as entity_type", "ttb_transactions.proof_gallons as proof_gallons", "ttb_transactions.notes as notes"]',
            '{"filter": "ttb_transactions.company_id = @companyId AND ttb_transactions.transaction_type IN (''Transfer_In'', ''Transfer_Out'')", "defaultParameters": {}}',
            NULL,
            '[{"column": "date", "direction": "desc"}]',
            100, true, true, v_user_id
        ) ON CONFLICT DO NOTHING;

        -- 16. Inventory by Proof Range
        INSERT INTO public.report_templates (
            company_id, name, description, data_sources, columns, filters, groupings, sort_order,
            default_page_size, is_active, is_system_template, created_by_user_id
        ) VALUES (
            v_company_id,
            'Inventory by Proof Range',
            'Barrel inventory categorized by proof levels showing distribution',
            '["barrels", "ttb_gauge_records"]',
            '["barrels.sku as barrel_sku", "ttb_gauge_records.proof as proof", "ttb_gauge_records.wine_gallons as wine_gallons", "ttb_gauge_records.proof_gallons as proof_gallons", "ttb_gauge_records.gauge_date as gauge_date"]',
            '{"filter": "barrels.company_id = @companyId", "defaultParameters": {}}',
            NULL,
            '[{"column": "proof", "direction": "desc"}]',
            100, true, true, v_user_id
        ) ON CONFLICT DO NOTHING;

        -- 17. Batch Yield Analysis
        INSERT INTO public.report_templates (
            company_id, name, description, data_sources, columns, filters, groupings, sort_order,
            default_page_size, is_active, is_system_template, created_by_user_id
        ) VALUES (
            v_company_id,
            'Batch Yield Analysis',
            'Comparison of expected vs actual yield per batch showing production efficiency',
            '["batches", "orders", "barrels"]',
            '["batches.id as batch_id", "orders.name as order_name", "orders.quantity as expected_quantity", "COUNT(barrels.id) as actual_barrels"]',
            '{"filter": "batches.company_id = @companyId", "defaultParameters": {}}',
            '["batches.id", "orders.name", "orders.quantity"]',
            '[{"column": "batch_id", "direction": "asc"}]',
            50, true, true, v_user_id
        ) ON CONFLICT DO NOTHING;

        -- 18. Evaporation Loss Report
        INSERT INTO public.report_templates (
            company_id, name, description, data_sources, columns, filters, groupings, sort_order,
            default_page_size, is_active, is_system_template, created_by_user_id
        ) VALUES (
            v_company_id,
            'Evaporation Loss Report',
            'Angels share tracking - evaporation losses recorded over time per barrel',
            '["ttb_transactions"]',
            '["ttb_transactions.transaction_date as date", "ttb_transactions.product_type as product_type", "ttb_transactions.proof_gallons as proof_gallons_lost", "ttb_transactions.wine_gallons as wine_gallons_lost", "ttb_transactions.notes as notes"]',
            '{"filter": "ttb_transactions.company_id = @companyId AND ttb_transactions.transaction_type = ''Loss''", "defaultParameters": {}}',
            NULL,
            '[{"column": "date", "direction": "desc"}]',
            100, true, true, v_user_id
        ) ON CONFLICT DO NOTHING;

        -- 19. Barrels Due for Dumping
        INSERT INTO public.report_templates (
            company_id, name, description, data_sources, columns, filters, groupings, sort_order,
            default_page_size, is_active, is_system_template, created_by_user_id
        ) VALUES (
            v_company_id,
            'Barrels Due for Dumping',
            'Barrels that have reached target age threshold and are ready for dumping',
            '["barrels", "orders", "rickhouses", "batches"]',
            '["barrels.sku as barrel_sku", "barrels.id as barrel_id", "orders.name as order_name", "orders.created_date as fill_date", "rickhouses.name as location", "batches.id as batch_id"]',
            '{"filter": "barrels.company_id = @companyId AND orders.created_date <= @targetDate", "defaultParameters": {"targetDate": "2022-01-01"}}',
            NULL,
            '[{"column": "fill_date", "direction": "asc"}]',
            100, true, true, v_user_id
        ) ON CONFLICT DO NOTHING;

        -- 20. Multi-Warehouse Inventory Comparison
        INSERT INTO public.report_templates (
            company_id, name, description, data_sources, columns, filters, groupings, sort_order,
            default_page_size, is_active, is_system_template, created_by_user_id
        ) VALUES (
            v_company_id,
            'Multi-Warehouse Inventory Comparison',
            'Side-by-side comparison of inventory levels across all warehouse locations',
            '["rickhouses", "barrels", "batches"]',
            '["rickhouses.name as rickhouse", "COUNT(barrels.id) as total_barrels", "COUNT(DISTINCT batches.id) as batch_count"]',
            '{"filter": "rickhouses.company_id = @companyId", "defaultParameters": {}}',
            '["rickhouses.name"]',
            '[{"column": "total_barrels", "direction": "desc"}]',
            20, true, true, v_user_id
        ) ON CONFLICT DO NOTHING;

        -- ==========================================
        -- PRODUCTION REPORTS (5)
        -- ==========================================

        -- 21. Monthly Production Volume
        INSERT INTO public.report_templates (
            company_id, name, description, data_sources, columns, filters, groupings, sort_order,
            default_page_size, is_active, is_system_template, created_by_user_id
        ) VALUES (
            v_company_id,
            'Monthly Production Volume',
            'Proof gallons produced per month showing production output trends',
            '["ttb_transactions"]',
            '["DATE_TRUNC(''month'', ttb_transactions.transaction_date) as month", "SUM(ttb_transactions.proof_gallons) as total_proof_gallons", "SUM(ttb_transactions.wine_gallons) as total_wine_gallons", "COUNT(*) as transaction_count"]',
            '{"filter": "ttb_transactions.company_id = @companyId AND ttb_transactions.transaction_type = ''Production'' AND ttb_transactions.transaction_date >= @startDate", "defaultParameters": {"startDate": "2024-01-01"}}',
            '["DATE_TRUNC(''month'', ttb_transactions.transaction_date)"]',
            '[{"column": "month", "direction": "asc"}]',
            12, true, true, v_user_id
        ) ON CONFLICT DO NOTHING;

        -- 22. Batch Efficiency Report
        INSERT INTO public.report_templates (
            company_id, name, description, data_sources, columns, filters, groupings, sort_order,
            default_page_size, is_active, is_system_template, created_by_user_id
        ) VALUES (
            v_company_id,
            'Batch Efficiency Report',
            'Time analysis from mash to barrel showing production cycle efficiency',
            '["batches", "mash_bills", "orders", "barrels"]',
            '["batches.id as batch_id", "mash_bills.name as mash_bill", "orders.name as order_name", "orders.created_date as start_date", "COUNT(barrels.id) as barrel_count"]',
            '{"filter": "batches.company_id = @companyId", "defaultParameters": {}}',
            '["batches.id", "mash_bills.name", "orders.name", "orders.created_date"]',
            '[{"column": "start_date", "direction": "desc"}]',
            50, true, true, v_user_id
        ) ON CONFLICT DO NOTHING;

        -- 23. Equipment Utilization
        INSERT INTO public.report_templates (
            company_id, name, description, data_sources, columns, filters, groupings, sort_order,
            default_page_size, is_active, is_system_template, created_by_user_id
        ) VALUES (
            v_company_id,
            'Equipment Utilization',
            'Rickhouse and storage equipment usage metrics and capacity analysis',
            '["rickhouses", "barrels"]',
            '["rickhouses.name as rickhouse", "rickhouses.address as address", "COUNT(barrels.id) as barrels_stored"]',
            '{"filter": "rickhouses.company_id = @companyId", "defaultParameters": {}}',
            '["rickhouses.name", "rickhouses.address"]',
            '[{"column": "barrels_stored", "direction": "desc"}]',
            20, true, true, v_user_id
        ) ON CONFLICT DO NOTHING;

        -- 24. Quality Control Metrics
        INSERT INTO public.report_templates (
            company_id, name, description, data_sources, columns, filters, groupings, sort_order,
            default_page_size, is_active, is_system_template, created_by_user_id
        ) VALUES (
            v_company_id,
            'Quality Control Metrics',
            'Gauge record analysis showing proof and quality measurements over time',
            '["barrels", "ttb_gauge_records", "users"]',
            '["ttb_gauge_records.gauge_date as date", "ttb_gauge_records.gauge_type as gauge_type", "barrels.sku as barrel_sku", "ttb_gauge_records.proof as proof", "ttb_gauge_records.temperature as temperature", "users.name as gauged_by"]',
            '{"filter": "barrels.company_id = @companyId AND ttb_gauge_records.gauge_date >= @startDate", "defaultParameters": {"startDate": "2024-01-01"}}',
            NULL,
            '[{"column": "date", "direction": "desc"}]',
            100, true, true, v_user_id
        ) ON CONFLICT DO NOTHING;

        -- 25. Mash Bill Usage Analysis
        INSERT INTO public.report_templates (
            company_id, name, description, data_sources, columns, filters, groupings, sort_order,
            default_page_size, is_active, is_system_template, created_by_user_id
        ) VALUES (
            v_company_id,
            'Mash Bill Usage Analysis',
            'Breakdown of mash bill recipes used in production showing popularity and batch counts',
            '["mash_bills", "batches", "orders"]',
            '["mash_bills.name as mash_bill", "mash_bills.id as mash_bill_id", "COUNT(DISTINCT batches.id) as batch_count", "SUM(orders.quantity) as total_quantity"]',
            '{"filter": "mash_bills.company_id = @companyId", "defaultParameters": {}}',
            '["mash_bills.name", "mash_bills.id"]',
            '[{"column": "batch_count", "direction": "desc"}]',
            20, true, true, v_user_id
        ) ON CONFLICT DO NOTHING;

        -- ==========================================
        -- COMPLIANCE REPORTS (5)
        -- ==========================================

        -- 26. TTB Monthly Summary
        INSERT INTO public.report_templates (
            company_id, name, description, data_sources, columns, filters, groupings, sort_order,
            default_page_size, is_active, is_system_template, created_by_user_id
        ) VALUES (
            v_company_id,
            'TTB Monthly Summary',
            'Summary matching TTB Form 5110.28 requirements - monthly operations report',
            '["ttb_monthly_reports", "users"]',
            '["ttb_monthly_reports.report_month as month", "ttb_monthly_reports.report_year as year", "ttb_monthly_reports.status as status", "ttb_monthly_reports.form_type as form_type", "ttb_monthly_reports.generated_at as generated_at", "ttb_monthly_reports.submitted_at as submitted_at", "ttb_monthly_reports.ttb_confirmation_number as confirmation_number", "users.name as created_by"]',
            '{"filter": "ttb_monthly_reports.company_id = @companyId AND ttb_monthly_reports.report_year = @year", "defaultParameters": {"year": 2024}}',
            NULL,
            '[{"column": "year", "direction": "desc"}, {"column": "month", "direction": "desc"}]',
            12, true, true, v_user_id
        ) ON CONFLICT DO NOTHING;

        -- 27. Transfer Documentation Log
        INSERT INTO public.report_templates (
            company_id, name, description, data_sources, columns, filters, groupings, sort_order,
            default_page_size, is_active, is_system_template, created_by_user_id
        ) VALUES (
            v_company_id,
            'Transfer Documentation Log',
            'Complete log of product transfers for TTB compliance documentation',
            '["ttb_transactions"]',
            '["ttb_transactions.transaction_date as date", "ttb_transactions.transaction_type as type", "ttb_transactions.product_type as product", "ttb_transactions.spirits_type as spirits_type", "ttb_transactions.proof_gallons as proof_gallons", "ttb_transactions.wine_gallons as wine_gallons", "ttb_transactions.source_entity_type as source_type", "ttb_transactions.notes as notes"]',
            '{"filter": "ttb_transactions.company_id = @companyId AND ttb_transactions.transaction_type IN (''Transfer_In'', ''Transfer_Out'') AND ttb_transactions.transaction_date >= @startDate", "defaultParameters": {"startDate": "2024-01-01"}}',
            NULL,
            '[{"column": "date", "direction": "desc"}]',
            100, true, true, v_user_id
        ) ON CONFLICT DO NOTHING;

        -- 28. Gauge Record Summary
        INSERT INTO public.report_templates (
            company_id, name, description, data_sources, columns, filters, groupings, sort_order,
            default_page_size, is_active, is_system_template, created_by_user_id
        ) VALUES (
            v_company_id,
            'Gauge Record Summary',
            'Summary of all gauge records for TTB compliance showing barrel measurements',
            '["barrels", "ttb_gauge_records", "users"]',
            '["ttb_gauge_records.id as record_id", "barrels.sku as barrel_sku", "ttb_gauge_records.gauge_date as date", "ttb_gauge_records.gauge_type as type", "ttb_gauge_records.proof as proof", "ttb_gauge_records.temperature as temp", "ttb_gauge_records.wine_gallons as wine_gal", "ttb_gauge_records.proof_gallons as proof_gal", "users.name as gauged_by", "ttb_gauge_records.notes as notes"]',
            '{"filter": "barrels.company_id = @companyId AND ttb_gauge_records.gauge_date >= @startDate", "defaultParameters": {"startDate": "2024-01-01"}}',
            NULL,
            '[{"column": "date", "direction": "desc"}]',
            100, true, true, v_user_id
        ) ON CONFLICT DO NOTHING;

        -- 29. Tax Determination History
        INSERT INTO public.report_templates (
            company_id, name, description, data_sources, columns, filters, groupings, sort_order,
            default_page_size, is_active, is_system_template, created_by_user_id
        ) VALUES (
            v_company_id,
            'Tax Determination History',
            'Complete history of tax determinations for excise tax tracking and reporting',
            '["ttb_tax_determinations", "orders"]',
            '["ttb_tax_determinations.id as determination_id", "orders.name as order_name", "ttb_tax_determinations.determination_date as date", "ttb_tax_determinations.proof_gallons as proof_gallons", "ttb_tax_determinations.tax_rate as tax_rate", "ttb_tax_determinations.tax_amount as tax_amount", "ttb_tax_determinations.paid_date as paid_date", "ttb_tax_determinations.payment_reference as payment_ref", "ttb_tax_determinations.notes as notes"]',
            '{"filter": "ttb_tax_determinations.company_id = @companyId AND ttb_tax_determinations.determination_date >= @startDate", "defaultParameters": {"startDate": "2024-01-01"}}',
            NULL,
            '[{"column": "date", "direction": "desc"}]',
            100, true, true, v_user_id
        ) ON CONFLICT DO NOTHING;

        -- 30. Audit Trail Report
        INSERT INTO public.report_templates (
            company_id, name, description, data_sources, columns, filters, groupings, sort_order,
            default_page_size, is_active, is_system_template, created_by_user_id
        ) VALUES (
            v_company_id,
            'Audit Trail Report',
            'Complete audit trail of TTB-relevant changes for compliance verification',
            '["ttb_audit_logs", "users"]',
            '["ttb_audit_logs.change_timestamp as timestamp", "ttb_audit_logs.entity_type as entity_type", "ttb_audit_logs.entity_id as entity_id", "ttb_audit_logs.action as action", "users.name as changed_by", "ttb_audit_logs.change_description as description", "ttb_audit_logs.ip_address as ip_address"]',
            '{"filter": "ttb_audit_logs.company_id = @companyId AND ttb_audit_logs.change_timestamp >= @startDate", "defaultParameters": {"startDate": "2024-01-01"}}',
            NULL,
            '[{"column": "timestamp", "direction": "desc"}]',
            100, true, true, v_user_id
        ) ON CONFLICT DO NOTHING;

    END LOOP;
END $$;

-- Sync the sequence for report_templates
SELECT setval('report_templates_id_seq', COALESCE((SELECT MAX(id) FROM public.report_templates), 1), true);

-- Log successful completion
DO $$
BEGIN
    RAISE NOTICE 'Successfully seeded % standard report templates',
        (SELECT COUNT(*) FROM public.report_templates WHERE is_system_template = true);
END $$;
