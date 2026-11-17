using System;
using System.Collections.Generic;

namespace WMS.API.Models;

public partial class UnitsOfMeasure
{
    public long UomId { get; set; }

    public string UomCode { get; set; } = null!;

    public string UomName { get; set; } = null!;

    public string UnitType { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual ICollection<BillOfMaterial> BillOfMaterials { get; set; } = new List<BillOfMaterial>();

    public virtual ICollection<BomLine> BomLines { get; set; } = new List<BomLine>();

    public virtual ICollection<GoodsReceiptLine> GoodsReceiptLines { get; set; } = new List<GoodsReceiptLine>();

    public virtual ICollection<InventoryTransactionLine> InventoryTransactionLines { get; set; } = new List<InventoryTransactionLine>();

    public virtual ICollection<Item> Items { get; set; } = new List<Item>();

    public virtual ICollection<ProductionBatchGenealogy> ProductionBatchGenealogies { get; set; } = new List<ProductionBatchGenealogy>();

    public virtual ICollection<ProductionConsumption> ProductionConsumptions { get; set; } = new List<ProductionConsumption>();

    public virtual ICollection<ProductionMaterialIssueLine> ProductionMaterialIssueLines { get; set; } = new List<ProductionMaterialIssueLine>();

    public virtual ICollection<ProductionMaterialRequirement> ProductionMaterialRequirements { get; set; } = new List<ProductionMaterialRequirement>();

    public virtual ICollection<ProductionOrder> ProductionOrders { get; set; } = new List<ProductionOrder>();

    public virtual ICollection<ProductionOutput> ProductionOutputs { get; set; } = new List<ProductionOutput>();

    public virtual ICollection<ProductionWaste> ProductionWastes { get; set; } = new List<ProductionWaste>();

    public virtual ICollection<PurchaseOrderLine> PurchaseOrderLines { get; set; } = new List<PurchaseOrderLine>();

    public virtual ICollection<StockTransferLine> StockTransferLines { get; set; } = new List<StockTransferLine>();

    public virtual ICollection<UomConversion> UomConversionFromUoms { get; set; } = new List<UomConversion>();

    public virtual ICollection<UomConversion> UomConversionToUoms { get; set; } = new List<UomConversion>();
}
