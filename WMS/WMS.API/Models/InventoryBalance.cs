using System;
using System.Collections.Generic;

namespace WMS.API.Models;

public partial class InventoryBalance
{
    public long BalanceId { get; set; }

    public long LotId { get; set; }

    public long BinId { get; set; }

    public string StockStatus { get; set; } = null!;

    public decimal QuantityOnHand { get; set; }

    public decimal QuantityReserved { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual StorageBin Bin { get; set; } = null!;

    public virtual InventoryLot Lot { get; set; } = null!;
}
