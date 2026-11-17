using System;
using System.Collections.Generic;

namespace WMS.API.Models;

public partial class GoodsReceiptLine
{
    public long ReceiptLineId { get; set; }

    public long ReceiptId { get; set; }

    public long? PoLineId { get; set; }

    public long ItemId { get; set; }

    public long LotId { get; set; }

    public decimal ReceivedQuantity { get; set; }

    public long UomId { get; set; }

    public string InspectionStatus { get; set; } = null!;

    public virtual Item Item { get; set; } = null!;

    public virtual InventoryLot Lot { get; set; } = null!;

    public virtual PurchaseOrderLine? PoLine { get; set; }

    public virtual ICollection<PutawayTask> PutawayTasks { get; set; } = new List<PutawayTask>();

    public virtual GoodsReceipt Receipt { get; set; } = null!;

    public virtual UnitsOfMeasure Uom { get; set; } = null!;
}
