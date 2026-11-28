
namespace WMS.API.DTOs.ProductionReports;

public class CreateProductionReportDto
{
    public long ProductionOrderId { get; set; }

    public string BatchCode { get; set; } = null!;

    public decimal ProducedQuantity { get; set; }

    public decimal RejectedQuantity { get; set; }

    public DateTime ProductionDate { get; set; }

    public string? Remarks { get; set; }
}
