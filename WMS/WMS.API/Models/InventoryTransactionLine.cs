using System;
using System.Collections.Generic;

namespace WMS.API.Models;

public partial class InventoryTransactionLine
{
    public long TransactionLineId { get; set; }

    public long TransactionId { get; set; }

    public long LotId { get; set; }

    public long? FromBinId { get; set; }

    public long? ToBinId { get; set; }

    public decimal Quantity { get; set; }

    public long UomId { get; set; }

    public string? LineRemarks { get; set; }

    public virtual StorageBin? FromBin { get; set; }

    public virtual InventoryLot Lot { get; set; } = null!;

    public virtual StorageBin? ToBin { get; set; }

    public virtual InventoryTransaction Transaction { get; set; } = null!;

    public virtual UnitsOfMeasure Uom { get; set; } = null!;
}
