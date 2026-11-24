using System.ComponentModel.DataAnnotations;

namespace WMS.API.DTOs.Users;

public class CreateUserDto
{
    [MaxLength(40)]
    public string? EmployeeCode { get; set; }

    [Required]
    [MaxLength(150)]
    public string FullName { get; set; } = null!;

    [Required]
    [MaxLength(80)]
    public string Username { get; set; } = null!;

    [EmailAddress]
    [MaxLength(150)]
    public string? Email { get; set; }

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = null!;

    [Required]
    [MinLength(1)]
    public List<long> RoleIds { get; set; } = new();

    public List<long> WarehouseIds { get; set; } = new();
}