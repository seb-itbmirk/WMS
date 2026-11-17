using System;
using System.Collections.Generic;

namespace WMS.API.Models;

public partial class QualityInspectionLine
{
    public long InspectionLineId { get; set; }

    public long InspectionId { get; set; }

    public long ParameterId { get; set; }

    public decimal? MeasuredValue { get; set; }

    public decimal? MinimumValue { get; set; }

    public decimal? MaximumValue { get; set; }

    public string Result { get; set; } = null!;

    public string? Remarks { get; set; }

    public virtual QualityInspection Inspection { get; set; } = null!;

    public virtual QualityParameter Parameter { get; set; } = null!;
}
