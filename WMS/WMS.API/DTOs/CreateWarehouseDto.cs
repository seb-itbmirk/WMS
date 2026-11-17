using System.ComponentModel.DataAnnotations;

namespace WMS.API.DTOs;

public class CreateWarehouseDto
{
    [Required]
    [StringLength(30)]
    public string WarehouseCode { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    public string WarehouseName { get; set; } = string.Empty;

    public string? City { get; set; }

    public string? Address { get; set; }
}

public class UpdateWarehouseDto
{
    [Required]
    [StringLength(150)]
    public string WarehouseName { get; set; } = string.Empty;

    public string? City { get; set; }

    public string? Address { get; set; }
}
