using System;
using System.Collections.Generic;

namespace WMS.API.Models;

public partial class ProductionConsumption
{
    public long ConsumptionId { get; set; }

    public long ProductionOrderId { get; set; }

    public long LotId { get; set; }

    public decimal ConsumedQuantity { get; set; }

    public long UomId { get; set; }

    public DateTime ConsumptionDate { get; set; }

    public long? RecordedBy { get; set; }

    public virtual InventoryLot Lot { get; set; } = null!;

    public virtual ProductionOrder ProductionOrder { get; set; } = null!;

    public virtual AppUser? RecordedByNavigation { get; set; }

    public virtual UnitsOfMeasure Uom { get; set; } = null!;
}
