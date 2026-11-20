
namespace WMS.API.DTOs.PurchaseOrders
{
    public class UpdatePurchaseOrderLineDto
    {
        public long ItemId { get; set; }

        public decimal OrderedQuantity { get; set; }

        public long UomId { get; set; }

        public decimal? UnitCost { get; set; }
    }
}
