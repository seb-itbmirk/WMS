using System;
using System.Collections.Generic;

namespace WMS.API.Models;

public partial class QualityInspection
{
    public long InspectionId { get; set; }

    public long LotId { get; set; }

    public long? ReceiptId { get; set; }

    public long? ProductionOrderId { get; set; }

    public string InspectionType { get; set; } = null!;

    public DateTime InspectionDate { get; set; }

    public string OverallResult { get; set; } = null!;

    public long? InspectedBy { get; set; }

    public string? Remarks { get; set; }

    public virtual AppUser? InspectedByNavigation { get; set; }

    public virtual InventoryLot Lot { get; set; } = null!;

    public virtual ProductionOrder? ProductionOrder { get; set; }

    public virtual ICollection<QualityInspectionLine> QualityInspectionLines { get; set; } = new List<QualityInspectionLine>();

    public virtual GoodsReceipt? Receipt { get; set; }
}
