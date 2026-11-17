using System;
using System.Collections.Generic;

namespace WMS.API.Models;

public partial class Item
{
    public long ItemId { get; set; }

    public string ItemCode { get; set; } = null!;

    public string ItemName { get; set; } = null!;

    public string? Description { get; set; }

    public long CategoryId { get; set; }

    public long BaseUomId { get; set; }

    public string StockRotationMethod { get; set; } = null!;

    public bool RequiresExpiry { get; set; }

    public bool RequiresQualityInspection { get; set; }

    public decimal ReorderLevel { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual UnitsOfMeasure BaseUom { get; set; } = null!;

    public virtual ICollection<BillOfMaterial> BillOfMaterials { get; set; } = new List<BillOfMaterial>();

    public virtual ICollection<BomLine> BomLines { get; set; } = new List<BomLine>();

    public virtual ItemCategory Category { get; set; } = null!;

    public virtual ICollection<GoodsReceiptLine> GoodsReceiptLines { get; set; } = new List<GoodsReceiptLine>();

    public virtual ICollection<InventoryLot> InventoryLots { get; set; } = new List<InventoryLot>();

    public virtual ItemBarcode? ItemBarcode { get; set; }

    public virtual ICollection<ProductionMaterialRequirement> ProductionMaterialRequirements { get; set; } = new List<ProductionMaterialRequirement>();

    public virtual ICollection<ProductionOrder> ProductionOrders { get; set; } = new List<ProductionOrder>();

    public virtual ICollection<PurchaseOrderLine> PurchaseOrderLines { get; set; } = new List<PurchaseOrderLine>();
}
