using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using WMS.API.Models;

namespace WMS.API.Data;

public partial class WmsDbContext : DbContext
{
    public WmsDbContext(DbContextOptions<WmsDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AppUser> AppUsers { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<BillOfMaterial> BillOfMaterials { get; set; }

    public virtual DbSet<BomLine> BomLines { get; set; }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<GoodsReceipt> GoodsReceipts { get; set; }

    public virtual DbSet<GoodsReceiptLine> GoodsReceiptLines { get; set; }

    public virtual DbSet<InventoryBalance> InventoryBalances { get; set; }

    public virtual DbSet<InventoryLot> InventoryLots { get; set; }

    public virtual DbSet<InventoryReservation> InventoryReservations { get; set; }

    public virtual DbSet<InventoryTransaction> InventoryTransactions { get; set; }

    public virtual DbSet<InventoryTransactionLine> InventoryTransactionLines { get; set; }

    public virtual DbSet<Item> Items { get; set; }

    public virtual DbSet<ItemBarcode> ItemBarcodes { get; set; }

    public virtual DbSet<ItemCategory> ItemCategories { get; set; }

    public virtual DbSet<LocationBarcode> LocationBarcodes { get; set; }

    public virtual DbSet<LotBarcode> LotBarcodes { get; set; }

    public virtual DbSet<ProductionBatchGenealogy> ProductionBatchGenealogies { get; set; }

    public virtual DbSet<ProductionConsumption> ProductionConsumptions { get; set; }

    public virtual DbSet<ProductionMaterialIssue> ProductionMaterialIssues { get; set; }

    public virtual DbSet<ProductionMaterialIssueLine> ProductionMaterialIssueLines { get; set; }

    public virtual DbSet<ProductionMaterialRequirement> ProductionMaterialRequirements { get; set; }

    public virtual DbSet<ProductionOrder> ProductionOrders { get; set; }

    public virtual DbSet<ProductionOutput> ProductionOutputs { get; set; }

    public virtual DbSet<ProductionWaste> ProductionWastes { get; set; }

    public virtual DbSet<PurchaseOrder> PurchaseOrders { get; set; }

    public virtual DbSet<PurchaseOrderLine> PurchaseOrderLines { get; set; }

    public virtual DbSet<PutawayTask> PutawayTasks { get; set; }

    public virtual DbSet<QualityInspection> QualityInspections { get; set; }

    public virtual DbSet<QualityInspectionLine> QualityInspectionLines { get; set; }

    public virtual DbSet<QualityParameter> QualityParameters { get; set; }

    public virtual DbSet<QualityStatusHistory> QualityStatusHistories { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<StockAdjustment> StockAdjustments { get; set; }

    public virtual DbSet<StockAdjustmentLine> StockAdjustmentLines { get; set; }

    public virtual DbSet<StockCount> StockCounts { get; set; }

    public virtual DbSet<StockCountLine> StockCountLines { get; set; }

    public virtual DbSet<StockTransfer> StockTransfers { get; set; }

    public virtual DbSet<StockTransferLine> StockTransferLines { get; set; }

    public virtual DbSet<StorageBin> StorageBins { get; set; }

    public virtual DbSet<Supplier> Suppliers { get; set; }

    public virtual DbSet<SystemNotification> SystemNotifications { get; set; }

    public virtual DbSet<UnitsOfMeasure> UnitsOfMeasures { get; set; }

    public virtual DbSet<UomConversion> UomConversions { get; set; }

    public virtual DbSet<Warehouse> Warehouses { get; set; }

    public virtual DbSet<WarehouseZone> WarehouseZones { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pgcrypto");

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("app_users_pkey");

            entity.ToTable("app_users");

            entity.HasIndex(e => e.EmployeeCode, "app_users_employee_code_key").IsUnique();

            entity.HasIndex(e => e.Username, "app_users_username_key").IsUnique();

            entity.Property(e => e.UserId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("user_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(150)
                .HasColumnName("email");
            entity.Property(e => e.EmployeeCode)
                .HasMaxLength(40)
                .HasColumnName("employee_code");
            entity.Property(e => e.FullName)
                .HasMaxLength(150)
                .HasColumnName("full_name");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
            entity.Property(e => e.Username)
                .HasMaxLength(80)
                .HasColumnName("username");

            entity.HasMany(d => d.Roles).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "UserRole",
                    r => r.HasOne<Role>().WithMany()
                        .HasForeignKey("RoleId")
                        .HasConstraintName("user_roles_role_id_fkey"),
                    l => l.HasOne<AppUser>().WithMany()
                        .HasForeignKey("UserId")
                        .HasConstraintName("user_roles_user_id_fkey"),
                    j =>
                    {
                        j.HasKey("UserId", "RoleId").HasName("user_roles_pkey");
                        j.ToTable("user_roles");
                        j.IndexerProperty<long>("UserId").HasColumnName("user_id");
                        j.IndexerProperty<long>("RoleId").HasColumnName("role_id");
                    });

            entity.HasMany(d => d.Warehouses).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "UserWarehouseAccess",
                    r => r.HasOne<Warehouse>().WithMany()
                        .HasForeignKey("WarehouseId")
                        .HasConstraintName("user_warehouse_access_warehouse_id_fkey"),
                    l => l.HasOne<AppUser>().WithMany()
                        .HasForeignKey("UserId")
                        .HasConstraintName("user_warehouse_access_user_id_fkey"),
                    j =>
                    {
                        j.HasKey("UserId", "WarehouseId").HasName("user_warehouse_access_pkey");
                        j.ToTable("user_warehouse_access");
                        j.IndexerProperty<long>("UserId").HasColumnName("user_id");
                        j.IndexerProperty<long>("WarehouseId").HasColumnName("warehouse_id");
                    });
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.AuditId).HasName("audit_logs_pkey");

            entity.ToTable("audit_logs");

            entity.HasIndex(e => new { e.TableName, e.RecordId }, "ix_audit_logs_table_record");

            entity.Property(e => e.AuditId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("audit_id");
            entity.Property(e => e.ActionTimestamp)
                .HasDefaultValueSql("now()")
                .HasColumnName("action_timestamp");
            entity.Property(e => e.ActionType)
                .HasMaxLength(30)
                .HasColumnName("action_type");
            entity.Property(e => e.IpAddress).HasColumnName("ip_address");
            entity.Property(e => e.NewValues)
                .HasColumnType("jsonb")
                .HasColumnName("new_values");
            entity.Property(e => e.OldValues)
                .HasColumnType("jsonb")
                .HasColumnName("old_values");
            entity.Property(e => e.RecordId)
                .HasMaxLength(100)
                .HasColumnName("record_id");
            entity.Property(e => e.TableName)
                .HasMaxLength(100)
                .HasColumnName("table_name");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("audit_logs_user_id_fkey");
        });

        modelBuilder.Entity<BillOfMaterial>(entity =>
        {
            entity.HasKey(e => e.BomId).HasName("bill_of_materials_pkey");

            entity.ToTable("bill_of_materials");

            entity.HasIndex(e => new { e.BomCode, e.VersionNumber }, "bill_of_materials_bom_code_version_number_key").IsUnique();

            entity.Property(e => e.BomId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("bom_id");
            entity.Property(e => e.BomCode)
                .HasMaxLength(50)
                .HasColumnName("bom_code");
            entity.Property(e => e.EffectiveFrom).HasColumnName("effective_from");
            entity.Property(e => e.EffectiveTo).HasColumnName("effective_to");
            entity.Property(e => e.FinishedItemId).HasColumnName("finished_item_id");
            entity.Property(e => e.OutputQuantity)
                .HasPrecision(18, 6)
                .HasColumnName("output_quantity");
            entity.Property(e => e.OutputUomId).HasColumnName("output_uom_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'DRAFT'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.VersionNumber)
                .HasDefaultValue(1)
                .HasColumnName("version_number");

            entity.HasOne(d => d.FinishedItem).WithMany(p => p.BillOfMaterials)
                .HasForeignKey(d => d.FinishedItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("bill_of_materials_finished_item_id_fkey");

            entity.HasOne(d => d.OutputUom).WithMany(p => p.BillOfMaterials)
                .HasForeignKey(d => d.OutputUomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("bill_of_materials_output_uom_id_fkey");
        });

        modelBuilder.Entity<BomLine>(entity =>
        {
            entity.HasKey(e => e.BomLineId).HasName("bom_lines_pkey");

            entity.ToTable("bom_lines");

            entity.HasIndex(e => new { e.BomId, e.ComponentItemId }, "bom_lines_bom_id_component_item_id_key").IsUnique();

            entity.Property(e => e.BomLineId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("bom_line_id");
            entity.Property(e => e.BomId).HasColumnName("bom_id");
            entity.Property(e => e.ComponentItemId).HasColumnName("component_item_id");
            entity.Property(e => e.RequiredQuantity)
                .HasPrecision(18, 6)
                .HasColumnName("required_quantity");
            entity.Property(e => e.UomId).HasColumnName("uom_id");
            entity.Property(e => e.WastagePercentage)
                .HasPrecision(7, 4)
                .HasColumnName("wastage_percentage");

            entity.HasOne(d => d.Bom).WithMany(p => p.BomLines)
                .HasForeignKey(d => d.BomId)
                .HasConstraintName("bom_lines_bom_id_fkey");

            entity.HasOne(d => d.ComponentItem).WithMany(p => p.BomLines)
                .HasForeignKey(d => d.ComponentItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("bom_lines_component_item_id_fkey");

            entity.HasOne(d => d.Uom).WithMany(p => p.BomLines)
                .HasForeignKey(d => d.UomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("bom_lines_uom_id_fkey");
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.CustomerId).HasName("customers_pkey");

            entity.ToTable("customers");

            entity.HasIndex(e => e.CustomerCode, "customers_customer_code_key").IsUnique();

            entity.Property(e => e.CustomerId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("customer_id");
            entity.Property(e => e.Address).HasColumnName("address");
            entity.Property(e => e.ContactPerson)
                .HasMaxLength(150)
                .HasColumnName("contact_person");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CustomerCode)
                .HasMaxLength(40)
                .HasColumnName("customer_code");
            entity.Property(e => e.CustomerName)
                .HasMaxLength(200)
                .HasColumnName("customer_name");
            entity.Property(e => e.Email)
                .HasMaxLength(150)
                .HasColumnName("email");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Phone)
                .HasMaxLength(50)
                .HasColumnName("phone");
        });

        modelBuilder.Entity<GoodsReceipt>(entity =>
        {
            entity.HasKey(e => e.ReceiptId).HasName("goods_receipts_pkey");

            entity.ToTable("goods_receipts");

            entity.HasIndex(e => e.ReceiptNumber, "goods_receipts_receipt_number_key").IsUnique();

            entity.HasIndex(e => new { e.WarehouseId, e.ReceiptDate }, "ix_receipts_warehouse_date");

            entity.Property(e => e.ReceiptId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("receipt_id");
            entity.Property(e => e.DeliveryReference)
                .HasMaxLength(100)
                .HasColumnName("delivery_reference");
            entity.Property(e => e.PurchaseOrderId).HasColumnName("purchase_order_id");
            entity.Property(e => e.ReceiptDate)
                .HasDefaultValueSql("now()")
                .HasColumnName("receipt_date");
            entity.Property(e => e.ReceiptNumber)
                .HasMaxLength(50)
                .HasColumnName("receipt_number");
            entity.Property(e => e.ReceivedBy).HasColumnName("received_by");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'DRAFT'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.WarehouseId).HasColumnName("warehouse_id");

            entity.HasOne(d => d.PurchaseOrder).WithMany(p => p.GoodsReceipts)
                .HasForeignKey(d => d.PurchaseOrderId)
                .HasConstraintName("goods_receipts_purchase_order_id_fkey");

            entity.HasOne(d => d.ReceivedByNavigation).WithMany(p => p.GoodsReceipts)
                .HasForeignKey(d => d.ReceivedBy)
                .HasConstraintName("goods_receipts_received_by_fkey");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.GoodsReceipts)
                .HasForeignKey(d => d.WarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("goods_receipts_warehouse_id_fkey");
        });

        modelBuilder.Entity<GoodsReceiptLine>(entity =>
        {
            entity.HasKey(e => e.ReceiptLineId).HasName("goods_receipt_lines_pkey");

            entity.ToTable("goods_receipt_lines");

            entity.Property(e => e.ReceiptLineId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("receipt_line_id");
            entity.Property(e => e.InspectionStatus)
                .HasMaxLength(20)
                .HasDefaultValueSql("'PENDING'::character varying")
                .HasColumnName("inspection_status");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.LotId).HasColumnName("lot_id");

            entity.Property(e => e.ReceivingBinId)
                .HasColumnName("receiving_bin_id");

                        entity.HasOne<StorageBin>()
                            .WithMany()
                            .HasForeignKey(e => e.ReceivingBinId)
                            .HasConstraintName("goods_receipt_lines_receiving_bin_id_fkey");

            entity.Property(e => e.PoLineId).HasColumnName("po_line_id");
            entity.Property(e => e.ReceiptId).HasColumnName("receipt_id");
            entity.Property(e => e.ReceivedQuantity)
                .HasPrecision(18, 6)
                .HasColumnName("received_quantity");
            entity.Property(e => e.UomId).HasColumnName("uom_id");

            entity.HasOne(d => d.Item).WithMany(p => p.GoodsReceiptLines)
                .HasForeignKey(d => d.ItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("goods_receipt_lines_item_id_fkey");

            entity.HasOne(d => d.Lot).WithMany(p => p.GoodsReceiptLines)
                .HasForeignKey(d => d.LotId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("goods_receipt_lines_lot_id_fkey");

            entity.HasOne(d => d.PoLine).WithMany(p => p.GoodsReceiptLines)
                .HasForeignKey(d => d.PoLineId)
                .HasConstraintName("goods_receipt_lines_po_line_id_fkey");

            entity.HasOne(d => d.Receipt).WithMany(p => p.GoodsReceiptLines)
                .HasForeignKey(d => d.ReceiptId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("goods_receipt_lines_receipt_id_fkey");

            entity.HasOne(d => d.Uom).WithMany(p => p.GoodsReceiptLines)
                .HasForeignKey(d => d.UomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("goods_receipt_lines_uom_id_fkey");
        });

        modelBuilder.Entity<InventoryBalance>(entity =>
        {
            entity.HasKey(e => e.BalanceId).HasName("inventory_balances_pkey");

            entity.ToTable("inventory_balances");

            entity.HasIndex(e => new { e.LotId, e.BinId, e.StockStatus }, "inventory_balances_lot_id_bin_id_stock_status_key").IsUnique();

            entity.HasIndex(e => e.BinId, "ix_balances_bin");

            entity.HasIndex(e => e.LotId, "ix_balances_lot");

            entity.Property(e => e.BalanceId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("balance_id");
            entity.Property(e => e.BinId).HasColumnName("bin_id");
            entity.Property(e => e.LotId).HasColumnName("lot_id");
            entity.Property(e => e.QuantityOnHand)
                .HasPrecision(18, 6)
                .HasColumnName("quantity_on_hand");
            entity.Property(e => e.QuantityReserved)
                .HasPrecision(18, 6)
                .HasColumnName("quantity_reserved");
            entity.Property(e => e.StockStatus)
                .HasMaxLength(20)
                .HasDefaultValueSql("'AVAILABLE'::character varying")
                .HasColumnName("stock_status");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Bin).WithMany(p => p.InventoryBalances)
                .HasForeignKey(d => d.BinId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("inventory_balances_bin_id_fkey");

            entity.HasOne(d => d.Lot).WithMany(p => p.InventoryBalances)
                .HasForeignKey(d => d.LotId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("inventory_balances_lot_id_fkey");
        });

        modelBuilder.Entity<InventoryLot>(entity =>
        {
            entity.HasKey(e => e.LotId).HasName("inventory_lots_pkey");

            entity.ToTable("inventory_lots");

            entity.HasIndex(e => new { e.ItemId, e.BatchCode }, "inventory_lots_item_id_batch_code_key").IsUnique();

            entity.HasIndex(e => e.ExpiryDate, "ix_lots_expiry");

            entity.HasIndex(e => e.ItemId, "ix_lots_item");

            entity.HasIndex(e => e.QualityStatus, "ix_lots_quality");

            entity.Property(e => e.LotId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("lot_id");
            entity.Property(e => e.BatchCode)
                .HasMaxLength(100)
                .HasColumnName("batch_code");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiryDate).HasColumnName("expiry_date");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.ManufactureDate).HasColumnName("manufacture_date");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.QualityStatus)
                .HasMaxLength(20)
                .HasDefaultValueSql("'PENDING'::character varying")
                .HasColumnName("quality_status");
            entity.Property(e => e.ReceivedDate)
                .HasDefaultValueSql("CURRENT_DATE")
                .HasColumnName("received_date");
            entity.Property(e => e.SupplierBatchCode)
                .HasMaxLength(100)
                .HasColumnName("supplier_batch_code");

            entity.HasOne(d => d.Item).WithMany(p => p.InventoryLots)
                .HasForeignKey(d => d.ItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("inventory_lots_item_id_fkey");
        });

        modelBuilder.Entity<InventoryReservation>(entity =>
        {
            entity.HasKey(e => e.ReservationId).HasName("inventory_reservations_pkey");

            entity.ToTable("inventory_reservations");

            entity.Property(e => e.ReservationId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("reservation_id");
            entity.Property(e => e.BinId).HasColumnName("bin_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.LotId).HasColumnName("lot_id");
            entity.Property(e => e.ReferenceId).HasColumnName("reference_id");
            entity.Property(e => e.ReferenceType)
                .HasMaxLength(30)
                .HasColumnName("reference_type");
            entity.Property(e => e.ReservedQuantity)
                .HasPrecision(18, 6)
                .HasColumnName("reserved_quantity");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'ACTIVE'::character varying")
                .HasColumnName("status");

            entity.HasOne(d => d.Bin).WithMany(p => p.InventoryReservations)
                .HasForeignKey(d => d.BinId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("inventory_reservations_bin_id_fkey");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.InventoryReservations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("inventory_reservations_created_by_fkey");

            entity.HasOne(d => d.Lot).WithMany(p => p.InventoryReservations)
                .HasForeignKey(d => d.LotId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("inventory_reservations_lot_id_fkey");
        });

        modelBuilder.Entity<InventoryTransaction>(entity =>
        {
            entity.HasKey(e => e.TransactionId).HasName("inventory_transactions_pkey");

            entity.ToTable("inventory_transactions");

            entity.HasIndex(e => e.TransactionNumber, "inventory_transactions_transaction_number_key").IsUnique();

            entity.HasIndex(e => e.TransactionDate, "ix_transactions_date");

            entity.Property(e => e.TransactionId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("transaction_id");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.PostedAt).HasColumnName("posted_at");
            entity.Property(e => e.PostedBy).HasColumnName("posted_by");
            entity.Property(e => e.ReferenceNumber)
                .HasMaxLength(100)
                .HasColumnName("reference_number");
            entity.Property(e => e.Remarks).HasColumnName("remarks");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'DRAFT'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.TransactionDate)
                .HasDefaultValueSql("now()")
                .HasColumnName("transaction_date");
            entity.Property(e => e.TransactionNumber)
                .HasMaxLength(50)
                .HasColumnName("transaction_number");
            entity.Property(e => e.TransactionType)
                .HasMaxLength(30)
                .HasColumnName("transaction_type");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.InventoryTransactionCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("inventory_transactions_created_by_fkey");

            entity.HasOne(d => d.PostedByNavigation).WithMany(p => p.InventoryTransactionPostedByNavigations)
                .HasForeignKey(d => d.PostedBy)
                .HasConstraintName("inventory_transactions_posted_by_fkey");
        });

        modelBuilder.Entity<InventoryTransactionLine>(entity =>
        {
            entity.HasKey(e => e.TransactionLineId).HasName("inventory_transaction_lines_pkey");

            entity.ToTable("inventory_transaction_lines");

            entity.HasIndex(e => e.LotId, "ix_transaction_lines_lot");

            entity.Property(e => e.TransactionLineId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("transaction_line_id");
            entity.Property(e => e.FromBinId).HasColumnName("from_bin_id");
            entity.Property(e => e.LineRemarks).HasColumnName("line_remarks");
            entity.Property(e => e.LotId).HasColumnName("lot_id");
            entity.Property(e => e.Quantity)
                .HasPrecision(18, 6)
                .HasColumnName("quantity");
            entity.Property(e => e.ToBinId).HasColumnName("to_bin_id");
            entity.Property(e => e.TransactionId).HasColumnName("transaction_id");
            entity.Property(e => e.UomId).HasColumnName("uom_id");

            entity.HasOne(d => d.FromBin).WithMany(p => p.InventoryTransactionLineFromBins)
                .HasForeignKey(d => d.FromBinId)
                .HasConstraintName("inventory_transaction_lines_from_bin_id_fkey");

            entity.HasOne(d => d.Lot).WithMany(p => p.InventoryTransactionLines)
                .HasForeignKey(d => d.LotId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("inventory_transaction_lines_lot_id_fkey");

            entity.HasOne(d => d.ToBin).WithMany(p => p.InventoryTransactionLineToBins)
                .HasForeignKey(d => d.ToBinId)
                .HasConstraintName("inventory_transaction_lines_to_bin_id_fkey");

            entity.HasOne(d => d.Transaction).WithMany(p => p.InventoryTransactionLines)
                .HasForeignKey(d => d.TransactionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("inventory_transaction_lines_transaction_id_fkey");

            entity.HasOne(d => d.Uom).WithMany(p => p.InventoryTransactionLines)
                .HasForeignKey(d => d.UomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("inventory_transaction_lines_uom_id_fkey");
        });

        modelBuilder.Entity<Item>(entity =>
        {
            entity.HasKey(e => e.ItemId).HasName("items_pkey");

            entity.ToTable("items");

            entity.HasIndex(e => e.ItemCode, "items_item_code_key").IsUnique();

            entity.HasIndex(e => e.CategoryId, "ix_items_category");

            entity.Property(e => e.ItemId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("item_id");
            entity.Property(e => e.BaseUomId).HasColumnName("base_uom_id");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.ItemCode)
                .HasMaxLength(50)
                .HasColumnName("item_code");
            entity.Property(e => e.ItemName)
                .HasMaxLength(200)
                .HasColumnName("item_name");
            entity.Property(e => e.ReorderLevel)
                .HasPrecision(18, 6)
                .HasColumnName("reorder_level");
            entity.Property(e => e.RequiresExpiry).HasColumnName("requires_expiry");
            entity.Property(e => e.RequiresQualityInspection).HasColumnName("requires_quality_inspection");
            entity.Property(e => e.StockRotationMethod)
                .HasMaxLength(10)
                .HasDefaultValueSql("'FIFO'::character varying")
                .HasColumnName("stock_rotation_method");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.BaseUom).WithMany(p => p.Items)
                .HasForeignKey(d => d.BaseUomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("items_base_uom_id_fkey");

            entity.HasOne(d => d.Category).WithMany(p => p.Items)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("items_category_id_fkey");
        });

        modelBuilder.Entity<ItemBarcode>(entity =>
        {
            entity.HasKey(e => e.BarcodeId).HasName("item_barcodes_pkey");

            entity.ToTable("item_barcodes");

            entity.HasIndex(e => e.BarcodeValue, "item_barcodes_barcode_value_key").IsUnique();

            entity.HasIndex(e => e.ItemId, "uq_item_primary_barcode")
                .IsUnique()
                .HasFilter("(is_primary AND is_active)");

            entity.Property(e => e.BarcodeId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("barcode_id");
            entity.Property(e => e.BarcodeType)
                .HasMaxLength(30)
                .HasDefaultValueSql("'CODE128'::character varying")
                .HasColumnName("barcode_type");
            entity.Property(e => e.BarcodeValue)
                .HasMaxLength(120)
                .HasColumnName("barcode_value");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.IsPrimary).HasColumnName("is_primary");
            entity.Property(e => e.ItemId).HasColumnName("item_id");

            entity.HasOne(d => d.Item).WithOne(p => p.ItemBarcode)
                .HasForeignKey<ItemBarcode>(d => d.ItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("item_barcodes_item_id_fkey");
        });

        modelBuilder.Entity<ItemCategory>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("item_categories_pkey");

            entity.ToTable("item_categories");

            entity.HasIndex(e => e.CategoryCode, "item_categories_category_code_key").IsUnique();

            entity.HasIndex(e => e.CategoryName, "item_categories_category_name_key").IsUnique();

            entity.Property(e => e.CategoryId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("category_id");
            entity.Property(e => e.CategoryCode)
                .HasMaxLength(30)
                .HasColumnName("category_code");
            entity.Property(e => e.CategoryName)
                .HasMaxLength(100)
                .HasColumnName("category_name");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
        });

        modelBuilder.Entity<LocationBarcode>(entity =>
        {
            entity.HasKey(e => e.LocationBarcodeId).HasName("location_barcodes_pkey");

            entity.ToTable("location_barcodes");

            entity.HasIndex(e => e.BarcodeValue, "location_barcodes_barcode_value_key").IsUnique();

            entity.Property(e => e.LocationBarcodeId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("location_barcode_id");
            entity.Property(e => e.BarcodeType)
                .HasMaxLength(30)
                .HasDefaultValueSql("'CODE128'::character varying")
                .HasColumnName("barcode_type");
            entity.Property(e => e.BarcodeValue)
                .HasMaxLength(120)
                .HasColumnName("barcode_value");
            entity.Property(e => e.BinId).HasColumnName("bin_id");

            entity.HasOne(d => d.Bin).WithMany(p => p.LocationBarcodes)
                .HasForeignKey(d => d.BinId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("location_barcodes_bin_id_fkey");
        });

        modelBuilder.Entity<LotBarcode>(entity =>
        {
            entity.HasKey(e => e.LotBarcodeId).HasName("lot_barcodes_pkey");

            entity.ToTable("lot_barcodes");

            entity.HasIndex(e => e.BarcodeValue, "lot_barcodes_barcode_value_key").IsUnique();

            entity.Property(e => e.LotBarcodeId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("lot_barcode_id");
            entity.Property(e => e.BarcodeType)
                .HasMaxLength(30)
                .HasDefaultValueSql("'CODE128'::character varying")
                .HasColumnName("barcode_type");
            entity.Property(e => e.BarcodeValue)
                .HasMaxLength(120)
                .HasColumnName("barcode_value");
            entity.Property(e => e.LotId).HasColumnName("lot_id");

            entity.HasOne(d => d.Lot).WithMany(p => p.LotBarcodes)
                .HasForeignKey(d => d.LotId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("lot_barcodes_lot_id_fkey");
        });

        modelBuilder.Entity<ProductionBatchGenealogy>(entity =>
        {
            entity.HasKey(e => e.GenealogyId).HasName("production_batch_genealogy_pkey");

            entity.ToTable("production_batch_genealogy");

            entity.Property(e => e.GenealogyId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("genealogy_id");
            entity.Property(e => e.ConsumedQuantity)
                .HasPrecision(18, 6)
                .HasColumnName("consumed_quantity");
            entity.Property(e => e.InputLotId).HasColumnName("input_lot_id");
            entity.Property(e => e.OutputLotId).HasColumnName("output_lot_id");
            entity.Property(e => e.ProducedQuantity)
                .HasPrecision(18, 6)
                .HasColumnName("produced_quantity");
            entity.Property(e => e.ProductionOrderId).HasColumnName("production_order_id");
            entity.Property(e => e.UomId).HasColumnName("uom_id");

            entity.HasOne(d => d.InputLot).WithMany(p => p.ProductionBatchGenealogyInputLots)
                .HasForeignKey(d => d.InputLotId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("production_batch_genealogy_input_lot_id_fkey");

            entity.HasOne(d => d.OutputLot).WithMany(p => p.ProductionBatchGenealogyOutputLots)
                .HasForeignKey(d => d.OutputLotId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("production_batch_genealogy_output_lot_id_fkey");

            entity.HasOne(d => d.ProductionOrder).WithMany(p => p.ProductionBatchGenealogies)
                .HasForeignKey(d => d.ProductionOrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("production_batch_genealogy_production_order_id_fkey");

            entity.HasOne(d => d.Uom).WithMany(p => p.ProductionBatchGenealogies)
                .HasForeignKey(d => d.UomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("production_batch_genealogy_uom_id_fkey");
        });

        modelBuilder.Entity<ProductionConsumption>(entity =>
        {
            entity.HasKey(e => e.ConsumptionId).HasName("production_consumptions_pkey");

            entity.ToTable("production_consumptions");

            entity.Property(e => e.ConsumptionId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("consumption_id");
            entity.Property(e => e.ConsumedQuantity)
                .HasPrecision(18, 6)
                .HasColumnName("consumed_quantity");
            entity.Property(e => e.ConsumptionDate)
                .HasDefaultValueSql("now()")
                .HasColumnName("consumption_date");
            entity.Property(e => e.LotId).HasColumnName("lot_id");
            entity.Property(e => e.ProductionOrderId).HasColumnName("production_order_id");
            entity.Property(e => e.RecordedBy).HasColumnName("recorded_by");
            entity.Property(e => e.UomId).HasColumnName("uom_id");

            entity.HasOne(d => d.Lot).WithMany(p => p.ProductionConsumptions)
                .HasForeignKey(d => d.LotId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("production_consumptions_lot_id_fkey");

            entity.HasOne(d => d.ProductionOrder).WithMany(p => p.ProductionConsumptions)
                .HasForeignKey(d => d.ProductionOrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("production_consumptions_production_order_id_fkey");

            entity.HasOne(d => d.RecordedByNavigation).WithMany(p => p.ProductionConsumptions)
                .HasForeignKey(d => d.RecordedBy)
                .HasConstraintName("production_consumptions_recorded_by_fkey");

            entity.HasOne(d => d.Uom).WithMany(p => p.ProductionConsumptions)
                .HasForeignKey(d => d.UomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("production_consumptions_uom_id_fkey");
        });

        modelBuilder.Entity<ProductionMaterialIssue>(entity =>
        {
            entity.HasKey(e => e.IssueId).HasName("production_material_issues_pkey");

            entity.ToTable("production_material_issues");

            entity.HasIndex(e => e.IssueNumber, "production_material_issues_issue_number_key").IsUnique();

            entity.Property(e => e.IssueId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("issue_id");
            entity.Property(e => e.IssueDate)
                .HasDefaultValueSql("now()")
                .HasColumnName("issue_date");
            entity.Property(e => e.IssueNumber)
                .HasMaxLength(50)
                .HasColumnName("issue_number");
            entity.Property(e => e.IssuedBy).HasColumnName("issued_by");
            entity.Property(e => e.ProductionOrderId).HasColumnName("production_order_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'DRAFT'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.WarehouseId).HasColumnName("warehouse_id");

            entity.HasOne(d => d.IssuedByNavigation).WithMany(p => p.ProductionMaterialIssues)
                .HasForeignKey(d => d.IssuedBy)
                .HasConstraintName("production_material_issues_issued_by_fkey");

            entity.HasOne(d => d.ProductionOrder).WithMany(p => p.ProductionMaterialIssues)
                .HasForeignKey(d => d.ProductionOrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("production_material_issues_production_order_id_fkey");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.ProductionMaterialIssues)
                .HasForeignKey(d => d.WarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("production_material_issues_warehouse_id_fkey");
        });

        modelBuilder.Entity<ProductionMaterialIssueLine>(entity =>
        {
            entity.HasKey(e => e.IssueLineId).HasName("production_material_issue_lines_pkey");

            entity.ToTable("production_material_issue_lines");

            entity.Property(e => e.IssueLineId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("issue_line_id");
            entity.Property(e => e.BinId).HasColumnName("bin_id");
            entity.Property(e => e.IssueId).HasColumnName("issue_id");
            entity.Property(e => e.IssuedQuantity)
                .HasPrecision(18, 6)
                .HasColumnName("issued_quantity");
            entity.Property(e => e.LotId).HasColumnName("lot_id");
            entity.Property(e => e.UomId).HasColumnName("uom_id");

            entity.HasOne(d => d.Bin).WithMany(p => p.ProductionMaterialIssueLines)
                .HasForeignKey(d => d.BinId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("production_material_issue_lines_bin_id_fkey");

            entity.HasOne(d => d.Issue).WithMany(p => p.ProductionMaterialIssueLines)
                .HasForeignKey(d => d.IssueId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("production_material_issue_lines_issue_id_fkey");

            entity.HasOne(d => d.Lot).WithMany(p => p.ProductionMaterialIssueLines)
                .HasForeignKey(d => d.LotId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("production_material_issue_lines_lot_id_fkey");

            entity.HasOne(d => d.Uom).WithMany(p => p.ProductionMaterialIssueLines)
                .HasForeignKey(d => d.UomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("production_material_issue_lines_uom_id_fkey");
        });



        modelBuilder.Entity<ProductionMaterialRequirement>(entity =>
        {
            entity.HasKey(e => e.RequirementId).HasName("production_material_requirements_pkey");

            entity.ToTable("production_material_requirements");

            entity.HasIndex(e => new { e.ProductionOrderId, e.ItemId }, "production_material_requirement_production_order_id_item_id_key").IsUnique();

            entity.Property(e => e.RequirementId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("requirement_id");
            entity.Property(e => e.ConsumedQuantity)
                .HasPrecision(18, 6)
                .HasColumnName("consumed_quantity");
            entity.Property(e => e.IssuedQuantity)
                .HasPrecision(18, 6)
                .HasColumnName("issued_quantity");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.RequiredQuantity)
                .HasPrecision(18, 6)
                .HasColumnName("required_quantity");
            entity.Property(e => e.ProductionOrderId).HasColumnName("production_order_id");
            entity.Property(e => e.ReservedQuantity)
                .HasPrecision(18, 6)
                .HasColumnName("reserved_quantity");
            entity.Property(e => e.UomId).HasColumnName("uom_id");

            entity.HasOne(d => d.Item).WithMany(p => p.ProductionMaterialRequirements)
                .HasForeignKey(d => d.ItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("production_material_requirements_item_id_fkey");

            entity.HasOne(d => d.ProductionOrder).WithMany(p => p.ProductionMaterialRequirements)
                .HasForeignKey(d => d.ProductionOrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("production_material_requirements_production_order_id_fkey");

            entity.HasOne(d => d.Uom).WithMany(p => p.ProductionMaterialRequirements)
                .HasForeignKey(d => d.UomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("production_material_requirements_uom_id_fkey");
        });

        modelBuilder.Entity<ProductionOrder>(entity =>
        {
            entity.HasKey(e => e.ProductionOrderId).HasName("production_orders_pkey");

            entity.ToTable("production_orders");

            entity.HasIndex(e => e.ProductionStatus, "ix_production_orders_status");

            entity.HasIndex(e => e.WarehouseId, "ix_production_orders_warehouse");

            entity.HasIndex(e => e.ProductionNumber, "production_orders_production_number_key").IsUnique();

            entity.Property(e => e.ProductionOrderId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("production_order_id");
            entity.Property(e => e.ActualEndDate).HasColumnName("actual_end_date");
            entity.Property(e => e.ActualStartDate).HasColumnName("actual_start_date");
            entity.Property(e => e.BomId).HasColumnName("bom_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.FinishedItemId).HasColumnName("finished_item_id");
            entity.Property(e => e.PlannedEndDate).HasColumnName("planned_end_date");
            entity.Property(e => e.PlannedQuantity)
                .HasPrecision(18, 6)
                .HasColumnName("planned_quantity");
            entity.Property(e => e.PlannedStartDate).HasColumnName("planned_start_date");
            entity.Property(e => e.ProductionNumber)
                .HasMaxLength(50)
                .HasColumnName("production_number");
            entity.Property(e => e.ProductionStatus)
                .HasMaxLength(25)
                .HasDefaultValueSql("'DRAFT'::character varying")
                .HasColumnName("production_status");
            entity.Property(e => e.UomId).HasColumnName("uom_id");
            entity.Property(e => e.WarehouseId).HasColumnName("warehouse_id");

            entity.HasOne(d => d.Bom).WithMany(p => p.ProductionOrders)
                .HasForeignKey(d => d.BomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("production_orders_bom_id_fkey");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.ProductionOrders)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("production_orders_created_by_fkey");

            entity.HasOne(d => d.FinishedItem).WithMany(p => p.ProductionOrders)
                .HasForeignKey(d => d.FinishedItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("production_orders_finished_item_id_fkey");

            entity.HasOne(d => d.Uom).WithMany(p => p.ProductionOrders)
                .HasForeignKey(d => d.UomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("production_orders_uom_id_fkey");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.ProductionOrders)
                .HasForeignKey(d => d.WarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("production_orders_warehouse_id_fkey");
        });

        modelBuilder.Entity<ProductionOutput>(entity =>
        {
            entity.HasKey(e => e.OutputId).HasName("production_outputs_pkey");

            entity.ToTable("production_outputs");

            entity.Property(e => e.OutputId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("output_id");
            entity.Property(e => e.FinishedLotId).HasColumnName("finished_lot_id");
            entity.Property(e => e.ProducedQuantity)
                .HasPrecision(18, 6)
                .HasColumnName("produced_quantity");
            entity.Property(e => e.ProductionDate)
                .HasDefaultValueSql("now()")
                .HasColumnName("production_date");
            entity.Property(e => e.ProductionOrderId).HasColumnName("production_order_id");
            entity.Property(e => e.QualityStatus)
                .HasMaxLength(20)
                .HasDefaultValueSql("'PENDING'::character varying")
                .HasColumnName("quality_status");
            entity.Property(e => e.UomId).HasColumnName("uom_id");

            entity.HasOne(d => d.FinishedLot).WithMany(p => p.ProductionOutputs)
                .HasForeignKey(d => d.FinishedLotId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("production_outputs_finished_lot_id_fkey");

            entity.HasOne(d => d.ProductionOrder).WithMany(p => p.ProductionOutputs)
                .HasForeignKey(d => d.ProductionOrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("production_outputs_production_order_id_fkey");

            entity.HasOne(d => d.Uom).WithMany(p => p.ProductionOutputs)
                .HasForeignKey(d => d.UomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("production_outputs_uom_id_fkey");
        });

        modelBuilder.Entity<ProductionWaste>(entity =>
        {
            entity.HasKey(e => e.WasteId).HasName("production_waste_pkey");

            entity.ToTable("production_waste");

            entity.Property(e => e.WasteId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("waste_id");
            entity.Property(e => e.LotId).HasColumnName("lot_id");
            entity.Property(e => e.ProductionOrderId).HasColumnName("production_order_id");
            entity.Property(e => e.RecordedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("recorded_at");
            entity.Property(e => e.RecordedBy).HasColumnName("recorded_by");
            entity.Property(e => e.UomId).HasColumnName("uom_id");
            entity.Property(e => e.WasteReason).HasColumnName("waste_reason");
            entity.Property(e => e.WastedQuantity)
                .HasPrecision(18, 6)
                .HasColumnName("wasted_quantity");

            entity.HasOne(d => d.Lot).WithMany(p => p.ProductionWastes)
                .HasForeignKey(d => d.LotId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("production_waste_lot_id_fkey");

            entity.HasOne(d => d.ProductionOrder).WithMany(p => p.ProductionWastes)
                .HasForeignKey(d => d.ProductionOrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("production_waste_production_order_id_fkey");

            entity.HasOne(d => d.RecordedByNavigation).WithMany(p => p.ProductionWastes)
                .HasForeignKey(d => d.RecordedBy)
                .HasConstraintName("production_waste_recorded_by_fkey");

            entity.HasOne(d => d.Uom).WithMany(p => p.ProductionWastes)
                .HasForeignKey(d => d.UomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("production_waste_uom_id_fkey");
        });

        modelBuilder.Entity<PurchaseOrder>(entity =>
        {
            entity.HasKey(e => e.PurchaseOrderId).HasName("purchase_orders_pkey");

            entity.ToTable("purchase_orders");

            entity.HasIndex(e => e.PoNumber, "purchase_orders_po_number_key").IsUnique();

            entity.Property(e => e.PurchaseOrderId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("purchase_order_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.ExpectedDeliveryDate).HasColumnName("expected_delivery_date");
            entity.Property(e => e.OrderDate)
                .HasDefaultValueSql("CURRENT_DATE")
                .HasColumnName("order_date");
            entity.Property(e => e.PoNumber)
                .HasMaxLength(50)
                .HasColumnName("po_number");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'DRAFT'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.SupplierId).HasColumnName("supplier_id");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.PurchaseOrders)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("purchase_orders_created_by_fkey");

            entity.HasOne(d => d.Supplier).WithMany(p => p.PurchaseOrders)
                .HasForeignKey(d => d.SupplierId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("purchase_orders_supplier_id_fkey");
        });

        modelBuilder.Entity<PurchaseOrderLine>(entity =>
        {
            entity.HasKey(e => e.PoLineId).HasName("purchase_order_lines_pkey");

            entity.ToTable("purchase_order_lines");

            entity.Property(e => e.PoLineId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("po_line_id");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.OrderedQuantity)
                .HasPrecision(18, 6)
                .HasColumnName("ordered_quantity");
            entity.Property(e => e.PurchaseOrderId).HasColumnName("purchase_order_id");
            entity.Property(e => e.UnitCost)
                .HasPrecision(18, 6)
                .HasColumnName("unit_cost");
            entity.Property(e => e.UomId).HasColumnName("uom_id");

            entity.HasOne(d => d.Item).WithMany(p => p.PurchaseOrderLines)
                .HasForeignKey(d => d.ItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("purchase_order_lines_item_id_fkey");

            entity.HasOne(d => d.PurchaseOrder).WithMany(p => p.PurchaseOrderLines)
                .HasForeignKey(d => d.PurchaseOrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("purchase_order_lines_purchase_order_id_fkey");

            entity.HasOne(d => d.Uom).WithMany(p => p.PurchaseOrderLines)
                .HasForeignKey(d => d.UomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("purchase_order_lines_uom_id_fkey");
        });

        modelBuilder.Entity<PutawayTask>(entity =>
        {
            entity.HasKey(e => e.PutawayId).HasName("putaway_tasks_pkey");

            entity.ToTable("putaway_tasks");

            entity.Property(e => e.PutawayId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("putaway_id");
            entity.Property(e => e.AssignedTo).HasColumnName("assigned_to");
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
            entity.Property(e => e.FromBinId).HasColumnName("from_bin_id");
            entity.Property(e => e.LotId).HasColumnName("lot_id");
            entity.Property(e => e.Quantity)
                .HasPrecision(18, 6)
                .HasColumnName("quantity");
            entity.Property(e => e.ReceiptLineId).HasColumnName("receipt_line_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'PENDING'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.ToBinId).HasColumnName("to_bin_id");

            entity.HasOne(d => d.AssignedToNavigation).WithMany(p => p.PutawayTasks)
                .HasForeignKey(d => d.AssignedTo)
                .HasConstraintName("putaway_tasks_assigned_to_fkey");

            entity.HasOne(d => d.FromBin).WithMany(p => p.PutawayTaskFromBins)
                .HasForeignKey(d => d.FromBinId)
                .HasConstraintName("putaway_tasks_from_bin_id_fkey");

            entity.HasOne(d => d.Lot).WithMany(p => p.PutawayTasks)
                .HasForeignKey(d => d.LotId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("putaway_tasks_lot_id_fkey");

            entity.HasOne(d => d.ReceiptLine).WithMany(p => p.PutawayTasks)
                .HasForeignKey(d => d.ReceiptLineId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("putaway_tasks_receipt_line_id_fkey");

            entity.HasOne(d => d.ToBin).WithMany(p => p.PutawayTaskToBins)
                .HasForeignKey(d => d.ToBinId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("putaway_tasks_to_bin_id_fkey");
        });

        modelBuilder.Entity<QualityInspection>(entity =>
        {
            entity.HasKey(e => e.InspectionId).HasName("quality_inspections_pkey");

            entity.ToTable("quality_inspections");

            entity.HasIndex(e => e.LotId, "ix_quality_inspections_lot");

            entity.Property(e => e.InspectionId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("inspection_id");
            entity.Property(e => e.InspectedBy).HasColumnName("inspected_by");
            entity.Property(e => e.InspectionDate)
                .HasDefaultValueSql("now()")
                .HasColumnName("inspection_date");
            entity.Property(e => e.InspectionType)
                .HasMaxLength(30)
                .HasColumnName("inspection_type");
            entity.Property(e => e.LotId).HasColumnName("lot_id");
            entity.Property(e => e.OverallResult)
                .HasMaxLength(20)
                .HasDefaultValueSql("'PENDING'::character varying")
                .HasColumnName("overall_result");
            entity.Property(e => e.ProductionOrderId).HasColumnName("production_order_id");
            entity.Property(e => e.ReceiptId).HasColumnName("receipt_id");
            entity.Property(e => e.Remarks).HasColumnName("remarks");

            entity.HasOne(d => d.InspectedByNavigation).WithMany(p => p.QualityInspections)
                .HasForeignKey(d => d.InspectedBy)
                .HasConstraintName("quality_inspections_inspected_by_fkey");

            entity.HasOne(d => d.Lot).WithMany(p => p.QualityInspections)
                .HasForeignKey(d => d.LotId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("quality_inspections_lot_id_fkey");

            entity.HasOne(d => d.ProductionOrder).WithMany(p => p.QualityInspections)
                .HasForeignKey(d => d.ProductionOrderId)
                .HasConstraintName("quality_inspections_production_order_id_fkey");

            entity.HasOne(d => d.Receipt).WithMany(p => p.QualityInspections)
                .HasForeignKey(d => d.ReceiptId)
                .HasConstraintName("quality_inspections_receipt_id_fkey");
        });

        modelBuilder.Entity<QualityInspectionLine>(entity =>
        {
            entity.HasKey(e => e.InspectionLineId).HasName("quality_inspection_lines_pkey");

            entity.ToTable("quality_inspection_lines");

            entity.Property(e => e.InspectionLineId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("inspection_line_id");
            entity.Property(e => e.InspectionId).HasColumnName("inspection_id");
            entity.Property(e => e.MaximumValue)
                .HasPrecision(18, 6)
                .HasColumnName("maximum_value");
            entity.Property(e => e.MeasuredValue)
                .HasPrecision(18, 6)
                .HasColumnName("measured_value");
            entity.Property(e => e.MinimumValue)
                .HasPrecision(18, 6)
                .HasColumnName("minimum_value");
            entity.Property(e => e.ParameterId).HasColumnName("parameter_id");
            entity.Property(e => e.Remarks).HasColumnName("remarks");
            entity.Property(e => e.Result)
                .HasMaxLength(20)
                .HasColumnName("result");

            entity.HasOne(d => d.Inspection).WithMany(p => p.QualityInspectionLines)
                .HasForeignKey(d => d.InspectionId)
                .HasConstraintName("quality_inspection_lines_inspection_id_fkey");

            entity.HasOne(d => d.Parameter).WithMany(p => p.QualityInspectionLines)
                .HasForeignKey(d => d.ParameterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("quality_inspection_lines_parameter_id_fkey");
        });

        modelBuilder.Entity<QualityParameter>(entity =>
        {
            entity.HasKey(e => e.ParameterId).HasName("quality_parameters_pkey");

            entity.ToTable("quality_parameters");

            entity.HasIndex(e => e.ParameterCode, "quality_parameters_parameter_code_key").IsUnique();

            entity.Property(e => e.ParameterId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("parameter_id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.ParameterCode)
                .HasMaxLength(40)
                .HasColumnName("parameter_code");
            entity.Property(e => e.ParameterName)
                .HasMaxLength(120)
                .HasColumnName("parameter_name");
            entity.Property(e => e.UnitOfMeasure)
                .HasMaxLength(40)
                .HasColumnName("unit_of_measure");
        });

        modelBuilder.Entity<QualityStatusHistory>(entity =>
        {
            entity.HasKey(e => e.HistoryId).HasName("quality_status_history_pkey");

            entity.ToTable("quality_status_history");

            entity.Property(e => e.HistoryId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("history_id");
            entity.Property(e => e.ChangedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("changed_at");
            entity.Property(e => e.ChangedBy).HasColumnName("changed_by");
            entity.Property(e => e.LotId).HasColumnName("lot_id");
            entity.Property(e => e.NewStatus)
                .HasMaxLength(20)
                .HasColumnName("new_status");
            entity.Property(e => e.PreviousStatus)
                .HasMaxLength(20)
                .HasColumnName("previous_status");
            entity.Property(e => e.Reason).HasColumnName("reason");

            entity.HasOne(d => d.ChangedByNavigation).WithMany(p => p.QualityStatusHistories)
                .HasForeignKey(d => d.ChangedBy)
                .HasConstraintName("quality_status_history_changed_by_fkey");

            entity.HasOne(d => d.Lot).WithMany(p => p.QualityStatusHistories)
                .HasForeignKey(d => d.LotId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("quality_status_history_lot_id_fkey");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("roles_pkey");

            entity.ToTable("roles");

            entity.HasIndex(e => e.RoleName, "roles_role_name_key").IsUnique();

            entity.Property(e => e.RoleId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("role_id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.RoleName)
                .HasMaxLength(80)
                .HasColumnName("role_name");
        });

        modelBuilder.Entity<StockAdjustment>(entity =>
        {
            entity.HasKey(e => e.AdjustmentId).HasName("stock_adjustments_pkey");

            entity.ToTable("stock_adjustments");

            entity.HasIndex(e => e.AdjustmentNumber, "stock_adjustments_adjustment_number_key").IsUnique();

            entity.Property(e => e.AdjustmentId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("adjustment_id");
            entity.Property(e => e.AdjustmentNumber)
                .HasMaxLength(50)
                .HasColumnName("adjustment_number");
            entity.Property(e => e.AdjustmentReason)
                .HasMaxLength(200)
                .HasColumnName("adjustment_reason");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.PostedAt).HasColumnName("posted_at");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'DRAFT'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.WarehouseId).HasColumnName("warehouse_id");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.StockAdjustments)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("stock_adjustments_created_by_fkey");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.StockAdjustments)
                .HasForeignKey(d => d.WarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("stock_adjustments_warehouse_id_fkey");
        });

        modelBuilder.Entity<StockAdjustmentLine>(entity =>
        {
            entity.HasKey(e => e.AdjustmentLineId).HasName("stock_adjustment_lines_pkey");

            entity.ToTable("stock_adjustment_lines");

            entity.Property(e => e.AdjustmentLineId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("adjustment_line_id");
            entity.Property(e => e.AdjustmentId).HasColumnName("adjustment_id");
            entity.Property(e => e.BinId).HasColumnName("bin_id");
            entity.Property(e => e.CountedQuantity)
                .HasPrecision(18, 6)
                .HasColumnName("counted_quantity");
            entity.Property(e => e.DifferenceQuantity)
                .HasPrecision(18, 6)
                .HasColumnName("difference_quantity");
            entity.Property(e => e.LotId).HasColumnName("lot_id");
            entity.Property(e => e.SystemQuantity)
                .HasPrecision(18, 6)
                .HasColumnName("system_quantity");

            entity.HasOne(d => d.Adjustment).WithMany(p => p.StockAdjustmentLines)
                .HasForeignKey(d => d.AdjustmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("stock_adjustment_lines_adjustment_id_fkey");

            entity.HasOne(d => d.Bin).WithMany(p => p.StockAdjustmentLines)
                .HasForeignKey(d => d.BinId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("stock_adjustment_lines_bin_id_fkey");

            entity.HasOne(d => d.Lot).WithMany(p => p.StockAdjustmentLines)
                .HasForeignKey(d => d.LotId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("stock_adjustment_lines_lot_id_fkey");
        });

        modelBuilder.Entity<StockCount>(entity =>
        {
            entity.HasKey(e => e.StockCountId).HasName("stock_counts_pkey");

            entity.ToTable("stock_counts");

            entity.HasIndex(e => e.CountNumber, "stock_counts_count_number_key").IsUnique();

            entity.Property(e => e.StockCountId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("stock_count_id");
            entity.Property(e => e.CountDate)
                .HasDefaultValueSql("now()")
                .HasColumnName("count_date");
            entity.Property(e => e.CountNumber)
                .HasMaxLength(50)
                .HasColumnName("count_number");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'OPEN'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.WarehouseId).HasColumnName("warehouse_id");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.StockCounts)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("stock_counts_created_by_fkey");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.StockCounts)
                .HasForeignKey(d => d.WarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("stock_counts_warehouse_id_fkey");
        });

        modelBuilder.Entity<StockCountLine>(entity =>
        {
            entity.HasKey(e => e.StockCountLineId).HasName("stock_count_lines_pkey");

            entity.ToTable("stock_count_lines");

            entity.HasIndex(e => new { e.StockCountId, e.LotId, e.BinId }, "stock_count_lines_stock_count_id_lot_id_bin_id_key").IsUnique();

            entity.Property(e => e.StockCountLineId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("stock_count_line_id");
            entity.Property(e => e.BinId).HasColumnName("bin_id");
            entity.Property(e => e.CountedAt).HasColumnName("counted_at");
            entity.Property(e => e.CountedBy).HasColumnName("counted_by");
            entity.Property(e => e.CountedQuantity)
                .HasPrecision(18, 6)
                .HasColumnName("counted_quantity");
            entity.Property(e => e.LotId).HasColumnName("lot_id");
            entity.Property(e => e.StockCountId).HasColumnName("stock_count_id");
            entity.Property(e => e.SystemQuantity)
                .HasPrecision(18, 6)
                .HasColumnName("system_quantity");

            entity.HasOne(d => d.Bin).WithMany(p => p.StockCountLines)
                .HasForeignKey(d => d.BinId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("stock_count_lines_bin_id_fkey");

            entity.HasOne(d => d.CountedByNavigation).WithMany(p => p.StockCountLines)
                .HasForeignKey(d => d.CountedBy)
                .HasConstraintName("stock_count_lines_counted_by_fkey");

            entity.HasOne(d => d.Lot).WithMany(p => p.StockCountLines)
                .HasForeignKey(d => d.LotId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("stock_count_lines_lot_id_fkey");

            entity.HasOne(d => d.StockCount).WithMany(p => p.StockCountLines)
                .HasForeignKey(d => d.StockCountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("stock_count_lines_stock_count_id_fkey");
        });

        modelBuilder.Entity<StockTransfer>(entity =>
        {
            entity.HasKey(e => e.TransferId).HasName("stock_transfers_pkey");

            entity.ToTable("stock_transfers");

            entity.HasIndex(e => e.Status, "ix_transfers_status");

            entity.HasIndex(e => e.TransferNumber, "stock_transfers_transfer_number_key").IsUnique();

            entity.Property(e => e.TransferId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("transfer_id");
            entity.Property(e => e.DestinationWarehouseId).HasColumnName("destination_warehouse_id");
            entity.Property(e => e.DispatchedAt).HasColumnName("dispatched_at");
            entity.Property(e => e.DispatchedBy).HasColumnName("dispatched_by");
            entity.Property(e => e.ReceivedAt).HasColumnName("received_at");
            entity.Property(e => e.ReceivedBy).HasColumnName("received_by");
            entity.Property(e => e.RequestedBy).HasColumnName("requested_by");
            entity.Property(e => e.SourceWarehouseId).HasColumnName("source_warehouse_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'DRAFT'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.TransferDate)
                .HasDefaultValueSql("now()")
                .HasColumnName("transfer_date");
            entity.Property(e => e.TransferNumber)
                .HasMaxLength(50)
                .HasColumnName("transfer_number");

            entity.HasOne(d => d.DestinationWarehouse).WithMany(p => p.StockTransferDestinationWarehouses)
                .HasForeignKey(d => d.DestinationWarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("stock_transfers_destination_warehouse_id_fkey");

            entity.HasOne(d => d.DispatchedByNavigation).WithMany(p => p.StockTransferDispatchedByNavigations)
                .HasForeignKey(d => d.DispatchedBy)
                .HasConstraintName("stock_transfers_dispatched_by_fkey");

            entity.HasOne(d => d.ReceivedByNavigation).WithMany(p => p.StockTransferReceivedByNavigations)
                .HasForeignKey(d => d.ReceivedBy)
                .HasConstraintName("stock_transfers_received_by_fkey");

            entity.HasOne(d => d.RequestedByNavigation).WithMany(p => p.StockTransferRequestedByNavigations)
                .HasForeignKey(d => d.RequestedBy)
                .HasConstraintName("stock_transfers_requested_by_fkey");

            entity.HasOne(d => d.SourceWarehouse).WithMany(p => p.StockTransferSourceWarehouses)
                .HasForeignKey(d => d.SourceWarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("stock_transfers_source_warehouse_id_fkey");
        });

        modelBuilder.Entity<StockTransferLine>(entity =>
        {
            entity.HasKey(e => e.TransferLineId).HasName("stock_transfer_lines_pkey");

            entity.ToTable("stock_transfer_lines");

            entity.Property(e => e.TransferLineId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("transfer_line_id");
            entity.Property(e => e.DestinationBinId).HasColumnName("destination_bin_id");
            entity.Property(e => e.DiscrepancyReason).HasColumnName("discrepancy_reason");
            entity.Property(e => e.DispatchedQuantity)
                .HasPrecision(18, 6)
                .HasColumnName("dispatched_quantity");
            entity.Property(e => e.LotId).HasColumnName("lot_id");
            entity.Property(e => e.ReceivedQuantity)
                .HasPrecision(18, 6)
                .HasColumnName("received_quantity");
            entity.Property(e => e.RequestedQuantity)
                .HasPrecision(18, 6)
                .HasColumnName("requested_quantity");
            entity.Property(e => e.SourceBinId).HasColumnName("source_bin_id");
            entity.Property(e => e.TransferId).HasColumnName("transfer_id");
            entity.Property(e => e.UomId).HasColumnName("uom_id");

            entity.HasOne(d => d.DestinationBin).WithMany(p => p.StockTransferLineDestinationBins)
                .HasForeignKey(d => d.DestinationBinId)
                .HasConstraintName("stock_transfer_lines_destination_bin_id_fkey");

            entity.HasOne(d => d.Lot).WithMany(p => p.StockTransferLines)
                .HasForeignKey(d => d.LotId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("stock_transfer_lines_lot_id_fkey");

            entity.HasOne(d => d.SourceBin).WithMany(p => p.StockTransferLineSourceBins)
                .HasForeignKey(d => d.SourceBinId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("stock_transfer_lines_source_bin_id_fkey");

            entity.HasOne(d => d.Transfer).WithMany(p => p.StockTransferLines)
                .HasForeignKey(d => d.TransferId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("stock_transfer_lines_transfer_id_fkey");

            entity.HasOne(d => d.Uom).WithMany(p => p.StockTransferLines)
                .HasForeignKey(d => d.UomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("stock_transfer_lines_uom_id_fkey");
        });

        modelBuilder.Entity<StorageBin>(entity =>
        {
            entity.HasKey(e => e.BinId).HasName("storage_bins_pkey");

            entity.ToTable("storage_bins");

            entity.HasIndex(e => new { e.ZoneId, e.BinCode }, "storage_bins_zone_id_bin_code_key").IsUnique();

            entity.Property(e => e.BinId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("bin_id");
            entity.Property(e => e.BinCode)
                .HasMaxLength(50)
                .HasColumnName("bin_code");
            entity.Property(e => e.BinType)
                .HasMaxLength(30)
                .HasDefaultValueSql("'STORAGE'::character varying")
                .HasColumnName("bin_type");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.ZoneId).HasColumnName("zone_id");

            entity.HasOne(d => d.Zone).WithMany(p => p.StorageBins)
                .HasForeignKey(d => d.ZoneId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("storage_bins_zone_id_fkey");
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(e => e.SupplierId).HasName("suppliers_pkey");

            entity.ToTable("suppliers");

            entity.HasIndex(e => e.SupplierCode, "suppliers_supplier_code_key").IsUnique();

            entity.Property(e => e.SupplierId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("supplier_id");
            entity.Property(e => e.Address).HasColumnName("address");
            entity.Property(e => e.ContactPerson)
                .HasMaxLength(150)
                .HasColumnName("contact_person");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(150)
                .HasColumnName("email");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Phone)
                .HasMaxLength(50)
                .HasColumnName("phone");
            entity.Property(e => e.SupplierCode)
                .HasMaxLength(40)
                .HasColumnName("supplier_code");
            entity.Property(e => e.SupplierName)
                .HasMaxLength(200)
                .HasColumnName("supplier_name");
        });

        modelBuilder.Entity<SystemNotification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("system_notifications_pkey");

            entity.ToTable("system_notifications");

            entity.Property(e => e.NotificationId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("notification_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.IsRead).HasColumnName("is_read");
            entity.Property(e => e.Message).HasColumnName("message");
            entity.Property(e => e.NotificationType)
                .HasMaxLength(50)
                .HasColumnName("notification_type");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.SystemNotifications)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("system_notifications_user_id_fkey");
        });

        modelBuilder.Entity<UnitsOfMeasure>(entity =>
        {
            entity.HasKey(e => e.UomId).HasName("units_of_measure_pkey");

            entity.ToTable("units_of_measure");

            entity.HasIndex(e => e.UomCode, "units_of_measure_uom_code_key").IsUnique();

            entity.Property(e => e.UomId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("uom_id");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.UnitType)
                .HasMaxLength(30)
                .HasColumnName("unit_type");
            entity.Property(e => e.UomCode)
                .HasMaxLength(20)
                .HasColumnName("uom_code");
            entity.Property(e => e.UomName)
                .HasMaxLength(80)
                .HasColumnName("uom_name");
        });

        modelBuilder.Entity<UomConversion>(entity =>
        {
            entity.HasKey(e => e.ConversionId).HasName("uom_conversions_pkey");

            entity.ToTable("uom_conversions");

            entity.HasIndex(e => new { e.FromUomId, e.ToUomId }, "uom_conversions_from_uom_id_to_uom_id_key").IsUnique();

            entity.Property(e => e.ConversionId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("conversion_id");
            entity.Property(e => e.ConversionFactor)
                .HasPrecision(18, 6)
                .HasColumnName("conversion_factor");
            entity.Property(e => e.FromUomId).HasColumnName("from_uom_id");
            entity.Property(e => e.ToUomId).HasColumnName("to_uom_id");

            entity.HasOne(d => d.FromUom).WithMany(p => p.UomConversionFromUoms)
                .HasForeignKey(d => d.FromUomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("uom_conversions_from_uom_id_fkey");

            entity.HasOne(d => d.ToUom).WithMany(p => p.UomConversionToUoms)
                .HasForeignKey(d => d.ToUomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("uom_conversions_to_uom_id_fkey");
        });

        modelBuilder.Entity<Warehouse>(entity =>
        {
            entity.HasKey(e => e.WarehouseId).HasName("warehouses_pkey");

            entity.ToTable("warehouses");

            entity.HasIndex(e => e.WarehouseCode, "warehouses_warehouse_code_key").IsUnique();

            entity.Property(e => e.WarehouseId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("warehouse_id");
            entity.Property(e => e.Address).HasColumnName("address");
            entity.Property(e => e.City)
                .HasMaxLength(100)
                .HasColumnName("city");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.WarehouseCode)
                .HasMaxLength(40)
                .HasColumnName("warehouse_code");
            entity.Property(e => e.WarehouseName)
                .HasMaxLength(150)                .HasColumnName("warehouse_name");
        });

        modelBuilder.Entity<WarehouseZone>(entity =>
        {
            entity.HasKey(e => e.ZoneId).HasName("warehouse_zones_pkey");

            entity.ToTable("warehouse_zones");

            entity.HasIndex(e => new { e.WarehouseId, e.ZoneCode }, "warehouse_zones_warehouse_id_zone_code_key").IsUnique();

            entity.Property(e => e.ZoneId)
                .UseIdentityAlwaysColumn()
                .HasColumnName("zone_id");
            entity.Property(e => e.WarehouseId).HasColumnName("warehouse_id");
            entity.Property(e => e.ZoneCode)
                .HasMaxLength(40)
                .HasColumnName("zone_code");
            entity.Property(e => e.ZoneName)
                .HasMaxLength(100)
                .HasColumnName("zone_name");

            entity.HasOne(d => d.Warehouse).WithMany(p => p.WarehouseZones)
                .HasForeignKey(d => d.WarehouseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("warehouse_zones_warehouse_id_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
