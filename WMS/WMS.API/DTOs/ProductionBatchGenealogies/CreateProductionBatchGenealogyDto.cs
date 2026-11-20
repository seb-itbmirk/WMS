
namespace WMS.API.DTOs.ProductionBatchGenealogies
{
    public class CreateProductionBatchGenealogyDto
    {
        public long ProductionOrderId { get; set; }

        public long InputLotId { get; set; }

        public long OutputLotId { get; set; }

        public decimal ConsumedQuantity { get; set; }

        public decimal? ProducedQuantity { get; set; }

        public long UomId { get; set; }
    }
}
