using System;
using System.Collections.Generic;

namespace WMS.API.Models;

public partial class StockTransfer
{
    public long TransferId { get; set; }

    public string TransferNumber { get; set; } = null!;

    public long SourceWarehouseId { get; set; }

    public long DestinationWarehouseId { get; set; }

    public DateTime TransferDate { get; set; }

    public string Status { get; set; } = null!;

    public long? RequestedBy { get; set; }

    public long? DispatchedBy { get; set; }

    public long? ReceivedBy { get; set; }

    public DateTime? DispatchedAt { get; set; }

    public DateTime? ReceivedAt { get; set; }

    public virtual Warehouse DestinationWarehouse { get; set; } = null!;

    public virtual AppUser? DispatchedByNavigation { get; set; }

    public virtual AppUser? ReceivedByNavigation { get; set; }

    public virtual AppUser? RequestedByNavigation { get; set; }

    public virtual Warehouse SourceWarehouse { get; set; } = null!;

    public virtual ICollection<StockTransferLine> StockTransferLines { get; set; } = new List<StockTransferLine>();
}
