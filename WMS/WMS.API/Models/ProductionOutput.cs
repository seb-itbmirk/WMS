
namespace WMS.API.Models;

public partial class ProductionOutput
{
    public long OutputId { get; set; }

    public long ProductionOrderId { get; set; }

    public long FinishedLotId { get; set; }

    // Accepted / good quantity
    public decimal ProducedQuantity { get; set; }

    // Rejected quantity
    public decimal RejectedQuantity { get; set; }

    public long UomId { get; set; }

    public DateTime ProductionDate { get; set; }

    public string QualityStatus { get; set; } = null!;

    public bool IsPosted { get; set; }

    public string? Remarks { get; set; }

    public virtual InventoryLot FinishedLot { get; set; } = null!;

    public virtual ProductionOrder ProductionOrder { get; set; } = null!;

    public virtual UnitsOfMeasure Uom { get; set; } = null!;
}
