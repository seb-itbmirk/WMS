namespace WMS.API.DTOs.StockAdjustments
{
    public class AddStockAdjustmentLineDto
    {
        public long LotId { get; set; }

        public long BinId { get; set; }

        public decimal CountedQuantity { get; set; }
    }
}
