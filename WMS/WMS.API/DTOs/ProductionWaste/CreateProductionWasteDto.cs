namespace WMS.API.DTOs.ProductionWaste
{
    public class CreateProductionWasteDto
    {
        public long ProductionOrderId { get; set; }

        public long LotId { get; set; }

        public decimal WastedQuantity { get; set; }

        public long UomId { get; set; }

        public string WasteReason { get; set; } = null!;

        public DateTime? RecordedAt { get; set; }

        public long? RecordedBy { get; set; }
    }
}
