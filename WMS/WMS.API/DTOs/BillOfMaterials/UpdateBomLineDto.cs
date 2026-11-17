
namespace WMS.API.DTOs.BillOfMaterials
{
    public class UpdateBomLineDto
    {
        public long ComponentItemId { get; set; }
        public decimal RequiredQuantity { get; set; }
        public long UomId { get; set; }
        public decimal WastagePercentage { get; set; }
    }
}
