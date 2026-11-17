
namespace WMS.API.DTOs.ProductionConsumptions
{
    public class CreateProductionConsumptionDto
    {
        public long ProductionOrderId { get; set; }

        public long LotId { get; set; }

        public decimal ConsumedQuantity { get; set; }

        public long UomId { get; set; }

        public DateTime? ConsumptionDate { get; set; }

        public long? RecordedBy { get; set; }
    }
}
