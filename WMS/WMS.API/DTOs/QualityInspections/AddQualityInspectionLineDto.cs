namespace WMS.API.DTOs.QualityInspections
{
    public class AddQualityInspectionLineDto
    {
        public long ParameterId { get; set; }
        public decimal? MeasuredValue { get; set; }
        public decimal? MinimumValue { get; set; }
        public decimal? MaximumValue { get; set; }
        public string? Remarks { get; set; }
    }
}