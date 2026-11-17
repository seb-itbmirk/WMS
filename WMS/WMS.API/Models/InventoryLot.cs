using System;
using System.Collections.Generic;

namespace WMS.API.Models;

public partial class InventoryLot
{
    public long LotId { get; set; }

    public long ItemId { get; set; }

    public string BatchCode { get; set; } = null!;

    public string? SupplierBatchCode { get; set; }

    public DateOnly? ManufactureDate { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    public DateOnly ReceivedDate { get; set; }

    public string QualityStatus { get; set; } = null!;

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<GoodsReceiptLine> GoodsReceiptLines { get; set; } = new List<GoodsReceiptLine>();

    public virtual ICollection<InventoryBalance> InventoryBalances { get; set; } = new List<InventoryBalance>();

    public virtual ICollection<InventoryReservation> InventoryReservations { get; set; } = new List<InventoryReservation>();

    public virtual ICollection<InventoryTransactionLine> InventoryTransactionLines { get; set; } = new List<InventoryTransactionLine>();

    public virtual Item Item { get; set; } = null!;

    public virtual ICollection<LotBarcode> LotBarcodes { get; set; } = new List<LotBarcode>();

    public virtual ICollection<ProductionBatchGenealogy> ProductionBatchGenealogyInputLots { get; set; } = new List<ProductionBatchGenealogy>();

    public virtual ICollection<ProductionBatchGenealogy> ProductionBatchGenealogyOutputLots { get; set; } = new List<ProductionBatchGenealogy>();

    public virtual ICollection<ProductionConsumption> ProductionConsumptions { get; set; } = new List<ProductionConsumption>();

    public virtual ICollection<ProductionMaterialIssueLine> ProductionMaterialIssueLines { get; set; } = new List<ProductionMaterialIssueLine>();

    public virtual ICollection<ProductionOutput> ProductionOutputs { get; set; } = new List<ProductionOutput>();

    public virtual ICollection<ProductionWaste> ProductionWastes { get; set; } = new List<ProductionWaste>();

    public virtual ICollection<PutawayTask> PutawayTasks { get; set; } = new List<PutawayTask>();

    public virtual ICollection<QualityInspection> QualityInspections { get; set; } = new List<QualityInspection>();

    public virtual ICollection<QualityStatusHistory> QualityStatusHistories { get; set; } = new List<QualityStatusHistory>();

    public virtual ICollection<StockAdjustmentLine> StockAdjustmentLines { get; set; } = new List<StockAdjustmentLine>();

    public virtual ICollection<StockCountLine> StockCountLines { get; set; } = new List<StockCountLine>();

    public virtual ICollection<StockTransferLine> StockTransferLines { get; set; } = new List<StockTransferLine>();
}
