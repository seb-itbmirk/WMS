
namespace WMS.API.DTOs.PutawayTasks
{
    public class CreatePutawayTaskDto
    {
        public long ReceiptLineId { get; set; }
        public long LotId { get; set; }
        public long? FromBinId { get; set; }
        public long ToBinId { get; set; }
        public decimal Quantity { get; set; }
        public long? AssignedTo { get; set; }
    }
}
