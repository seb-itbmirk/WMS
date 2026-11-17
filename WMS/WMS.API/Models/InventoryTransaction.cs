using System;
using System.Collections.Generic;

namespace WMS.API.Models;

public partial class InventoryTransaction
{
    public long TransactionId { get; set; }

    public string TransactionNumber { get; set; } = null!;

    public string TransactionType { get; set; } = null!;

    public DateTime TransactionDate { get; set; }

    public string? ReferenceNumber { get; set; }

    public string? Remarks { get; set; }

    public string Status { get; set; } = null!;

    public long? CreatedBy { get; set; }

    public long? PostedBy { get; set; }

    public DateTime? PostedAt { get; set; }

    public virtual AppUser? CreatedByNavigation { get; set; }

    public virtual ICollection<InventoryTransactionLine> InventoryTransactionLines { get; set; } = new List<InventoryTransactionLine>();

    public virtual AppUser? PostedByNavigation { get; set; }
}
