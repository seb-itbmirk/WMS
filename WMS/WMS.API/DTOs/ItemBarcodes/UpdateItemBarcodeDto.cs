
namespace WMS.API.DTOs.ItemBarcodes
{
    public class UpdateItemBarcodeDto
    {
        public string BarcodeValue { get; set; } = null!;

        public string BarcodeType { get; set; } = "CODE128";

        public bool IsPrimary { get; set; }
    }
}
