namespace WMS.API.DTOs.QualityInspections
{
    public class FinalizeQualityInspectionDto
    {
        public string LotQualityStatus { get; set; } = null!;
        public long? InspectedBy { get; set; }
        public string? Remarks { get; set; }
        public string? Reason { get; set; }
    }
}