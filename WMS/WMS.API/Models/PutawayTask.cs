using System;
using System.Collections.Generic;

namespace WMS.API.Models;

public partial class PutawayTask
{
    public long PutawayId { get; set; }

    public long ReceiptLineId { get; set; }

    public long LotId { get; set; }

    public long? FromBinId { get; set; }

    public long ToBinId { get; set; }

    public decimal Quantity { get; set; }

    public string Status { get; set; } = null!;

    public long? AssignedTo { get; set; }

    public DateTime? CompletedAt { get; set; }

    public virtual AppUser? AssignedToNavigation { get; set; }

    public virtual StorageBin? FromBin { get; set; }

    public virtual InventoryLot Lot { get; set; } = null!;

    public virtual GoodsReceiptLine ReceiptLine { get; set; } = null!;

    public virtual StorageBin ToBin { get; set; } = null!;
}
