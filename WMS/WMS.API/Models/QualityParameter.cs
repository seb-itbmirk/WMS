using System;
using System.Collections.Generic;

namespace WMS.API.Models;

public partial class QualityParameter
{
    public long ParameterId { get; set; }

    public string ParameterCode { get; set; } = null!;

    public string ParameterName { get; set; } = null!;

    public string? Description { get; set; }

    public string? UnitOfMeasure { get; set; }

    public virtual ICollection<QualityInspectionLine> QualityInspectionLines { get; set; } = new List<QualityInspectionLine>();
}
