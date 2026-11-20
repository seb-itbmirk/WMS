
namespace WMS.API.DTOs.PurchaseOrders
{
    public class UpdatePurchaseOrderDto
    {
        public string PoNumber { get; set; } = string.Empty;

        public long SupplierId { get; set; }

        public DateOnly OrderDate { get; set; }

        public DateOnly? ExpectedDeliveryDate { get; set; }
    }
}
