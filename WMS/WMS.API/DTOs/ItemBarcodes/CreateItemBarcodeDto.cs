
namespace WMS.API.DTOs.ItemBarcodes
{
    public class CreateItemBarcodeDto
    {
        public long ItemId { get; set; }

        public string BarcodeValue { get; set; } = null!;

        public string BarcodeType { get; set; } = "CODE128";

        public bool IsPrimary { get; set; }
    }
}
