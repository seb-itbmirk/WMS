using System;
using System.Collections.Generic;

namespace WMS.API.Models;

public partial class StockTransferLine
{
    public long TransferLineId { get; set; }

    public long TransferId { get; set; }

    public long LotId { get; set; }

    public long SourceBinId { get; set; }

    public long? DestinationBinId { get; set; }

    public decimal RequestedQuantity { get; set; }

    public decimal DispatchedQuantity { get; set; }

    public decimal ReceivedQuantity { get; set; }

    public long UomId { get; set; }

    public string? DiscrepancyReason { get; set; }

    public virtual StorageBin? DestinationBin { get; set; }

    public virtual InventoryLot Lot { get; set; } = null!;

    public virtual StorageBin SourceBin { get; set; } = null!;

    public virtual StockTransfer Transfer { get; set; } = null!;

    public virtual UnitsOfMeasure Uom { get; set; } = null!;
}
