
namespace WMS.API.DTOs.PurchaseOrders
{
    public class CreatePurchaseOrderDto
    {
        public string PoNumber { get; set; } = string.Empty;

        public long SupplierId { get; set; }

        public DateOnly OrderDate { get; set; }

        public DateOnly? ExpectedDeliveryDate { get; set; }

        public List<CreatePurchaseOrderLineDto> Lines { get; set; } = new();
    }

    public class CreatePurchaseOrderLineDto
    {
        public long ItemId { get; set; }

        public decimal OrderedQuantity { get; set; }

        public long UomId { get; set; }

        public decimal? UnitCost { get; set; }
    }
}
