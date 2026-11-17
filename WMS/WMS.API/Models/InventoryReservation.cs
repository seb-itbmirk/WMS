using System;
using System.Collections.Generic;

namespace WMS.API.Models;

public partial class InventoryReservation
{
    public long ReservationId { get; set; }

    public long LotId { get; set; }

    public long BinId { get; set; }

    public string ReferenceType { get; set; } = null!;

    public long ReferenceId { get; set; }

    public decimal ReservedQuantity { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public long? CreatedBy { get; set; }

    public virtual StorageBin Bin { get; set; } = null!;

    public virtual AppUser? CreatedByNavigation { get; set; }

    public virtual InventoryLot Lot { get; set; } = null!;
}
