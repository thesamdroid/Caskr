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

    public virtual DbSet<Order?> Orders { get; set; }

    public virtual DbSet<Product?> Products { get; set; }

    public virtual DbSet<Status?> Statuses { get; set; }

    public virtual DbSet<StatusTask?> StatusTasks { get; set; }

    public virtual DbSet<User?> Users { get; set; }

    public virtual DbSet<UserType?> UserTypes { get; set; }

    public virtual DbSet<TaskItem?> Tasks { get; set; }

    public virtual DbSet<Company?> Companies { get; set; }

    public virtual DbSet<SpiritType?> SpiritTypes { get; set; }

    public virtual DbSet<Batch?> Batches { get; set; }

    public virtual DbSet<MashBill?> MashBills { get; set; }

    public virtual DbSet<Component?> Components { get; set; }

    public virtual DbSet<Rickhouse?> Rickhouses { get; set; }

    public virtual DbSet<Barrel?> Barrels { get; set; }

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
        });

        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Tasks_pkey");

            entity.ToTable("tasks");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("nextval('\"Tasks_id_seq\"'::regclass)")
                .HasColumnName("id");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.Name).HasColumnName("name");
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
            entity.Property(e => e.PrimaryContactId).HasColumnName("primary_contact_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("created_date");
            entity.Property(e => e.RenewalDate).HasColumnName("renewal_date");

            entity.HasOne(d => d.PrimaryContact).WithMany()
                .HasForeignKey(d => d.PrimaryContactId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_company_primary_contact");
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

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
