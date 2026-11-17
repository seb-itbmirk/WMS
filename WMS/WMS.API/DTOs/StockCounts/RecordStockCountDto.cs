namespace WMS.API.DTOs.StockCounts
{
    public class RecordStockCountDto
    {
        public decimal CountedQuantity { get; set; }
        public long? CountedBy { get; set; }
    }
}