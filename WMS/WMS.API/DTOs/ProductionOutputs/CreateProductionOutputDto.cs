
namespace WMS.API.DTOs.ProductionOutputs
{
    public class CreateProductionOutputDto
    {
        public long ProductionOrderId { get; set; }

        public string BatchCode { get; set; } = null!;

        public decimal ProducedQuantity { get; set; }

        public long UomId { get; set; }

        public long DestinationBinId { get; set; }

        public DateOnly? ManufactureDate { get; set; }

        public DateOnly? ExpiryDate { get; set; }

        public DateTime? ProductionDate { get; set; }

        public long? RecordedBy { get; set; }
    }
}
