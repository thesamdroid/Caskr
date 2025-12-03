using System;
using System.IO;
using Xunit;

namespace Caskr.Server.Tests;

public class DatabaseMigrationTests
{
    [Fact]
    public void QuickBooksMigrationScript_DefinesExpectedTables()
    {
        var solutionRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var scriptPath = Path.Combine(solutionRoot, "Database", "initdb.d", "07-migration-accounting-integrations.sql");

        Assert.True(File.Exists(scriptPath), $"Migration script not found at path '{scriptPath}'.");

        var scriptContents = File.ReadAllText(scriptPath);

        Assert.Contains("CREATE TABLE IF NOT EXISTS public.accounting_integrations", scriptContents);
        Assert.Contains("CREATE TABLE IF NOT EXISTS public.accounting_sync_logs", scriptContents);
        Assert.Contains("CREATE TABLE IF NOT EXISTS public.chart_of_accounts_mapping", scriptContents);
    }

    [Fact]
    public void TtbComplianceMigrationScript_DefinesExpectedArtifacts()
    {
        var solutionRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var scriptPath = Path.Combine(solutionRoot, "Database", "initdb.d", "07-migration-ttb-compliance.sql");

        Assert.True(File.Exists(scriptPath), $"Migration script not found at path '{scriptPath}'.");

        var scriptContents = File.ReadAllText(scriptPath);

        Assert.Contains("CREATE TABLE IF NOT EXISTS public.ttb_monthly_reports", scriptContents);
        Assert.Contains("CREATE INDEX IF NOT EXISTS idx_ttb_monthly_reports_company_month_year", scriptContents);
        Assert.Contains("CREATE TABLE IF NOT EXISTS public.ttb_inventory_snapshots", scriptContents);
        Assert.Contains("CREATE INDEX IF NOT EXISTS idx_ttb_inventory_snapshots_company_date", scriptContents);
        Assert.Contains("CREATE TABLE IF NOT EXISTS public.ttb_transactions", scriptContents);
        Assert.Contains("CREATE INDEX IF NOT EXISTS idx_ttb_transactions_company_date", scriptContents);
    }

    [Fact]
    public void TtbAutoReportsMigrationScript_DefinesScheduleColumns()
    {
        var solutionRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var scriptPath = Path.Combine(solutionRoot, "Database", "initdb.d", "11-migration-ttb-auto-reports.sql");

        Assert.True(File.Exists(scriptPath), $"Migration script not found at path '{scriptPath}'.");

        var scriptContents = File.ReadAllText(scriptPath);

        Assert.Contains("auto_generate_ttb_reports", scriptContents);
        Assert.Contains("ttb_auto_report_cadence", scriptContents);
        Assert.Contains("ttb_auto_report_hour_utc", scriptContents);
        Assert.Contains("ttb_auto_report_day_of_month", scriptContents);
        Assert.Contains("ttb_auto_report_day_of_week", scriptContents);
        Assert.Contains("is_ttb_contact", scriptContents);
    }

    [Fact]
    public void SupplyChainMigrationScript_DefinesExpectedTables()
    {
        var solutionRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var scriptPath = Path.Combine(solutionRoot, "Database", "initdb.d", "16-migration-supply-chain.sql");

        Assert.True(File.Exists(scriptPath), $"Migration script not found at path '{scriptPath}'.");

        var scriptContents = File.ReadAllText(scriptPath);

        // Verify ENUM types
        Assert.Contains("CREATE TYPE supplier_type AS ENUM", scriptContents);
        Assert.Contains("CREATE TYPE purchase_order_status AS ENUM", scriptContents);
        Assert.Contains("CREATE TYPE payment_status AS ENUM", scriptContents);
        Assert.Contains("CREATE TYPE receipt_item_condition AS ENUM", scriptContents);

        // Verify all 7 required tables
        Assert.Contains("CREATE TABLE IF NOT EXISTS public.suppliers", scriptContents);
        Assert.Contains("CREATE TABLE IF NOT EXISTS public.supplier_products", scriptContents);
        Assert.Contains("CREATE TABLE IF NOT EXISTS public.purchase_orders", scriptContents);
        Assert.Contains("CREATE TABLE IF NOT EXISTS public.purchase_order_items", scriptContents);
        Assert.Contains("CREATE TABLE IF NOT EXISTS public.inventory_receipts", scriptContents);
        Assert.Contains("CREATE TABLE IF NOT EXISTS public.inventory_receipt_items", scriptContents);
        Assert.Contains("CREATE TABLE IF NOT EXISTS public.supplier_price_history", scriptContents);
    }

    [Fact]
    public void SupplyChainMigrationScript_DefinesExpectedIndexes()
    {
        var solutionRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var scriptPath = Path.Combine(solutionRoot, "Database", "initdb.d", "16-migration-supply-chain.sql");

        Assert.True(File.Exists(scriptPath), $"Migration script not found at path '{scriptPath}'.");

        var scriptContents = File.ReadAllText(scriptPath);

        // Verify key indexes as specified in acceptance criteria
        Assert.Contains("idx_suppliers_company_supplier_id", scriptContents);
        Assert.Contains("idx_purchase_orders_po_number", scriptContents);
        Assert.Contains("idx_purchase_orders_order_date", scriptContents);
        Assert.Contains("idx_purchase_orders_company_supplier", scriptContents);
    }

    [Fact]
    public void SupplyChainMigrationScript_DefinesExpectedForeignKeys()
    {
        var solutionRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var scriptPath = Path.Combine(solutionRoot, "Database", "initdb.d", "16-migration-supply-chain.sql");

        Assert.True(File.Exists(scriptPath), $"Migration script not found at path '{scriptPath}'.");

        var scriptContents = File.ReadAllText(scriptPath);

        // Verify foreign key constraints
        Assert.Contains("fk_suppliers_company", scriptContents);
        Assert.Contains("fk_supplier_products_supplier", scriptContents);
        Assert.Contains("fk_purchase_orders_company", scriptContents);
        Assert.Contains("fk_purchase_orders_supplier", scriptContents);
        Assert.Contains("fk_purchase_order_items_po", scriptContents);
        Assert.Contains("fk_purchase_order_items_product", scriptContents);
        Assert.Contains("fk_inventory_receipts_po", scriptContents);
        Assert.Contains("fk_inventory_receipt_items_receipt", scriptContents);
        Assert.Contains("fk_supplier_price_history_product", scriptContents);
    }

    [Fact]
    public void SupplyChainMigrationScript_DefinesExpectedViews()
    {
        var solutionRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var scriptPath = Path.Combine(solutionRoot, "Database", "initdb.d", "16-migration-supply-chain.sql");

        Assert.True(File.Exists(scriptPath), $"Migration script not found at path '{scriptPath}'.");

        var scriptContents = File.ReadAllText(scriptPath);

        // Verify views for reporting
        Assert.Contains("vw_open_purchase_orders", scriptContents);
        Assert.Contains("vw_supplier_spend_analysis", scriptContents);
        Assert.Contains("vw_pending_deliveries", scriptContents);
    }

    [Fact]
    public void SupplyChainMigrationScript_DefinesExpectedTriggers()
    {
        var solutionRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var scriptPath = Path.Combine(solutionRoot, "Database", "initdb.d", "16-migration-supply-chain.sql");

        Assert.True(File.Exists(scriptPath), $"Migration script not found at path '{scriptPath}'.");

        var scriptContents = File.ReadAllText(scriptPath);

        // Verify trigger functions and triggers for automation
        Assert.Contains("update_supply_chain_updated_at", scriptContents);
        Assert.Contains("trg_suppliers_updated_at", scriptContents);
        Assert.Contains("trg_supplier_products_updated_at", scriptContents);
        Assert.Contains("trg_purchase_orders_updated_at", scriptContents);
        Assert.Contains("update_poi_received_quantity", scriptContents);
        Assert.Contains("update_po_status_on_receipt", scriptContents);
        Assert.Contains("log_supplier_product_price_change", scriptContents);
    }
}
