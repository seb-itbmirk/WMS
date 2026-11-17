using System;
using System.Collections.Generic;

namespace WMS.API.Models;

public partial class ProductionMaterialRequirement
{
    public long RequirementId { get; set; }

    public long ProductionOrderId { get; set; }

    public long ItemId { get; set; }

    public decimal RequiredQuantity { get; set; }

    public decimal ReservedQuantity { get; set; }

    public decimal IssuedQuantity { get; set; }

    public decimal ConsumedQuantity { get; set; }

    public long UomId { get; set; }

    public virtual Item Item { get; set; } = null!;

    public virtual ProductionOrder ProductionOrder { get; set; } = null!;

    public virtual UnitsOfMeasure Uom { get; set; } = null!;
}
