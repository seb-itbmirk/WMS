using System;
using System.Collections.Generic;

namespace WMS.API.Models;

public partial class ProductionMaterialIssueLine
{
    public long IssueLineId { get; set; }

    public long IssueId { get; set; }

    public long LotId { get; set; }

    public long BinId { get; set; }

    public decimal IssuedQuantity { get; set; }

    public long UomId { get; set; }

    public virtual StorageBin Bin { get; set; } = null!;

    public virtual ProductionMaterialIssue Issue { get; set; } = null!;

    public virtual InventoryLot Lot { get; set; } = null!;

    public virtual UnitsOfMeasure Uom { get; set; } = null!;
}
