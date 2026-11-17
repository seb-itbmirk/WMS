using System;
using System.Collections.Generic;

namespace WMS.API.Models;

public partial class StockCountLine
{
    public long StockCountLineId { get; set; }

    public long StockCountId { get; set; }

    public long LotId { get; set; }

    public long BinId { get; set; }

    public decimal SystemQuantity { get; set; }

    public decimal? CountedQuantity { get; set; }

    public long? CountedBy { get; set; }

    public DateTime? CountedAt { get; set; }

    public virtual StorageBin Bin { get; set; } = null!;

    public virtual AppUser? CountedByNavigation { get; set; }

    public virtual InventoryLot Lot { get; set; } = null!;

    public virtual StockCount StockCount { get; set; } = null!;
}
