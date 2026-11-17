using System;
using System.Collections.Generic;

namespace WMS.API.Models;

public partial class ProductionOutput
{
    public long OutputId { get; set; }

    public long ProductionOrderId { get; set; }

    public long FinishedLotId { get; set; }

    public decimal ProducedQuantity { get; set; }

    public long UomId { get; set; }

    public DateTime ProductionDate { get; set; }

    public string QualityStatus { get; set; } = null!;

    public virtual InventoryLot FinishedLot { get; set; } = null!;

    public virtual ProductionOrder ProductionOrder { get; set; } = null!;

    public virtual UnitsOfMeasure Uom { get; set; } = null!;
}
