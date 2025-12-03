using System;
using Microsoft.EntityFrameworkCore;
using Caskr.server.Models.Portal;
using Caskr.server.Models.Crm;
using Caskr.server.Models.SupplyChain;

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

    public virtual DbSet<Warehouse> Warehouses { get; set; } = null!;

    public virtual DbSet<InterWarehouseTransfer> InterWarehouseTransfers { get; set; } = null!;

    public virtual DbSet<BarrelTransfer> BarrelTransfers { get; set; } = null!;

    public virtual DbSet<WarehouseCapacitySnapshot> WarehouseCapacitySnapshots { get; set; } = null!;

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

    public virtual DbSet<WebhookSubscription> WebhookSubscriptions { get; set; } = null!;

    public virtual DbSet<WebhookDelivery> WebhookDeliveries { get; set; } = null!;

    // Portal entities (customer-facing portal)
    public virtual DbSet<PortalUser> PortalUsers { get; set; } = null!;

    public virtual DbSet<CaskOwnership> CaskOwnerships { get; set; } = null!;

    public virtual DbSet<PortalAccessLog> PortalAccessLogs { get; set; } = null!;

    public virtual DbSet<PortalDocument> PortalDocuments { get; set; } = null!;

    public virtual DbSet<PortalNotification> PortalNotifications { get; set; } = null!;

    // CRM Integration entities (CRM-001)
    public virtual DbSet<Customer> Customers { get; set; } = null!;

    public virtual DbSet<CrmIntegration> CrmIntegrations { get; set; } = null!;

    public virtual DbSet<CrmSyncLog> CrmSyncLogs { get; set; } = null!;

    public virtual DbSet<CrmEntityMapping> CrmEntityMappings { get; set; } = null!;

    public virtual DbSet<CrmFieldMapping> CrmFieldMappings { get; set; } = null!;

    public virtual DbSet<CrmSyncPreference> CrmSyncPreferences { get; set; } = null!;

    public virtual DbSet<CrmSyncConflict> CrmSyncConflicts { get; set; } = null!;

    // Supply Chain entities (SCM-001, SCM-002)
    public virtual DbSet<SupplyChain.Supplier> Suppliers { get; set; } = null!;

    public virtual DbSet<SupplyChain.SupplierProduct> SupplierProducts { get; set; } = null!;

    public virtual DbSet<SupplyChain.PurchaseOrder> PurchaseOrders { get; set; } = null!;

    public virtual DbSet<SupplyChain.PurchaseOrderItem> PurchaseOrderItems { get; set; } = null!;

    public virtual DbSet<SupplyChain.InventoryReceipt> InventoryReceipts { get; set; } = null!;

    public virtual DbSet<SupplyChain.InventoryReceiptItem> InventoryReceiptItems { get; set; } = null!;

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

            entity.Property(e => e.FulfillmentWarehouseId).HasColumnName("fulfillment_warehouse_id");

            entity.HasIndex(e => e.FulfillmentWarehouseId).HasDatabaseName("idx_orders_fulfillment_warehouse_id");

            entity.HasOne(d => d.FulfillmentWarehouse).WithMany(p => p.FulfillmentOrders)
                .HasForeignKey(d => d.FulfillmentWarehouseId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_orders_fulfillment_warehouse");

            // CRM Integration fields (CRM-001)
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.SalesforceOpportunityId)
                .HasMaxLength(18)
                .HasColumnName("salesforce_opportunity_id");
            entity.Property(e => e.SalesforceLastSyncAt).HasColumnName("salesforce_last_sync_at");
            entity.Property(e => e.OrderDate).HasColumnName("order_date");
            entity.Property(e => e.TotalAmount)
                .HasColumnType("decimal(12,2)")
                .HasColumnName("total_amount");
            entity.Property(e => e.OrderNotes).HasColumnName("order_notes");

            entity.HasIndex(e => e.CustomerId).HasDatabaseName("idx_orders_customer_id");
            entity.HasIndex(e => e.SalesforceOpportunityId)
                .HasDatabaseName("idx_orders_salesforce_opportunity_id");

            entity.HasOne(d => d.Customer)
                .WithMany()
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_orders_customer");
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
            entity.Property(e => e.WarehouseId).HasColumnName("warehouse_id");

            entity.HasIndex(e => e.WarehouseId).HasDatabaseName("idx_barrel_warehouse_id");

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

            entity.HasOne(d => d.Warehouse).WithMany(p => p.Barrels)
                .HasForeignKey(d => d.WarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_barrel_warehouse");
        });

        // Warehouse configuration
        modelBuilder.Entity<Warehouse>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("warehouses_pkey");

            entity.ToTable("warehouses");

            entity.HasIndex(e => e.CompanyId).HasDatabaseName("idx_warehouses_company_id");
            entity.HasIndex(e => e.IsActive).HasDatabaseName("idx_warehouses_is_active");
            entity.HasIndex(e => e.WarehouseType).HasDatabaseName("idx_warehouses_type");
            entity.HasIndex(e => new { e.CompanyId, e.Name })
                .IsUnique()
                .HasDatabaseName("uq_warehouses_company_name");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .HasColumnName("name");
            entity.Property(e => e.WarehouseType)
                .HasConversion<string>()
                .HasColumnName("warehouse_type");
            entity.Property(e => e.AddressLine1)
                .HasMaxLength(255)
                .HasColumnName("address_line1");
            entity.Property(e => e.AddressLine2)
                .HasMaxLength(255)
                .HasColumnName("address_line2");
            entity.Property(e => e.City)
                .HasMaxLength(100)
                .HasColumnName("city");
            entity.Property(e => e.State)
                .HasMaxLength(100)
                .HasColumnName("state");
            entity.Property(e => e.PostalCode)
                .HasMaxLength(20)
                .HasColumnName("postal_code");
            entity.Property(e => e.Country)
                .HasMaxLength(100)
                .HasDefaultValue("USA")
                .HasColumnName("country");
            entity.Property(e => e.TotalCapacity)
                .HasDefaultValue(0)
                .HasColumnName("total_capacity");
            entity.Property(e => e.LengthFeet)
                .HasColumnType("decimal(10,2)")
                .HasColumnName("length_feet");
            entity.Property(e => e.WidthFeet)
                .HasColumnType("decimal(10,2)")
                .HasColumnName("width_feet");
            entity.Property(e => e.HeightFeet)
                .HasColumnType("decimal(10,2)")
                .HasColumnName("height_feet");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");
            entity.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id");

            entity.HasOne(d => d.Company).WithMany(p => p.Warehouses)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_warehouses_company");

            entity.HasOne(d => d.CreatedByUser)
                .WithMany()
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_warehouses_created_by");
        });

        // InterWarehouseTransfer configuration
        modelBuilder.Entity<InterWarehouseTransfer>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("inter_warehouse_transfers_pkey");

            entity.ToTable("inter_warehouse_transfers");

            entity.HasIndex(e => e.FromWarehouseId).HasDatabaseName("idx_inter_warehouse_transfers_from");
            entity.HasIndex(e => e.ToWarehouseId).HasDatabaseName("idx_inter_warehouse_transfers_to");
            entity.HasIndex(e => e.Status).HasDatabaseName("idx_inter_warehouse_transfers_status");
            entity.HasIndex(e => e.TransferDate).HasDatabaseName("idx_inter_warehouse_transfers_date");
            entity.HasIndex(e => e.InitiatedByUserId).HasDatabaseName("idx_inter_warehouse_transfers_initiated_by");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.FromWarehouseId).HasColumnName("from_warehouse_id");
            entity.Property(e => e.ToWarehouseId).HasColumnName("to_warehouse_id");
            entity.Property(e => e.TransferDate).HasColumnName("transfer_date");
            entity.Property(e => e.BarrelsCount)
                .HasDefaultValue(0)
                .HasColumnName("barrels_count");
            entity.Property(e => e.ProofGallons)
                .HasColumnType("decimal(12,2)")
                .HasColumnName("proof_gallons");
            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasColumnName("status");
            entity.Property(e => e.InitiatedByUserId).HasColumnName("initiated_by_user_id");
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.FromWarehouse).WithMany(p => p.OutgoingTransfers)
                .HasForeignKey(d => d.FromWarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_inter_warehouse_transfers_from");

            entity.HasOne(d => d.ToWarehouse).WithMany(p => p.IncomingTransfers)
                .HasForeignKey(d => d.ToWarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_inter_warehouse_transfers_to");

            entity.HasOne(d => d.InitiatedByUser)
                .WithMany()
                .HasForeignKey(d => d.InitiatedByUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_inter_warehouse_transfers_initiated_by");
        });

        // BarrelTransfer configuration
        modelBuilder.Entity<BarrelTransfer>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("barrel_transfers_pkey");

            entity.ToTable("barrel_transfers");

            entity.HasIndex(e => e.BarrelId).HasDatabaseName("idx_barrel_transfers_barrel_id");
            entity.HasIndex(e => e.TransferId).HasDatabaseName("idx_barrel_transfers_transfer_id");
            entity.HasIndex(e => new { e.BarrelId, e.TransferId })
                .IsUnique()
                .HasDatabaseName("uq_barrel_transfer");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BarrelId).HasColumnName("barrel_id");
            entity.Property(e => e.TransferId).HasColumnName("transfer_id");
            entity.Property(e => e.TransferredAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("transferred_at");

            entity.HasOne(d => d.Barrel).WithMany(p => p.BarrelTransfers)
                .HasForeignKey(d => d.BarrelId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_barrel_transfers_barrel");

            entity.HasOne(d => d.Transfer).WithMany(p => p.BarrelTransfers)
                .HasForeignKey(d => d.TransferId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_barrel_transfers_transfer");
        });

        // WarehouseCapacitySnapshot configuration
        modelBuilder.Entity<WarehouseCapacitySnapshot>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("warehouse_capacity_snapshots_pkey");

            entity.ToTable("warehouse_capacity_snapshots");

            entity.HasIndex(e => e.WarehouseId).HasDatabaseName("idx_warehouse_capacity_snapshots_warehouse_id");
            entity.HasIndex(e => e.SnapshotDate).HasDatabaseName("idx_warehouse_capacity_snapshots_date");
            entity.HasIndex(e => e.OccupancyPercentage).HasDatabaseName("idx_warehouse_capacity_snapshots_occupancy");
            entity.HasIndex(e => new { e.WarehouseId, e.SnapshotDate })
                .IsUnique()
                .HasDatabaseName("uq_warehouse_snapshot_date");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.WarehouseId).HasColumnName("warehouse_id");
            entity.Property(e => e.SnapshotDate).HasColumnName("snapshot_date");
            entity.Property(e => e.TotalCapacity).HasColumnName("total_capacity");
            entity.Property(e => e.OccupiedPositions)
                .HasDefaultValue(0)
                .HasColumnName("occupied_positions");
            entity.Property(e => e.OccupancyPercentage)
                .HasColumnType("decimal(5,2)")
                .HasDefaultValue(0.00m)
                .HasColumnName("occupancy_percentage");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.CapacitySnapshots)
                .HasForeignKey(d => d.WarehouseId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_warehouse_capacity_snapshots_warehouse");
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

        modelBuilder.Entity<WebhookSubscription>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("webhook_subscriptions_pkey");

            entity.ToTable("webhook_subscriptions");

            entity.HasIndex(e => e.CompanyId).HasDatabaseName("idx_webhook_subscriptions_company_id");
            entity.HasIndex(e => e.IsActive).HasDatabaseName("idx_webhook_subscriptions_is_active");
            entity.HasIndex(e => e.CreatedByUserId).HasDatabaseName("idx_webhook_subscriptions_created_by");
            entity.HasIndex(e => e.EventTypes)
                .HasMethod("gin")
                .HasDatabaseName("idx_webhook_subscriptions_event_types");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .HasColumnName("name");
            entity.Property(e => e.TargetUrl)
                .HasMaxLength(500)
                .HasColumnName("target_url");
            entity.Property(e => e.EventTypes)
                .HasColumnType("jsonb")
                .HasColumnName("event_types");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.SecretKey)
                .HasMaxLength(100)
                .HasColumnName("secret_key");
            entity.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Company)
                .WithMany(p => p.WebhookSubscriptions)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_webhook_subscriptions_company");

            entity.HasOne(d => d.CreatedByUser)
                .WithMany()
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_webhook_subscriptions_created_by");
        });

        modelBuilder.Entity<WebhookDelivery>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("webhook_deliveries_pkey");

            entity.ToTable("webhook_deliveries");

            entity.HasIndex(e => e.SubscriptionId).HasDatabaseName("idx_webhook_deliveries_subscription_id");
            entity.HasIndex(e => e.DeliveryStatus).HasDatabaseName("idx_webhook_deliveries_status");
            entity.HasIndex(e => e.NextRetryAt).HasDatabaseName("idx_webhook_deliveries_next_retry");
            entity.HasIndex(e => e.CreatedAt).HasDatabaseName("idx_webhook_deliveries_created_at");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.SubscriptionId).HasColumnName("subscription_id");
            entity.Property(e => e.EventType)
                .HasMaxLength(100)
                .HasColumnName("event_type");
            entity.Property(e => e.EventId).HasColumnName("event_id");
            entity.Property(e => e.Payload)
                .HasColumnType("jsonb")
                .HasColumnName("payload");
            entity.Property(e => e.DeliveryStatus)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(WebhookDeliveryStatus.Pending)
                .HasColumnName("delivery_status");
            entity.Property(e => e.HttpStatusCode).HasColumnName("http_status_code");
            entity.Property(e => e.ResponseBody).HasColumnName("response_body");
            entity.Property(e => e.RetryCount)
                .HasDefaultValue(0)
                .HasColumnName("retry_count");
            entity.Property(e => e.NextRetryAt).HasColumnName("next_retry_at");
            entity.Property(e => e.DeliveredAt).HasColumnName("delivered_at");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");

            entity.HasOne(d => d.Subscription)
                .WithMany(p => p.Deliveries)
                .HasForeignKey(d => d.SubscriptionId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_webhook_deliveries_subscription");
        });

        // Portal User configuration
        modelBuilder.Entity<PortalUser>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("portal_users_pkey");

            entity.ToTable("portal_users");

            entity.HasIndex(e => e.Email)
                .IsUnique()
                .HasDatabaseName("portal_users_email_key");

            entity.HasIndex(e => new { e.CompanyId, e.Email })
                .HasDatabaseName("idx_portal_users_company_id_email");

            entity.HasIndex(e => e.VerificationToken)
                .HasDatabaseName("idx_portal_users_verification_token");

            entity.HasIndex(e => e.PasswordResetToken)
                .HasDatabaseName("idx_portal_users_password_reset_token");

            entity.HasIndex(e => e.IsActive)
                .HasDatabaseName("idx_portal_users_is_active");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Email)
                .HasMaxLength(200)
                .HasColumnName("email");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(500)
                .HasColumnName("password_hash");
            entity.Property(e => e.FirstName)
                .HasMaxLength(100)
                .HasColumnName("first_name");
            entity.Property(e => e.LastName)
                .HasMaxLength(100)
                .HasColumnName("last_name");
            entity.Property(e => e.Phone)
                .HasMaxLength(50)
                .HasColumnName("phone");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.EmailVerified)
                .HasDefaultValue(false)
                .HasColumnName("email_verified");
            entity.Property(e => e.VerificationToken)
                .HasMaxLength(100)
                .HasColumnName("verification_token");
            entity.Property(e => e.PasswordResetToken)
                .HasMaxLength(100)
                .HasColumnName("password_reset_token");
            entity.Property(e => e.PasswordResetTokenExpiresAt)
                .HasColumnName("password_reset_token_expires_at");
            entity.Property(e => e.FailedLoginAttempts)
                .HasDefaultValue(0)
                .HasColumnName("failed_login_attempts");
            entity.Property(e => e.LockoutUntil)
                .HasColumnName("lockout_until");
            entity.Property(e => e.LastLoginAt)
                .HasColumnName("last_login_at");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");

            // CRM Integration fields (CRM-001)
            entity.Property(e => e.SalesforceContactId)
                .HasMaxLength(18)
                .HasColumnName("salesforce_contact_id");
            entity.Property(e => e.SalesforceLastSyncAt).HasColumnName("salesforce_last_sync_at");
            entity.Property(e => e.LinkedCustomerId).HasColumnName("linked_customer_id");
            entity.Property(e => e.IsCaskInvestor)
                .HasDefaultValue(false)
                .HasColumnName("is_cask_investor");

            entity.HasIndex(e => e.SalesforceContactId)
                .HasDatabaseName("idx_portal_users_salesforce_contact_id");
            entity.HasIndex(e => e.LinkedCustomerId)
                .HasDatabaseName("idx_portal_users_linked_customer_id");

            entity.HasOne(d => d.Company)
                .WithMany()
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_portal_users_company");

            entity.HasOne(d => d.LinkedCustomer)
                .WithMany()
                .HasForeignKey(d => d.LinkedCustomerId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_portal_users_customer");
        });

        // Cask Ownership configuration
        modelBuilder.Entity<CaskOwnership>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("cask_ownerships_pkey");

            entity.ToTable("cask_ownerships");

            entity.HasIndex(e => e.PortalUserId)
                .HasDatabaseName("idx_cask_ownerships_portal_user_id");

            entity.HasIndex(e => e.BarrelId)
                .HasDatabaseName("idx_cask_ownerships_barrel_id");

            entity.HasIndex(e => new { e.PortalUserId, e.BarrelId })
                .IsUnique()
                .HasDatabaseName("uq_cask_ownerships_portal_user_barrel");

            entity.HasIndex(e => e.Status)
                .HasDatabaseName("idx_cask_ownerships_status");

            entity.HasIndex(e => e.CertificateNumber)
                .HasDatabaseName("idx_cask_ownerships_certificate_number");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.PortalUserId).HasColumnName("portal_user_id");
            entity.Property(e => e.BarrelId).HasColumnName("barrel_id");
            entity.Property(e => e.PurchaseDate).HasColumnName("purchase_date");
            entity.Property(e => e.PurchasePrice)
                .HasColumnType("decimal(10,2)")
                .HasColumnName("purchase_price");
            entity.Property(e => e.OwnershipPercentage)
                .HasColumnType("decimal(5,2)")
                .HasDefaultValue(100.00m)
                .HasColumnName("ownership_percentage");
            entity.Property(e => e.CertificateNumber)
                .HasMaxLength(100)
                .HasColumnName("certificate_number");
            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(CaskOwnershipStatus.Active)
                .HasColumnName("status");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.PortalUser)
                .WithMany(p => p.CaskOwnerships)
                .HasForeignKey(d => d.PortalUserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_cask_ownerships_portal_user");

            entity.HasOne(d => d.Barrel)
                .WithMany()
                .HasForeignKey(d => d.BarrelId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_cask_ownerships_barrel");
        });

        // Portal Access Log configuration
        modelBuilder.Entity<PortalAccessLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("portal_access_logs_pkey");

            entity.ToTable("portal_access_logs");

            entity.HasIndex(e => e.PortalUserId)
                .HasDatabaseName("idx_portal_access_logs_portal_user_id");

            entity.HasIndex(e => e.AccessedAt)
                .HasDatabaseName("idx_portal_access_logs_accessed_at");

            entity.HasIndex(e => e.Action)
                .HasDatabaseName("idx_portal_access_logs_action");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.PortalUserId).HasColumnName("portal_user_id");
            entity.Property(e => e.Action)
                .HasConversion<string>()
                .HasMaxLength(100)
                .HasColumnName("action");
            entity.Property(e => e.ResourceType)
                .HasMaxLength(50)
                .HasColumnName("resource_type");
            entity.Property(e => e.ResourceId).HasColumnName("resource_id");
            entity.Property(e => e.IpAddress)
                .HasMaxLength(45)
                .HasColumnName("ip_address");
            entity.Property(e => e.UserAgent).HasColumnName("user_agent");
            entity.Property(e => e.AccessedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("accessed_at");

            entity.HasOne(d => d.PortalUser)
                .WithMany(p => p.AccessLogs)
                .HasForeignKey(d => d.PortalUserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_portal_access_logs_portal_user");
        });

        // Portal Document configuration
        modelBuilder.Entity<PortalDocument>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("portal_documents_pkey");

            entity.ToTable("portal_documents");

            entity.HasIndex(e => e.CaskOwnershipId)
                .HasDatabaseName("idx_portal_documents_cask_ownership_id");

            entity.HasIndex(e => e.DocumentType)
                .HasDatabaseName("idx_portal_documents_document_type");

            entity.HasIndex(e => e.UploadedAt)
                .HasDatabaseName("idx_portal_documents_uploaded_at");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CaskOwnershipId).HasColumnName("cask_ownership_id");
            entity.Property(e => e.DocumentType)
                .HasConversion<string>()
                .HasMaxLength(50)
                .HasColumnName("document_type");
            entity.Property(e => e.FileName)
                .HasMaxLength(255)
                .HasColumnName("file_name");
            entity.Property(e => e.FilePath)
                .HasMaxLength(500)
                .HasColumnName("file_path");
            entity.Property(e => e.FileSizeBytes).HasColumnName("file_size_bytes");
            entity.Property(e => e.MimeType)
                .HasMaxLength(100)
                .HasColumnName("mime_type");
            entity.Property(e => e.UploadedByUserId).HasColumnName("uploaded_by_user_id");
            entity.Property(e => e.UploadedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("uploaded_at");

            entity.HasOne(d => d.CaskOwnership)
                .WithMany(p => p.Documents)
                .HasForeignKey(d => d.CaskOwnershipId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_portal_documents_cask_ownership");

            entity.HasOne(d => d.UploadedByUser)
                .WithMany()
                .HasForeignKey(d => d.UploadedByUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_portal_documents_uploaded_by");
        });

        // Portal Notification configuration
        modelBuilder.Entity<PortalNotification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("portal_notifications_pkey");

            entity.ToTable("portal_notifications");

            entity.HasIndex(e => e.PortalUserId)
                .HasDatabaseName("idx_portal_notifications_portal_user_id");

            entity.HasIndex(e => e.SentAt)
                .HasDatabaseName("idx_portal_notifications_sent_at");

            entity.HasIndex(e => e.NotificationType)
                .HasDatabaseName("idx_portal_notifications_type");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.PortalUserId).HasColumnName("portal_user_id");
            entity.Property(e => e.NotificationType)
                .HasConversion<string>()
                .HasMaxLength(50)
                .HasColumnName("notification_type");
            entity.Property(e => e.Title)
                .HasMaxLength(200)
                .HasColumnName("title");
            entity.Property(e => e.Message).HasColumnName("message");
            entity.Property(e => e.RelatedBarrelId).HasColumnName("related_barrel_id");
            entity.Property(e => e.IsRead)
                .HasDefaultValue(false)
                .HasColumnName("is_read");
            entity.Property(e => e.SentAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("sent_at");
            entity.Property(e => e.ReadAt).HasColumnName("read_at");

            entity.HasOne(d => d.PortalUser)
                .WithMany(p => p.Notifications)
                .HasForeignKey(d => d.PortalUserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_portal_notifications_portal_user");

            entity.HasOne(d => d.RelatedBarrel)
                .WithMany()
                .HasForeignKey(d => d.RelatedBarrelId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_portal_notifications_barrel");
        });

        // ========================================================================
        // CRM Integration Entity Configurations (CRM-001)
        // ========================================================================

        // Customer configuration
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("customers_pkey");

            entity.ToTable("customers");

            entity.HasIndex(e => e.CompanyId).HasDatabaseName("idx_customers_company_id");
            entity.HasIndex(e => new { e.CompanyId, e.CustomerName }).HasDatabaseName("idx_customers_customer_name");
            entity.HasIndex(e => e.SalesforceAccountId)
                .HasDatabaseName("idx_customers_salesforce_account_id");
            entity.HasIndex(e => new { e.CompanyId, e.CustomerType }).HasDatabaseName("idx_customers_customer_type");
            entity.HasIndex(e => new { e.CompanyId, e.IsActive }).HasDatabaseName("idx_customers_is_active");
            entity.HasIndex(e => new { e.CompanyId, e.SalesforceAccountId })
                .IsUnique()
                .HasDatabaseName("uq_customers_salesforce_account");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.CustomerName)
                .HasMaxLength(200)
                .HasColumnName("customer_name");
            entity.Property(e => e.CustomerType)
                .HasConversion<string>()
                .HasColumnName("customer_type");
            entity.Property(e => e.Email)
                .HasMaxLength(200)
                .HasColumnName("email");
            entity.Property(e => e.Phone)
                .HasMaxLength(50)
                .HasColumnName("phone");
            entity.Property(e => e.Website)
                .HasMaxLength(255)
                .HasColumnName("website");
            entity.Property(e => e.AddressLine1)
                .HasMaxLength(200)
                .HasColumnName("address_line1");
            entity.Property(e => e.AddressLine2)
                .HasMaxLength(200)
                .HasColumnName("address_line2");
            entity.Property(e => e.City)
                .HasMaxLength(100)
                .HasColumnName("city");
            entity.Property(e => e.State)
                .HasMaxLength(100)
                .HasColumnName("state");
            entity.Property(e => e.PostalCode)
                .HasMaxLength(20)
                .HasColumnName("postal_code");
            entity.Property(e => e.Country)
                .HasMaxLength(100)
                .HasDefaultValue("USA")
                .HasColumnName("country");
            entity.Property(e => e.SalesforceAccountId)
                .HasMaxLength(18)
                .HasColumnName("salesforce_account_id");
            entity.Property(e => e.SalesforceLastSyncAt).HasColumnName("salesforce_last_sync_at");
            entity.Property(e => e.AssignedUserId).HasColumnName("assigned_user_id");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Company)
                .WithMany()
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_customers_company");

            entity.HasOne(d => d.AssignedUser)
                .WithMany()
                .HasForeignKey(d => d.AssignedUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_customers_assigned_user");
        });

        // CrmIntegration configuration
        modelBuilder.Entity<CrmIntegration>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("crm_integrations_pkey");

            entity.ToTable("crm_integrations");

            entity.HasIndex(e => e.CompanyId).HasDatabaseName("idx_crm_integrations_company_id");
            entity.HasIndex(e => e.Provider).HasDatabaseName("idx_crm_integrations_provider");
            entity.HasIndex(e => e.IsActive).HasDatabaseName("idx_crm_integrations_is_active");
            entity.HasIndex(e => new { e.CompanyId, e.Provider })
                .IsUnique()
                .HasDatabaseName("uq_crm_integrations_company_provider");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.Provider).HasColumnName("provider");
            entity.Property(e => e.InstanceUrl).HasColumnName("instance_url");
            entity.Property(e => e.OrganizationId)
                .HasMaxLength(18)
                .HasColumnName("organization_id");
            entity.Property(e => e.AccessTokenEncrypted).HasColumnName("access_token_encrypted");
            entity.Property(e => e.RefreshTokenEncrypted).HasColumnName("refresh_token_encrypted");
            entity.Property(e => e.TokenExpiresAt).HasColumnName("token_expires_at");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.ConnectionStatus)
                .HasConversion<string>()
                .HasColumnName("connection_status");
            entity.Property(e => e.LastErrorMessage).HasColumnName("last_error_message");
            entity.Property(e => e.LastErrorAt).HasColumnName("last_error_at");
            entity.Property(e => e.ConnectedByUserId).HasColumnName("connected_by_user_id");
            entity.Property(e => e.ConnectedAt).HasColumnName("connected_at");
            entity.Property(e => e.LastSyncAt).HasColumnName("last_sync_at");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Company)
                .WithMany()
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_crm_integrations_company");

            entity.HasOne(d => d.ConnectedByUser)
                .WithMany()
                .HasForeignKey(d => d.ConnectedByUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_crm_integrations_connected_by");
        });

        // CrmSyncLog configuration
        modelBuilder.Entity<CrmSyncLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("crm_sync_logs_pkey");

            entity.ToTable("crm_sync_logs");

            entity.HasIndex(e => e.CompanyId).HasDatabaseName("idx_crm_sync_logs_company_id");
            entity.HasIndex(e => e.SyncStatus).HasDatabaseName("idx_crm_sync_logs_sync_status");
            entity.HasIndex(e => e.SyncedAt).HasDatabaseName("idx_crm_sync_logs_synced_at");
            entity.HasIndex(e => e.EntityType).HasDatabaseName("idx_crm_sync_logs_entity_type");
            entity.HasIndex(e => e.SalesforceId).HasDatabaseName("idx_crm_sync_logs_salesforce_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.Provider).HasColumnName("provider");
            entity.Property(e => e.EntityType).HasColumnName("entity_type");
            entity.Property(e => e.CaskrEntityId).HasColumnName("caskr_entity_id");
            entity.Property(e => e.CaskrEntityType).HasColumnName("caskr_entity_type");
            entity.Property(e => e.SalesforceId)
                .HasMaxLength(18)
                .HasColumnName("salesforce_id");
            entity.Property(e => e.SyncDirection)
                .HasConversion<string>()
                .HasColumnName("sync_direction");
            entity.Property(e => e.SyncStatus)
                .HasConversion<string>()
                .HasColumnName("sync_status");
            entity.Property(e => e.SyncAction).HasColumnName("sync_action");
            entity.Property(e => e.ErrorMessage).HasColumnName("error_message");
            entity.Property(e => e.ErrorCode).HasColumnName("error_code");
            entity.Property(e => e.RetryCount)
                .HasDefaultValue(0)
                .HasColumnName("retry_count");
            entity.Property(e => e.RequestPayload)
                .HasColumnType("jsonb")
                .HasColumnName("request_payload");
            entity.Property(e => e.ResponsePayload)
                .HasColumnType("jsonb")
                .HasColumnName("response_payload");
            entity.Property(e => e.SyncStartedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("sync_started_at");
            entity.Property(e => e.SyncCompletedAt).HasColumnName("sync_completed_at");
            entity.Property(e => e.SyncedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("synced_at");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");

            entity.HasOne(d => d.Company)
                .WithMany()
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_crm_sync_logs_company");
        });

        // CrmEntityMapping configuration
        modelBuilder.Entity<CrmEntityMapping>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("crm_entity_mappings_pkey");

            entity.ToTable("crm_entity_mappings");

            entity.HasIndex(e => e.CompanyId).HasDatabaseName("idx_crm_entity_mappings_company_id");
            entity.HasIndex(e => e.SalesforceId).HasDatabaseName("idx_crm_entity_mappings_salesforce_id");
            entity.HasIndex(e => new { e.CompanyId, e.Provider, e.CaskrEntityType, e.CaskrEntityId })
                .IsUnique()
                .HasDatabaseName("uq_crm_entity_mappings_caskr");
            entity.HasIndex(e => new { e.CompanyId, e.Provider, e.SalesforceEntityType, e.SalesforceId })
                .IsUnique()
                .HasDatabaseName("uq_crm_entity_mappings_salesforce");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.Provider).HasColumnName("provider");
            entity.Property(e => e.SalesforceEntityType).HasColumnName("salesforce_entity_type");
            entity.Property(e => e.SalesforceId)
                .HasMaxLength(18)
                .HasColumnName("salesforce_id");
            entity.Property(e => e.CaskrEntityType).HasColumnName("caskr_entity_type");
            entity.Property(e => e.CaskrEntityId).HasColumnName("caskr_entity_id");
            entity.Property(e => e.LastSyncAt).HasColumnName("last_sync_at");
            entity.Property(e => e.CaskrLastModified).HasColumnName("caskr_last_modified");
            entity.Property(e => e.SalesforceLastModified).HasColumnName("salesforce_last_modified");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Company)
                .WithMany()
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_crm_entity_mappings_company");
        });

        // CrmFieldMapping configuration
        modelBuilder.Entity<CrmFieldMapping>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("crm_field_mappings_pkey");

            entity.ToTable("crm_field_mappings");

            entity.HasIndex(e => e.CompanyId).HasDatabaseName("idx_crm_field_mappings_company_id");
            entity.HasIndex(e => e.SalesforceEntityType).HasDatabaseName("idx_crm_field_mappings_entity_type");
            entity.HasIndex(e => new { e.CompanyId, e.Provider, e.SalesforceEntityType, e.SalesforceField })
                .IsUnique()
                .HasDatabaseName("uq_crm_field_mappings");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.Provider).HasColumnName("provider");
            entity.Property(e => e.SalesforceEntityType).HasColumnName("salesforce_entity_type");
            entity.Property(e => e.CaskrEntityType).HasColumnName("caskr_entity_type");
            entity.Property(e => e.SalesforceField).HasColumnName("salesforce_field");
            entity.Property(e => e.CaskrField).HasColumnName("caskr_field");
            entity.Property(e => e.TransformationRule).HasColumnName("transformation_rule");
            entity.Property(e => e.DefaultValue).HasColumnName("default_value");
            entity.Property(e => e.SyncDirection)
                .HasConversion<string>()
                .HasColumnName("sync_direction");
            entity.Property(e => e.IsRequired)
                .HasDefaultValue(false)
                .HasColumnName("is_required");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Company)
                .WithMany()
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_crm_field_mappings_company");
        });

        // CrmSyncPreference configuration
        modelBuilder.Entity<CrmSyncPreference>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("crm_sync_preferences_pkey");

            entity.ToTable("crm_sync_preferences");

            entity.HasIndex(e => e.CompanyId).HasDatabaseName("idx_crm_sync_preferences_company_id");
            entity.HasIndex(e => new { e.CompanyId, e.Provider, e.EntityType })
                .IsUnique()
                .HasDatabaseName("uq_crm_sync_preferences");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.Provider).HasColumnName("provider");
            entity.Property(e => e.EntityType).HasColumnName("entity_type");
            entity.Property(e => e.SyncDirection)
                .HasConversion<string>()
                .HasColumnName("sync_direction");
            entity.Property(e => e.WebhookEnabled)
                .HasDefaultValue(true)
                .HasColumnName("webhook_enabled");
            entity.Property(e => e.PollingEnabled)
                .HasDefaultValue(true)
                .HasColumnName("polling_enabled");
            entity.Property(e => e.PollingIntervalMinutes)
                .HasDefaultValue(15)
                .HasColumnName("polling_interval_minutes");
            entity.Property(e => e.AutoCreateEnabled)
                .HasDefaultValue(true)
                .HasColumnName("auto_create_enabled");
            entity.Property(e => e.AutoUpdateEnabled)
                .HasDefaultValue(true)
                .HasColumnName("auto_update_enabled");
            entity.Property(e => e.AutoDeleteEnabled)
                .HasDefaultValue(false)
                .HasColumnName("auto_delete_enabled");
            entity.Property(e => e.ConflictResolution).HasColumnName("conflict_resolution");
            entity.Property(e => e.LastPollingAt).HasColumnName("last_polling_at");
            entity.Property(e => e.LastWebhookAt).HasColumnName("last_webhook_at");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Company)
                .WithMany()
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_crm_sync_preferences_company");
        });

        // CrmSyncConflict configuration
        modelBuilder.Entity<CrmSyncConflict>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("crm_sync_conflicts_pkey");

            entity.ToTable("crm_sync_conflicts");

            entity.HasIndex(e => e.CompanyId).HasDatabaseName("idx_crm_sync_conflicts_company_id");
            entity.HasIndex(e => e.ResolutionStatus).HasDatabaseName("idx_crm_sync_conflicts_status");
            entity.HasIndex(e => e.CreatedAt).HasDatabaseName("idx_crm_sync_conflicts_created_at");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.Provider).HasColumnName("provider");
            entity.Property(e => e.SalesforceEntityType).HasColumnName("salesforce_entity_type");
            entity.Property(e => e.SalesforceId)
                .HasMaxLength(18)
                .HasColumnName("salesforce_id");
            entity.Property(e => e.CaskrEntityType).HasColumnName("caskr_entity_type");
            entity.Property(e => e.CaskrEntityId).HasColumnName("caskr_entity_id");
            entity.Property(e => e.FieldName).HasColumnName("field_name");
            entity.Property(e => e.CaskrValue).HasColumnName("caskr_value");
            entity.Property(e => e.SalesforceValue).HasColumnName("salesforce_value");
            entity.Property(e => e.CaskrModifiedAt).HasColumnName("caskr_modified_at");
            entity.Property(e => e.SalesforceModifiedAt).HasColumnName("salesforce_modified_at");
            entity.Property(e => e.ResolutionStatus)
                .HasConversion<string>()
                .HasColumnName("resolution_status");
            entity.Property(e => e.ResolvedValue).HasColumnName("resolved_value");
            entity.Property(e => e.ResolvedByUserId).HasColumnName("resolved_by_user_id");
            entity.Property(e => e.ResolvedAt).HasColumnName("resolved_at");
            entity.Property(e => e.ResolutionNotes).HasColumnName("resolution_notes");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_at");

            entity.HasOne(d => d.Company)
                .WithMany()
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_crm_sync_conflicts_company");

            entity.HasOne(d => d.ResolvedByUser)
                .WithMany()
                .HasForeignKey(d => d.ResolvedByUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_crm_sync_conflicts_resolved_by");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
