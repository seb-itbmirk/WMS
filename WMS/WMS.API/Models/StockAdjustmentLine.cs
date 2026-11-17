using System;
using System.Collections.Generic;

namespace WMS.API.Models;

public partial class StockAdjustmentLine
{
    public long AdjustmentLineId { get; set; }

    public long AdjustmentId { get; set; }

    public long LotId { get; set; }

    public long BinId { get; set; }

    public decimal SystemQuantity { get; set; }

    public decimal CountedQuantity { get; set; }

    public decimal DifferenceQuantity { get; set; }

    public virtual StockAdjustment Adjustment { get; set; } = null!;

    public virtual StorageBin Bin { get; set; } = null!;

    public virtual InventoryLot Lot { get; set; } = null!;
}
