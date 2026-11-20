
namespace WMS.API.DTOs.GoodsReceipts
{
    public class CreateGoodsReceiptLineDto
    {
        public long ItemId { get; set; }

        public long LotId { get; set; }

        public decimal ReceivedQuantity { get; set; }

        public long? ReceivingBinId { get; set; }

        public long UomId { get; set; }

        public string InspectionStatus { get; set; } = "PENDING";

        public long? PoLineId { get; set; }
    }
}
