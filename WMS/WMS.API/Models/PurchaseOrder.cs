using System;
using System.Collections.Generic;

namespace WMS.API.Models;

public partial class PurchaseOrder
{
    public long PurchaseOrderId { get; set; }

    public string PoNumber { get; set; } = null!;

    public long SupplierId { get; set; }

    public DateOnly OrderDate { get; set; }

    public DateOnly? ExpectedDeliveryDate { get; set; }

    public string Status { get; set; } = null!;

    public long? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual AppUser? CreatedByNavigation { get; set; }

    public virtual ICollection<GoodsReceipt> GoodsReceipts { get; set; } = new List<GoodsReceipt>();

    public virtual ICollection<PurchaseOrderLine> PurchaseOrderLines { get; set; } = new List<PurchaseOrderLine>();

    public virtual Supplier Supplier { get; set; } = null!;
}
