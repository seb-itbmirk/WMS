namespace WMS.API.DTOs.StockTransfers;

public class DispatchStockTransferDto
{
    public long? DispatchedBy { get; set; }

    public List<DispatchStockTransferLineDto> Lines { get; set; } = new();
}

public class DispatchStockTransferLineDto
{
    public long TransferLineId { get; set; }

    public decimal DispatchedQuantity { get; set; }
}