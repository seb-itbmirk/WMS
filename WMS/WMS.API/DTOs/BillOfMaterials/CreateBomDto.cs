
namespace WMS.API.DTOs.BillOfMaterials
{
    public class CreateBomDto
    {
        public string BomCode { get; set; } = null!;
        public long FinishedItemId { get; set; }
        public int VersionNumber { get; set; }
        public decimal OutputQuantity { get; set; }
        public long OutputUomId { get; set; }
        public DateOnly? EffectiveFrom { get; set; }
        public DateOnly? EffectiveTo { get; set; }
    }
}
