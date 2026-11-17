using System;
using System.Collections.Generic;

namespace WMS.API.Models;

public partial class AppUser
{
    public long UserId { get; set; }

    public string? EmployeeCode { get; set; }

    public string FullName { get; set; } = null!;

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? Email { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    public virtual ICollection<GoodsReceipt> GoodsReceipts { get; set; } = new List<GoodsReceipt>();

    public virtual ICollection<InventoryReservation> InventoryReservations { get; set; } = new List<InventoryReservation>();

    public virtual ICollection<InventoryTransaction> InventoryTransactionCreatedByNavigations { get; set; } = new List<InventoryTransaction>();

    public virtual ICollection<InventoryTransaction> InventoryTransactionPostedByNavigations { get; set; } = new List<InventoryTransaction>();

    public virtual ICollection<ProductionConsumption> ProductionConsumptions { get; set; } = new List<ProductionConsumption>();

    public virtual ICollection<ProductionMaterialIssue> ProductionMaterialIssues { get; set; } = new List<ProductionMaterialIssue>();

    public virtual ICollection<ProductionOrder> ProductionOrders { get; set; } = new List<ProductionOrder>();

    public virtual ICollection<ProductionWaste> ProductionWastes { get; set; } = new List<ProductionWaste>();

    public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();

    public virtual ICollection<PutawayTask> PutawayTasks { get; set; } = new List<PutawayTask>();

    public virtual ICollection<QualityInspection> QualityInspections { get; set; } = new List<QualityInspection>();

    public virtual ICollection<QualityStatusHistory> QualityStatusHistories { get; set; } = new List<QualityStatusHistory>();

    public virtual ICollection<StockAdjustment> StockAdjustments { get; set; } = new List<StockAdjustment>();

    public virtual ICollection<StockCountLine> StockCountLines { get; set; } = new List<StockCountLine>();

    public virtual ICollection<StockCount> StockCounts { get; set; } = new List<StockCount>();

    public virtual ICollection<StockTransfer> StockTransferDispatchedByNavigations { get; set; } = new List<StockTransfer>();

    public virtual ICollection<StockTransfer> StockTransferReceivedByNavigations { get; set; } = new List<StockTransfer>();

    public virtual ICollection<StockTransfer> StockTransferRequestedByNavigations { get; set; } = new List<StockTransfer>();

    public virtual ICollection<SystemNotification> SystemNotifications { get; set; } = new List<SystemNotification>();

    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();

    public virtual ICollection<Warehouse> Warehouses { get; set; } = new List<Warehouse>();
}
