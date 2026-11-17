using System;
using System.Collections.Generic;

namespace WMS.API.Models;

public partial class QualityStatusHistory
{
    public long HistoryId { get; set; }

    public long LotId { get; set; }

    public string? PreviousStatus { get; set; }

    public string NewStatus { get; set; } = null!;

    public string? Reason { get; set; }

    public long? ChangedBy { get; set; }

    public DateTime ChangedAt { get; set; }

    public virtual AppUser? ChangedByNavigation { get; set; }

    public virtual InventoryLot Lot { get; set; } = null!;
}
