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

    public virtual DbSet<CompletedTask?> CompletedTasks { get; set; }

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
            entity.Property(e => e.StatusId)
                .HasDefaultValueSql("nextval('\"Orders_status_id_seq\"'::regclass)")
                .HasColumnName("status_id");

            entity.HasOne(d => d.Owner).WithMany(p => p.Orders)
                .HasForeignKey(d => d.OwnerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_ownerid_userid");

            entity.HasOne(d => d.Status).WithMany(p => p.Orders)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_statusid_stautusid");
        });

        modelBuilder.Entity<CompletedTask>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("CompletedTask_pkey");

            entity.ToTable("completed_task");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("nextval('\"CompletedTask_id_seq\"'::regclass)")
                .HasColumnName("id");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.Name).HasColumnName("name");

            entity.HasOne(d => d.Order).WithMany(p => p.CompletedTasks)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_completedtask_orderid");
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

        modelBuilder.Entity<Status>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Status_pkey");

            entity.ToTable("status");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("nextval('\"Status_id_seq\"'::regclass)")
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

            entity.HasOne(d => d.UserType).WithMany(p => p.Users)
                .HasForeignKey(d => d.UserTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_user_userstatus");
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

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
