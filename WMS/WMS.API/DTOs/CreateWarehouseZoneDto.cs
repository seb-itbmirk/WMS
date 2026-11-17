using System.ComponentModel.DataAnnotations;

namespace WMS.API.DTOs;

public class CreateWarehouseZoneDto
{
    [Required]
    [StringLength(40)]
    public string ZoneCode { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string ZoneName { get; set; } = string.Empty;
}