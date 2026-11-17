using System.ComponentModel.DataAnnotations;

namespace WMS.API.DTOs;

public class UpdateWarehouseZoneDto
{
    [Required]
    [StringLength(100)]
    public string ZoneName { get; set; } = string.Empty;
}