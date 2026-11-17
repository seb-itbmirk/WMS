
using System.Collections.Generic;

namespace WMS.API.DTOs.StockTransfers
{
    public class CreateStockTransferDto
    {
        public string TransferNumber { get; set; } = null!;

        public long SourceWarehouseId { get; set; }

        public long DestinationWarehouseId { get; set; }

        public long? RequestedBy { get; set; }

        public List<CreateStockTransferLineDto> Lines { get; set; } = new();
    }
}
