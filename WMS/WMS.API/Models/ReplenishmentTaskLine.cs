
using System;

namespace WMS.API.Models;

public partial class ReplenishmentTaskLine
{
    public long ReplenishmentTaskLineId { get; set; }

    public long ReplenishmentTaskId { get; set; }

    public long LotId { get; set; }

    public long SourceBinId { get; set; }

    public long DestinationBinId { get; set; }

    public decimal RequestedQuantity { get; set; }

    public decimal MovedQuantity { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ReplenishmentTask ReplenishmentTask { get; set; } = null!;

    public virtual InventoryLot Lot { get; set; } = null!;

    public virtual StorageBin SourceBin { get; set; } = null!;

    public virtual StorageBin DestinationBin { get; set; } = null!;
}
