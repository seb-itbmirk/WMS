namespace WMS.API.DTOs.QualityInspections
{
    public class CreateQualityInspectionDto
    {
        public long LotId { get; set; }
        public long? ReceiptId { get; set; }
        public long? ProductionOrderId { get; set; }
        public string InspectionType { get; set; } = null!;
        public DateTime? InspectionDate { get; set; }
        public long? InspectedBy { get; set; }
        public string? Remarks { get; set; }
    }
}