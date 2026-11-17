
namespace WMS.API.DTOs.BillOfMaterials
{
    public class AddBomLineDto
    {
        public long ComponentItemId { get; set; }
        public decimal RequiredQuantity { get; set; }
        public long UomId { get; set; }
        public decimal WastagePercentage { get; set; }
    }
}
