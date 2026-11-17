namespace WMS.API.DTOs.StockCounts
{
    public class CreateStockCountDto
    {
        public string CountNumber { get; set; } = null!;
        public long WarehouseId { get; set; }
        public DateTime CountDate { get; set; }
        public long? CreatedBy { get; set; }
    }
}