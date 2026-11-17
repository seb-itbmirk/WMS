
namespace WMS.API.DTOs.ProductionMaterialIssues
{
    public class CreateProductionMaterialIssueDto
    {
        public long ProductionOrderId { get; set; }

        public long? IssuedBy { get; set; }
    }
}
