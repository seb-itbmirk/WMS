
using System;
using System.Collections.Generic;

namespace WMS.API.Models;

public partial class ReplenishmentTask
{
    public long ReplenishmentTaskId { get; set; }

    public string TaskNumber { get; set; } = null!;

    public long WarehouseId { get; set; }

    public string Status { get; set; } = "DRAFT";

    public long? CreatedBy { get; set; }

    public long? AssignedTo { get; set; }

    public long? CompletedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public string? Notes { get; set; }

    public virtual Warehouse Warehouse { get; set; } = null!;

    public virtual AppUser? CreatedByNavigation { get; set; }

    public virtual AppUser? AssignedToNavigation { get; set; }

    public virtual AppUser? CompletedByNavigation { get; set; }

    public virtual ICollection<ReplenishmentTaskLine> ReplenishmentTaskLines
    {
        get;
        set;
    } = new List<ReplenishmentTaskLine>();
}
