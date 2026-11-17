using System;
using System.Collections.Generic;

namespace WMS.API.Models;

public partial class Warehouse
{
    public long WarehouseId { get; set; }

    public string WarehouseCode { get; set; } = null!;

    public string WarehouseName { get; set; } = null!;

    public string? City { get; set; }

    public string? Address { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<GoodsReceipt> GoodsReceipts { get; set; } = new List<GoodsReceipt>();

    public virtual ICollection<ProductionMaterialIssue> ProductionMaterialIssues { get; set; } = new List<ProductionMaterialIssue>();

    public virtual ICollection<ProductionOrder> ProductionOrders { get; set; } = new List<ProductionOrder>();

    public virtual ICollection<StockAdjustment> StockAdjustments { get; set; } = new List<StockAdjustment>();

    public virtual ICollection<StockCount> StockCounts { get; set; } = new List<StockCount>();

    public virtual ICollection<StockTransfer> StockTransferDestinationWarehouses { get; set; } = new List<StockTransfer>();

    public virtual ICollection<StockTransfer> StockTransferSourceWarehouses { get; set; } = new List<StockTransfer>();

    public virtual ICollection<WarehouseZone> WarehouseZones { get; set; } = new List<WarehouseZone>();

    public virtual ICollection<AppUser> Users { get; set; } = new List<AppUser>();
}
