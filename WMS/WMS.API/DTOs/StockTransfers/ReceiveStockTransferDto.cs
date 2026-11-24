
using System.Collections.Generic;

namespace WMS.API.DTOs.StockTransfers
{
    public class ReceiveStockTransferDto
    {
        public List<ReceiveStockTransferLineDto> Lines { get; set; } = new();
    }

    public class ReceiveStockTransferLineDto
    {
        public long TransferLineId { get; set; }

        public long DestinationBinId { get; set; }

        public decimal ReceivedQuantity { get; set; }

        public string? DiscrepancyReason { get; set; }
    }
}
