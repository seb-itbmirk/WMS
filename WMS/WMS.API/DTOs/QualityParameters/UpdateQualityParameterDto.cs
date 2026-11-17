
namespace WMS.API.DTOs.QualityParameters
{
    public class UpdateQualityParameterDto
    {
        public string ParameterCode { get; set; } = null!;
        public string ParameterName { get; set; } = null!;
        public string? Description { get; set; }
        public string? UnitOfMeasure { get; set; }
    }
}
