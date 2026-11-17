using System;
using System.Collections.Generic;

namespace WMS.API.Models;

public partial class StockAdjustment
{
    public long AdjustmentId { get; set; }

    public string AdjustmentNumber { get; set; } = null!;

    public long WarehouseId { get; set; }

    public string AdjustmentReason { get; set; } = null!;

    public string Status { get; set; } = null!;

    public long? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? PostedAt { get; set; }

    public virtual AppUser? CreatedByNavigation { get; set; }

    public virtual ICollection<StockAdjustmentLine> StockAdjustmentLines { get; set; } = new List<StockAdjustmentLine>();

    public virtual Warehouse Warehouse { get; set; } = null!;
}
