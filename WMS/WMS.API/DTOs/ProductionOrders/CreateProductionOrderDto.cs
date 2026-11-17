
namespace WMS.API.DTOs.ProductionOrders
{
    public class CreateProductionOrderDto
    {
        public string ProductionNumber { get; set; } = null!;

        public long BomId { get; set; }

        public long WarehouseId { get; set; }

        public decimal PlannedQuantity { get; set; }

        public DateOnly? PlannedStartDate { get; set; }

        public DateOnly? PlannedEndDate { get; set; }

        public long? CreatedBy { get; set; }
    }
}
