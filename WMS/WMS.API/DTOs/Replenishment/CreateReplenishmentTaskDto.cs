using System.ComponentModel.DataAnnotations;

namespace WMS.API.DTOs.Replenishment;

public class CreateReplenishmentTaskDto
{
    [Required]
    [MaxLength(50)]
    public string TaskNumber { get; set; } = null!;

    [Range(1, long.MaxValue)]
    public long WarehouseId { get; set; }

    [Range(1, long.MaxValue)]
    public long? AssignedTo { get; set; }

    public string? Notes { get; set; }
}