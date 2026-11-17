using System;
using System.Collections.Generic;

namespace WMS.API.Models;

public partial class BillOfMaterial
{
    public long BomId { get; set; }

    public string BomCode { get; set; } = null!;

    public long FinishedItemId { get; set; }

    public int VersionNumber { get; set; }

    public decimal OutputQuantity { get; set; }

    public long OutputUomId { get; set; }

    public DateOnly? EffectiveFrom { get; set; }

    public DateOnly? EffectiveTo { get; set; }

    public string Status { get; set; } = null!;

    public virtual ICollection<BomLine> BomLines { get; set; } = new List<BomLine>();

    public virtual Item FinishedItem { get; set; } = null!;

    public virtual UnitsOfMeasure OutputUom { get; set; } = null!;

    public virtual ICollection<ProductionOrder> ProductionOrders { get; set; } = new List<ProductionOrder>();
}
