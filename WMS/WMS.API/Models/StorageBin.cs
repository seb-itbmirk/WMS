using System;
using System.Collections.Generic;

namespace WMS.API.Models;

public partial class StorageBin
{
    public long BinId { get; set; }

    public long ZoneId { get; set; }

    public string BinCode { get; set; } = null!;

    public string BinType { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual ICollection<InventoryBalance> InventoryBalances { get; set; } = new List<InventoryBalance>();

    public virtual ICollection<InventoryReservation> InventoryReservations { get; set; } = new List<InventoryReservation>();

    public virtual ICollection<InventoryTransactionLine> InventoryTransactionLineFromBins { get; set; } = new List<InventoryTransactionLine>();

    public virtual ICollection<InventoryTransactionLine> InventoryTransactionLineToBins { get; set; } = new List<InventoryTransactionLine>();

    public virtual ICollection<LocationBarcode> LocationBarcodes { get; set; } = new List<LocationBarcode>();

    public virtual ICollection<ProductionMaterialIssueLine> ProductionMaterialIssueLines { get; set; } = new List<ProductionMaterialIssueLine>();

    public virtual ICollection<PutawayTask> PutawayTaskFromBins { get; set; } = new List<PutawayTask>();

    public virtual ICollection<PutawayTask> PutawayTaskToBins { get; set; } = new List<PutawayTask>();

    public virtual ICollection<StockAdjustmentLine> StockAdjustmentLines { get; set; } = new List<StockAdjustmentLine>();

    public virtual ICollection<StockCountLine> StockCountLines { get; set; } = new List<StockCountLine>();

    public virtual ICollection<StockTransferLine> StockTransferLineDestinationBins { get; set; } = new List<StockTransferLine>();

    public virtual ICollection<StockTransferLine> StockTransferLineSourceBins { get; set; } = new List<StockTransferLine>();

    public virtual WarehouseZone Zone { get; set; } = null!;
}
