using System;
using System.Collections.Generic;

namespace WMS.API.Models;

public partial class UomConversion
{
    public long ConversionId { get; set; }

    public long FromUomId { get; set; }

    public long ToUomId { get; set; }

    public decimal ConversionFactor { get; set; }

    public virtual UnitsOfMeasure FromUom { get; set; } = null!;

    public virtual UnitsOfMeasure ToUom { get; set; } = null!;
}
