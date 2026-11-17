
namespace WMS.API.DTOs.StockAdjustments
{
    public class CreateStockAdjustmentDto
    {
        public string AdjustmentNumber { get; set; } = null!;

        public long WarehouseId { get; set; }

        public string AdjustmentReason { get; set; } = null!;

        public long? CreatedBy { get; set; }
    }
}
