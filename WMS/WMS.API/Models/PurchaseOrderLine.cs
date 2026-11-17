using System;
using System.Collections.Generic;

namespace WMS.API.Models;

public partial class PurchaseOrderLine
{
    public long PoLineId { get; set; }

    public long PurchaseOrderId { get; set; }

    public long ItemId { get; set; }

    public decimal OrderedQuantity { get; set; }

    public long UomId { get; set; }

    public decimal? UnitCost { get; set; }

    public virtual ICollection<GoodsReceiptLine> GoodsReceiptLines { get; set; } = new List<GoodsReceiptLine>();

    public virtual Item Item { get; set; } = null!;

    public virtual PurchaseOrder PurchaseOrder { get; set; } = null!;

    public virtual UnitsOfMeasure Uom { get; set; } = null!;
}
