using System.ComponentModel.DataAnnotations;

namespace WMS.API.DTOs.Replenishment;

public class AddReplenishmentTaskLineDto
{
    [Range(1, long.MaxValue)]
    public long LotId { get; set; }

    [Range(1, long.MaxValue)]
    public long SourceBinId { get; set; }

    [Range(1, long.MaxValue)]
    public long DestinationBinId { get; set; }

    [Range(typeof(decimal), "0.0001", "999999999999")]
    public decimal RequestedQuantity { get; set; }
}