
namespace WMS.API.DTOs.StockTransfers
{
    public class CreateStockTransferLineDto
    {
        public long LotId { get; set; }

        public long SourceBinId { get; set; }

        public decimal RequestedQuantity { get; set; }

        public long UomId { get; set; }
    }
}
