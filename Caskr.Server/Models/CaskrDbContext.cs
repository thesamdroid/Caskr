using System;
using Microsoft.EntityFrameworkCore;

namespace Caskr.server.Models;

public partial class CaskrDbContext : DbContext
{
    public CaskrDbContext()
    {
    }

    public CaskrDbContext(DbContextOptions<CaskrDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Order> Orders { get; set; } = null!;

    public virtual DbSet<Product> Products { get; set; } = null!;

    public virtual DbSet<Status> Statuses { get; set; } = null!;

    public virtual DbSet<StatusTask> StatusTasks { get; set; } = null!;

    public virtual DbSet<User> Users { get; set; } = null!;

    public virtual DbSet<UserType> UserTypes { get; set; } = null!;

    public virtual DbSet<OrderTask> OrderTasks { get; set; } = null!;

    public virtual DbSet<Company> Companies { get; set; } = null!;

    public virtual DbSet<SpiritType> SpiritTypes { get; set; } = null!;

    public virtual DbSet<Batch> Batches { get; set; } = null!;

    public virtual DbSet<MashBill> MashBills { get; set; } = null!;

    public virtual DbSet<Component> Components { get; set; } = null!;

    public virtual DbSet<Rickhouse> Rickhouses { get; set; } = null!;

    public virtual DbSet<Barrel> Barrels { get; set; } = null!;

    public virtual DbSet<AccountingIntegration> AccountingIntegrations { get; set; } = null!;

    public virtual DbSet<AccountingSyncLog> AccountingSyncLogs { get; set; } = null!;

    public virtual DbSet<ChartOfAccountsMapping> ChartOfAccountsMappings { get; set; } = null!;

    public virtual DbSet<AccountingSyncPreference> AccountingSyncPreferences { get; set; } = null!;

    public virtual DbSet<Invoice> Invoices { get; set; } = null!;

    public virtual DbSet<InvoiceLineItem> InvoiceLineItems { get; set; } = null!;

    public virtual DbSet<InvoiceTax> InvoiceTaxes { get; set; } = null!;

    public virtual DbSet<TtbMonthlyReport> TtbMonthlyReports { get; set; } = null!;

    public virtual DbSet<TtbInventorySnapshot> TtbInventorySnapshots { get; set; } = null!;

    public virtual DbSet<TtbTransaction> TtbTransactions { get; set; } = null!;

    public virtual DbSet<TtbGaugeRecord> TtbGaugeRecords { get; set; } = null!;

    public virtual DbSet<TtbTaxDetermination> TtbTaxDeterminations { get; set; } = null!;

    public virtual DbSet<TtbAuditLog> TtbAuditLogs { get; set; } = null!;

    public virtual DbSet<ReportTemplate> ReportTemplates { get; set; } = null!;

    public virtual DbSet<SavedReport> SavedReports { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Orders_pkey");

            entity.ToTable("orders");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("nextval('\"Orders_id_seq\"'::regclass)")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_date");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_date");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.OwnerId)
                .HasDefaultValueSql("nextval('\"Orders_owner_id_seq\"'::regclass)")
                .HasColumnName("owner_id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.StatusId)
                .HasDefaultValueSql("nextval('\"Orders_status_id_seq\"'::regclass)")
                .HasColumnName("status_id");
            entity.Property(e => e.SpiritTypeId)
                .HasDefaultValueSql("nextval('\"Orders_spirit_type_id_seq\"'::regclass)")
                .HasColumnName("spirit_type_id");

            entity.Property(e => e.BatchId).HasColumnName("batch_id");

            entity.Property(e => e.Quantity).HasColumnName("quantity");

            entity.Property(e => e.InvoiceId).HasColumnName("invoice_id");

            entity.HasOne(d => d.Owner).WithMany(p => p.Orders)
                .HasForeignKey(d => d.OwnerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_ownerid_userid");

            entity.HasOne(d => d.Status).WithMany(p => p.Orders)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_statusid_stautusid");

            entity.HasOne(d => d.SpiritType).WithMany(p => p.Orders)
                .HasForeignKey(d => d.SpiritTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_spirit_typeid_spirit_typeid");

            entity.HasOne(d => d.Batch)
                .WithMany()
                .HasForeignKey(d => new { d.BatchId, d.CompanyId });

            entity.HasOne(d => d.Invoice)
                .WithMany()
                .HasForeignKey(d => d.InvoiceId)
                .HasConstraintName("fk_orders_invoice");
        });

        modelBuilder.Entity<OrderTask>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Tasks_pkey");

            entity.ToTable("tasks");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("nextval('\"Tasks_id_seq\"'::regclass)")
                .HasColumnName("id");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.AssigneeId).HasColumnName("assignee_id");
            entity.Property(e => e.IsComplete)
                .HasDefaultValue(false)
                .HasColumnName("is_complete");
            entity.Property(e => e.DueDate).HasColumnName("due_date");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAdd()
                .HasColumnName("created_date");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_date");
            entity.Property(e => e.CompletedAt)
                .HasColumnName("completed_date");

            entity.HasOne(d => d.Order).WithMany(p => p.Tasks)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_tasks_orderid");

            entity.HasOne(d => d.Assignee).WithMany()
                .HasForeignKey(d => d.AssigneeId)
                .HasConstraintName("fk_tasks_assigneeid");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("products_pkey");

            entity.ToTable("products");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.OwnerId).HasColumnName("owner_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_date");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_date");

            entity.HasOne(d => d.Owner).WithMany(p => p.Products)
                .HasForeignKey(d => d.OwnerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_owner_users");
        });

        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Company_pkey");

            entity.ToTable("company");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("nextval('\"Company_id_seq\"'::regclass)")
                .HasColumnName("id");
            entity.Property(e => e.CompanyName).HasColumnName("company_name");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_date");
            entity.Property(e => e.RenewalDate).HasColumnName("renewal_date");
            entity.Property(e => e.AddressLine1).HasColumnName("address_line1");
            entity.Property(e => e.AddressLine2).HasColumnName("address_line2");
            entity.Property(e => e.City).HasColumnName("city");
            entity.Property(e => e.State).HasColumnName("state");
            entity.Property(e => e.PostalCode).HasColumnName("postal_code");
            entity.Property(e => e.Country).HasColumnName("country");
            entity.Property(e => e.PhoneNumber).HasColumnName("phone_number");
            entity.Property(e => e.Website).HasColumnName("website");
            entity.Property(e => e.TtbPermitNumber).HasColumnName("ttb_permit_number");
            entity.Property(e => e.AutoGenerateTtbReports)
                .HasDefaultValue(false)
                .HasColumnName("auto_generate_ttb_reports");
            entity.Property(e => e.TtbAutoReportCadence)
                .HasConversion<string>()
                .HasDefaultValue(TtbAutoReportCadence.Monthly)
                .HasColumnName("ttb_auto_report_cadence");
            entity.Property(e => e.TtbAutoReportHourUtc)
                .HasDefaultValue(6)
                .HasColumnName("ttb_auto_report_hour_utc");
            entity.Property(e => e.TtbAutoReportDayOfMonth)
                .HasDefaultValue(1)
                .HasColumnName("ttb_auto_report_day_of_month");
            entity.Property(e => e.TtbAutoReportDayOfWeek)
                .HasConversion<string>()
                .HasDefaultValue(DayOfWeek.Monday)
                .HasColumnName("ttb_auto_report_day_of_week");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.AnnualProductionProofGallons)
                .HasColumnType("decimal(12,2)")
                .HasColumnName("annual_production_proof_gallons");
            entity.Property(e => e.IsEligibleForReducedExciseTaxRate)
                .HasDefaultValue(true)
                .HasColumnName("is_eligible_for_reduced_excise_tax_rate");
            entity.Property(e => e.ExciseTaxEligibilityNotes)
                .HasColumnName("excise_tax_eligibility_notes");
        });

        modelBuilder.Entity<Status>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Status_pkey");

            entity.ToTable("status");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("nextval('\"Status_id_seq\"'::regclass)")
                .HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
        });

        modelBuilder.Entity<SpiritType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("SpiritType_pkey");

            entity.ToTable("spirit_type");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("nextval('\"SpiritType_id_seq\"'::regclass)")
                .HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
        });

        modelBuilder.Entity<StatusTask>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("StatusTask_pkey");

            entity.ToTable("status_task");

            entity.Property(e => e.Id)
                .HasDefaultValueSql(@"nextval('""StatusTask_id_seq""'::regclass)")
                .HasColumnName("id");
            entity.Property(e => e.StatusId).HasColumnName("status_id");
            entity.Property(e => e.Name).HasColumnName("name");

            entity.HasOne(d => d.Status).WithMany(p => p.StatusTasks)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_statustask_statusid");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Users_pkey");

            entity.ToTable("users");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("nextval('\"Users_id_seq\"'::regclass)")
                .HasColumnName("id");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.UserTypeId).HasColumnName("user_type_id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.IsPrimaryContact).HasColumnName("is_primary_contact");
            entity.Property(e => e.KeycloakUserId).HasColumnName("keycloak_user_id");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.IsTtbContact)
                .HasDefaultValue(false)
                .HasColumnName("is_ttb_contact");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAdd()
                .HasColumnName("created_at");
            entity.Property(e => e.LastLoginAt).HasColumnName("last_login_at");

            entity.HasOne(d => d.UserType).WithMany(p => p.Users)
                .HasForeignKey(d => d.UserTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_user_userstatus");
            entity.HasOne(d => d.Company).WithMany(p => p.Users)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_user_company");
        });

        modelBuilder.Entity<UserType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_type_pkey");

            entity.ToTable("user_type");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Description).HasColumnName("description");
        });

        modelBuilder.Entity<MashBill>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("MashBill_pkey");

            entity.ToTable("mash_bill");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.ComponentIds).HasColumnName("component_ids");
        });

        modelBuilder.Entity<Batch>(entity =>
        {
            entity.HasKey(e => new { e.Id, e.CompanyId }).HasName("Batch_pkey");

            entity.ToTable("batch");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.MashBillId).HasColumnName("mash_bill_id");

            entity.HasOne(d => d.MashBill)
                .WithMany()
                .HasForeignKey(d => d.MashBillId);
        });

        modelBuilder.Entity<Component>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Component_pkey");

            entity.ToTable("component");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BatchId).HasColumnName("batch_id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Percentage).HasColumnName("percentage");
        });

        modelBuilder.Entity<Rickhouse>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Rickhouse_pkey");

            entity.ToTable("rickhouse");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("nextval('\"Rickhouse_id_seq\"'::regclass)")
                .HasColumnName("id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Address).HasColumnName("address");

            entity.HasOne(d => d.Company).WithMany(p => p.Rickhouses)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_rickhouse_company");
        });

        modelBuilder.Entity<Barrel>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Barrel_pkey");

            entity.ToTable("barrel");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("nextval('\"Barrel_id_seq\"'::regclass)")
                .HasColumnName("id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.Sku).HasColumnName("sku");
            entity.Property(e => e.BatchId).HasColumnName("batch_id");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.RickhouseId).HasColumnName("rickhouse_id");

            entity.HasOne(d => d.Company).WithMany(p => p.Barrels)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_barrel_company");

            entity.HasOne(d => d.Order).WithMany(p => p.Barrels)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_barrel_order");

            entity.HasOne(d => d.Rickhouse).WithMany(p => p.Barrels)
                .HasForeignKey(d => d.RickhouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_barrel_rickhouse");

            entity.HasOne(d => d.Batch).WithMany()
                .HasForeignKey(d => new { d.BatchId, d.CompanyId })
                .HasConstraintName("fk_barrel_batch");
        });

        modelBuilder.Entity<AccountingIntegration>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("accounting_integrations_pkey");

            entity.ToTable("accounting_integrations");

            entity.HasIndex(e => e.CompanyId).HasDatabaseName("idx_accounting_integrations_company_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.Provider)
                .HasConversion<string>()
                .HasColumnName("provider");
            entity.Property(e => e.AccessTokenEncrypted).HasColumnName("access_token_encrypted");
            entity.Property(e => e.RefreshTokenEncrypted).HasColumnName("refresh_token_encrypted");
            entity.Property(e => e.RealmId).HasColumnName("realm_id");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Company).WithMany(p => p.AccountingIntegrations)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_accounting_integrations_company");
        });

        modelBuilder.Entity<AccountingSyncLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("accounting_sync_logs_pkey");

            entity.ToTable("accounting_sync_logs");

            entity.HasIndex(e => e.CompanyId).HasDatabaseName("idx_accounting_sync_logs_company_id");
            entity.HasIndex(e => e.SyncStatus).HasDatabaseName("idx_accounting_sync_logs_sync_status");
            entity.HasIndex(e => e.SyncedAt).HasDatabaseName("idx_accounting_sync_logs_synced_at");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.EntityType).HasColumnName("entity_type");
            entity.Property(e => e.EntityId).HasColumnName("entity_id");
            entity.Property(e => e.ExternalEntityId).HasColumnName("external_entity_id");
            entity.Property(e => e.SyncStatus)
                .HasConversion<string>()
                .HasColumnName("sync_status");
            entity.Property(e => e.ErrorMessage).HasColumnName("error_message");
            entity.Property(e => e.RetryCount)
                .HasDefaultValue(0)
                .HasColumnName("retry_count");
            entity.Property(e => e.SyncedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("synced_at");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Company).WithMany(p => p.AccountingSyncLogs)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_accounting_sync_logs_company");
        });

        modelBuilder.Entity<ChartOfAccountsMapping>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("chart_of_accounts_mapping_pkey");

            entity.ToTable("chart_of_accounts_mapping");

            entity.HasIndex(e => e.CompanyId).HasDatabaseName("idx_chart_of_accounts_mapping_company_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.CaskrAccountType)
                .HasConversion<string>()
                .HasColumnName("caskr_account_type");
            entity.Property(e => e.QboAccountId).HasColumnName("qbo_account_id");
            entity.Property(e => e.QboAccountName).HasColumnName("qbo_account_name");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Company).WithMany(p => p.ChartOfAccountsMappings)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_chart_of_accounts_mapping_company");
        });

        modelBuilder.Entity<AccountingSyncPreference>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("accounting_sync_preferences_pkey");

            entity.ToTable("accounting_sync_preferences");

            entity.HasIndex(e => e.CompanyId)
                .HasDatabaseName("idx_accounting_sync_preferences_company_id");

            entity.HasIndex(e => new { e.CompanyId, e.Provider })
                .IsUnique()
                .HasDatabaseName("ux_accounting_sync_preferences_company_provider");

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("nextval('\"accounting_sync_preferences_id_seq\"'::regclass)");

            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.Provider)
                .HasConversion<string>()
                .HasColumnName("provider");
            entity.Property(e => e.AutoSyncInvoices)
                .HasDefaultValue(false)
                .HasColumnName("auto_sync_invoices");
            entity.Property(e => e.AutoSyncCogs)
                .HasDefaultValue(false)
                .HasColumnName("auto_sync_cogs");
            entity.Property(e => e.SyncFrequency)
                .HasMaxLength(32)
                .HasDefaultValue("Manual")
                .HasColumnName("sync_frequency");
            entity.Property(e => e.LastSyncAt)
                .HasColumnName("last_sync_at");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Company)
                .WithMany()
                .HasForeignKey(d => d.CompanyId)
                .HasConstraintName("fk_accounting_sync_preferences_company");
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("invoices_pkey");

            entity.ToTable("invoices");

            entity.HasIndex(e => e.CompanyId).HasDatabaseName("idx_invoices_company_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.InvoiceNumber).HasColumnName("invoice_number");
            entity.Property(e => e.CustomerName).HasColumnName("customer_name");
            entity.Property(e => e.CustomerEmail).HasColumnName("customer_email");
            entity.Property(e => e.CustomerPhone).HasColumnName("customer_phone");
            entity.Property(e => e.CustomerAddressLine1).HasColumnName("customer_address_line1");
            entity.Property(e => e.CustomerAddressLine2).HasColumnName("customer_address_line2");
            entity.Property(e => e.CustomerCity).HasColumnName("customer_city");
            entity.Property(e => e.CustomerState).HasColumnName("customer_state");
            entity.Property(e => e.CustomerPostalCode).HasColumnName("customer_postal_code");
            entity.Property(e => e.CustomerCountry).HasColumnName("customer_country");
            entity.Property(e => e.CurrencyCode).HasColumnName("currency_code");
            entity.Property(e => e.InvoiceDate).HasColumnName("invoice_date");
            entity.Property(e => e.DueDate).HasColumnName("due_date");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.SubtotalAmount).HasColumnName("subtotal_amount");
            entity.Property(e => e.TotalAmount).HasColumnName("total_amount");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(d => d.Company).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_invoices_company");
        });

        modelBuilder.Entity<InvoiceLineItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("invoice_line_items_pkey");

            entity.ToTable("invoice_line_items");

            entity.HasIndex(e => e.InvoiceId).HasDatabaseName("idx_invoice_line_items_invoice_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.InvoiceId).HasColumnName("invoice_id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.UnitPrice).HasColumnName("unit_price");
            entity.Property(e => e.IsTaxable).HasColumnName("is_taxable");
            entity.Property(e => e.AccountType)
                .HasConversion<string>()
                .HasColumnName("account_type");
            entity.Property(e => e.ProductCode).HasColumnName("product_code");
            entity.Property(e => e.ProductName).HasColumnName("product_name");
            entity.Property(e => e.DiscountAmount).HasColumnName("discount_amount");

            entity.HasOne(d => d.Invoice).WithMany(p => p.LineItems)
                .HasForeignKey(d => d.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_invoice_line_items_invoice");
        });

        modelBuilder.Entity<InvoiceTax>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("invoice_taxes_pkey");

            entity.ToTable("invoice_taxes");

            entity.HasIndex(e => e.InvoiceId).HasDatabaseName("idx_invoice_taxes_invoice_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.InvoiceId).HasColumnName("invoice_id");
            entity.Property(e => e.TaxName).HasColumnName("tax_name");
            entity.Property(e => e.TaxCode).HasColumnName("tax_code");
            entity.Property(e => e.Rate).HasColumnName("rate");
            entity.Property(e => e.Amount).HasColumnName("amount");

            entity.HasOne(d => d.Invoice).WithMany(p => p.Taxes)
                .HasForeignKey(d => d.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_invoice_taxes_invoice");
        });

        modelBuilder.Entity<TtbMonthlyReport>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ttb_monthly_reports_pkey");

            entity.ToTable("ttb_monthly_reports");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.ReportMonth).HasColumnName("report_month");
            entity.Property(e => e.ReportYear).HasColumnName("report_year");
            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasColumnName("status");
            entity.Property(e => e.FormType)
                .HasConversion<string>()
                .HasColumnName("form_type");
            entity.Property(e => e.GeneratedAt).HasColumnName("generated_at");
            entity.Property(e => e.SubmittedAt).HasColumnName("submitted_at");
            entity.Property(e => e.TtbConfirmationNumber).HasColumnName("ttb_confirmation_number");
            entity.Property(e => e.PdfPath).HasColumnName("pdf_path");
            entity.Property(e => e.ValidationErrors).HasColumnName("validation_errors");
            entity.Property(e => e.ValidationWarnings).HasColumnName("validation_warnings");
            entity.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id");

            entity.HasOne(d => d.Company)
                .WithMany(p => p.TtbMonthlyReports)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_ttb_monthly_reports_company");

            entity.HasOne(d => d.CreatedByUser)
                .WithMany(p => p.CreatedTtbMonthlyReports)
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_ttb_monthly_reports_created_by");

            entity.Property(e => e.SubmittedForReviewByUserId).HasColumnName("submitted_for_review_by_user_id");
            entity.Property(e => e.SubmittedForReviewAt).HasColumnName("submitted_for_review_at");
            entity.Property(e => e.ReviewedByUserId).HasColumnName("reviewed_by_user_id");
            entity.Property(e => e.ReviewedAt).HasColumnName("reviewed_at");
            entity.Property(e => e.ApprovedByUserId).HasColumnName("approved_by_user_id");
            entity.Property(e => e.ApprovedAt).HasColumnName("approved_at");
            entity.Property(e => e.ReviewNotes).HasColumnName("review_notes");

            entity.HasOne(d => d.SubmittedForReviewByUser)
                .WithMany()
                .HasForeignKey(d => d.SubmittedForReviewByUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_ttb_monthly_reports_submitted_for_review_by");

            entity.HasOne(d => d.ReviewedByUser)
                .WithMany()
                .HasForeignKey(d => d.ReviewedByUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_ttb_monthly_reports_reviewed_by");

            entity.HasOne(d => d.ApprovedByUser)
                .WithMany()
                .HasForeignKey(d => d.ApprovedByUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_ttb_monthly_reports_approved_by");
        });

        modelBuilder.Entity<TtbInventorySnapshot>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ttb_inventory_snapshots_pkey");

            entity.ToTable("ttb_inventory_snapshots");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.SnapshotDate).HasColumnName("snapshot_date");
            entity.Property(e => e.ProductType).HasColumnName("product_type");
            entity.Property(e => e.SpiritsType)
                .HasConversion<string>()
                .HasColumnName("spirits_type");
            entity.Property(e => e.ProofGallons)
                .HasColumnName("proof_gallons");
            entity.Property(e => e.WineGallons)
                .HasColumnName("wine_gallons");
            entity.Property(e => e.TaxStatus)
                .HasConversion<string>()
                .HasColumnName("tax_status");

            entity.HasOne(d => d.Company)
                .WithMany(p => p.TtbInventorySnapshots)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_ttb_inventory_snapshots_company");
        });

        modelBuilder.Entity<TtbTransaction>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ttb_transactions_pkey");

            entity.ToTable("ttb_transactions");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.TransactionDate).HasColumnName("transaction_date");
            entity.Property(e => e.TransactionType)
                .HasConversion<string>()
                .HasColumnName("transaction_type");
            entity.Property(e => e.ProductType).HasColumnName("product_type");
            entity.Property(e => e.SpiritsType)
                .HasConversion<string>()
                .HasColumnName("spirits_type");
            entity.Property(e => e.ProofGallons)
                .HasColumnName("proof_gallons");
            entity.Property(e => e.WineGallons)
                .HasColumnName("wine_gallons");
            entity.Property(e => e.SourceEntityType).HasColumnName("source_entity_type");
            entity.Property(e => e.SourceEntityId).HasColumnName("source_entity_id");
            entity.Property(e => e.Notes).HasColumnName("notes");

            entity.HasOne(d => d.Company)
                .WithMany(p => p.TtbTransactions)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_ttb_transactions_company");
        });

        modelBuilder.Entity<TtbGaugeRecord>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ttb_gauge_records_pkey");

            entity.ToTable("ttb_gauge_records");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BarrelId).HasColumnName("barrel_id");
            entity.Property(e => e.GaugeDate).HasColumnName("gauge_date");
            entity.Property(e => e.GaugeType)
                .HasConversion<string>()
                .HasColumnName("gauge_type");
            entity.Property(e => e.Proof)
                .HasColumnType("decimal(5,2)")
                .HasColumnName("proof");
            entity.Property(e => e.Temperature)
                .HasColumnType("decimal(5,2)")
                .HasColumnName("temperature");
            entity.Property(e => e.WineGallons)
                .HasColumnType("decimal(10,2)")
                .HasColumnName("wine_gallons");
            entity.Property(e => e.ProofGallons)
                .HasColumnType("decimal(10,2)")
                .HasColumnName("proof_gallons");
            entity.Property(e => e.GaugedByUserId).HasColumnName("gauged_by_user_id");
            entity.Property(e => e.Notes).HasColumnName("notes");

            entity.HasOne(d => d.Barrel)
                .WithMany()
                .HasForeignKey(d => d.BarrelId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_ttb_gauge_records_barrel");

            entity.HasOne(d => d.GaugedByUser)
                .WithMany()
                .HasForeignKey(d => d.GaugedByUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_ttb_gauge_records_user");
        });

        modelBuilder.Entity<TtbTaxDetermination>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ttb_tax_determinations_pkey");

            entity.ToTable("ttb_tax_determinations");

            entity.HasIndex(e => e.CompanyId).HasDatabaseName("idx_ttb_tax_determinations_company_id");
            entity.HasIndex(e => e.OrderId).HasDatabaseName("idx_ttb_tax_determinations_order_id");
            entity.HasIndex(e => e.DeterminationDate).HasDatabaseName("idx_ttb_tax_determinations_date");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.ProofGallons)
                .HasColumnType("decimal(10,2)")
                .HasColumnName("proof_gallons");
            entity.Property(e => e.TaxRate)
                .HasColumnType("decimal(10,2)")
                .HasColumnName("tax_rate");
            entity.Property(e => e.TaxAmount)
                .HasColumnType("decimal(10,2)")
                .HasColumnName("tax_amount");
            entity.Property(e => e.DeterminationDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("determination_date");
            entity.Property(e => e.PaidDate).HasColumnName("paid_date");
            entity.Property(e => e.PaymentReference).HasColumnName("payment_reference");
            entity.Property(e => e.QuickBooksJournalEntryId).HasColumnName("quickbooks_journal_entry_id");
            entity.Property(e => e.Notes).HasColumnName("notes");

            entity.HasOne(d => d.Company)
                .WithMany()
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_ttb_tax_determinations_company");

            entity.HasOne(d => d.Order)
                .WithMany()
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_ttb_tax_determinations_order");
        });

        modelBuilder.Entity<TtbAuditLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ttb_audit_logs_pkey");

            entity.ToTable("ttb_audit_logs");

            entity.HasIndex(e => e.CompanyId).HasDatabaseName("idx_ttb_audit_logs_company_id");
            entity.HasIndex(e => e.EntityType).HasDatabaseName("idx_ttb_audit_logs_entity_type");
            entity.HasIndex(e => e.EntityId).HasDatabaseName("idx_ttb_audit_logs_entity_id");
            entity.HasIndex(e => e.ChangeTimestamp).HasDatabaseName("idx_ttb_audit_logs_timestamp");
            entity.HasIndex(e => e.ChangedByUserId).HasDatabaseName("idx_ttb_audit_logs_user_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.EntityType)
                .HasMaxLength(50)
                .HasColumnName("entity_type");
            entity.Property(e => e.EntityId).HasColumnName("entity_id");
            entity.Property(e => e.Action)
                .HasConversion<string>()
                .HasColumnName("action");
            entity.Property(e => e.ChangedByUserId).HasColumnName("changed_by_user_id");
            entity.Property(e => e.ChangeTimestamp)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("change_timestamp");
            entity.Property(e => e.OldValues)
                .HasColumnType("jsonb")
                .HasColumnName("old_values");
            entity.Property(e => e.NewValues)
                .HasColumnType("jsonb")
                .HasColumnName("new_values");
            entity.Property(e => e.IpAddress)
                .HasMaxLength(45)
                .HasColumnName("ip_address");
            entity.Property(e => e.UserAgent).HasColumnName("user_agent");
            entity.Property(e => e.ChangeDescription).HasColumnName("change_description");

            entity.HasOne(d => d.Company)
                .WithMany()
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_ttb_audit_logs_company");

            entity.HasOne(d => d.ChangedByUser)
                .WithMany()
                .HasForeignKey(d => d.ChangedByUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_ttb_audit_logs_user");
        });

        modelBuilder.Entity<ReportTemplate>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("report_templates_pkey");

            entity.ToTable("report_templates");

            entity.HasIndex(e => e.CompanyId).HasDatabaseName("idx_report_templates_company_id");
            entity.HasIndex(e => e.CreatedByUserId).HasDatabaseName("idx_report_templates_created_by");
            entity.HasIndex(e => e.IsActive).HasDatabaseName("idx_report_templates_is_active");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");
            entity.Property(e => e.DataSources)
                .HasColumnType("jsonb")
                .HasDefaultValue("[]")
                .HasColumnName("data_sources");
            entity.Property(e => e.Columns)
                .HasColumnType("jsonb")
                .HasDefaultValue("[]")
                .HasColumnName("columns");
            entity.Property(e => e.Filters)
                .HasColumnType("jsonb")
                .HasColumnName("filters");
            entity.Property(e => e.Groupings)
                .HasColumnType("jsonb")
                .HasColumnName("groupings");
            entity.Property(e => e.SortOrder)
                .HasColumnType("jsonb")
                .HasColumnName("sort_order");
            entity.Property(e => e.DefaultPageSize)
                .HasDefaultValue(50)
                .HasColumnName("default_page_size");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.IsSystemTemplate)
                .HasDefaultValue(false)
                .HasColumnName("is_system_template");
            entity.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(d => d.Company)
                .WithMany()
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_report_templates_company");

            entity.HasOne(d => d.CreatedByUser)
                .WithMany()
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_report_templates_created_by");
        });

        modelBuilder.Entity<SavedReport>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("saved_reports_pkey");

            entity.ToTable("saved_reports");

            entity.HasIndex(e => e.CompanyId).HasDatabaseName("idx_saved_reports_company_id");
            entity.HasIndex(e => e.UserId).HasDatabaseName("idx_saved_reports_user_id");
            entity.HasIndex(e => e.ReportTemplateId).HasDatabaseName("idx_saved_reports_template_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.ReportTemplateId).HasColumnName("report_template_id");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .HasColumnName("description");
            entity.Property(e => e.FilterValues)
                .HasColumnType("jsonb")
                .HasColumnName("filter_values");
            entity.Property(e => e.IsFavorite)
                .HasDefaultValue(false)
                .HasColumnName("is_favorite");
            entity.Property(e => e.LastRunAt).HasColumnName("last_run_at");
            entity.Property(e => e.RunCount)
                .HasDefaultValue(0)
                .HasColumnName("run_count");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(d => d.Company)
                .WithMany()
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_saved_reports_company");

            entity.HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_saved_reports_user");

            entity.HasOne(d => d.ReportTemplate)
                .WithMany()
                .HasForeignKey(d => d.ReportTemplateId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_saved_reports_template");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
