using System;
using System.Collections.Generic;

namespace WMS.API.Models;

public partial class GoodsReceipt
{
    public long ReceiptId { get; set; }

    public string ReceiptNumber { get; set; } = null!;

    public long? PurchaseOrderId { get; set; }

    public long WarehouseId { get; set; }

    public DateTime ReceiptDate { get; set; }

    public string? DeliveryReference { get; set; }

    public string Status { get; set; } = null!;

    public long? ReceivedBy { get; set; }

    public virtual ICollection<GoodsReceiptLine> GoodsReceiptLines { get; set; } = new List<GoodsReceiptLine>();

    public virtual PurchaseOrder? PurchaseOrder { get; set; }

    public virtual ICollection<QualityInspection> QualityInspections { get; set; } = new List<QualityInspection>();

    public virtual AppUser? ReceivedByNavigation { get; set; }

    public virtual Warehouse Warehouse { get; set; } = null!;
}
