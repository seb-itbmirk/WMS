using System;
using System.Collections.Generic;

namespace WMS.API.Models;

public partial class BomLine
{
    public long BomLineId { get; set; }

    public long BomId { get; set; }

    public long ComponentItemId { get; set; }

    public decimal RequiredQuantity { get; set; }

    public long UomId { get; set; }

    public decimal WastagePercentage { get; set; }

    public virtual BillOfMaterial Bom { get; set; } = null!;

    public virtual Item ComponentItem { get; set; } = null!;

    public virtual UnitsOfMeasure Uom { get; set; } = null!;
}
