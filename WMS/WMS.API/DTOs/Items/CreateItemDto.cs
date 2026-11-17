
namespace WMS.API.DTOs.Items;

public class CreateItemDto
{
    public string ItemCode { get; set; } = null!;

    public string ItemName { get; set; } = null!;

    public string? Description { get; set; }

    public long CategoryId { get; set; }

    public long BaseUomId { get; set; }

    public string StockRotationMethod { get; set; } = "FIFO";

    public bool RequiresExpiry { get; set; }

    public bool RequiresQualityInspection { get; set; }

    public decimal ReorderLevel { get; set; }
}
