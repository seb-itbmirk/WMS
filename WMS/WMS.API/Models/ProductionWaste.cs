using System;
using System.Collections.Generic;

namespace WMS.API.Models;

public partial class ProductionWaste
{
    public long WasteId { get; set; }

    public long ProductionOrderId { get; set; }

    public long LotId { get; set; }

    public decimal WastedQuantity { get; set; }

    public long UomId { get; set; }

    public string WasteReason { get; set; } = null!;

    public DateTime RecordedAt { get; set; }

    public long? RecordedBy { get; set; }

    public virtual InventoryLot Lot { get; set; } = null!;

    public virtual ProductionOrder ProductionOrder { get; set; } = null!;

    public virtual AppUser? RecordedByNavigation { get; set; }

    public virtual UnitsOfMeasure Uom { get; set; } = null!;
}
