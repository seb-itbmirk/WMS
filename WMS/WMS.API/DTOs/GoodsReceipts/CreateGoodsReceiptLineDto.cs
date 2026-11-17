
namespace WMS.API.DTOs.GoodsReceipts
{
    public class CreateGoodsReceiptDto
    {
        public string ReceiptNumber { get; set; } = null!;

        public long? PurchaseOrderId { get; set; }

        public long WarehouseId { get; set; }

        public DateTime? ReceiptDate { get; set; }

        public string? DeliveryReference { get; set; }

        public long? ReceivedBy { get; set; }

        public List<CreateGoodsReceiptLineDto> Lines { get; set; } = new();
    }
}
