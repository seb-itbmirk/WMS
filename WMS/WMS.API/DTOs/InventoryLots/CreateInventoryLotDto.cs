namespace WMS.API.DTOs.InventoryLots
{
    public class CreateInventoryLotDto
    {
        public long ItemId { get; set; }

        public string BatchCode { get; set; } = null!;

        public string? SupplierBatchCode { get; set; }

        public DateOnly? ManufactureDate { get; set; }

        public DateOnly? ExpiryDate { get; set; }

        public DateOnly ReceivedDate { get; set; }

        public string QualityStatus { get; set; } = null!;

        public string? Notes { get; set; }
    }
}
