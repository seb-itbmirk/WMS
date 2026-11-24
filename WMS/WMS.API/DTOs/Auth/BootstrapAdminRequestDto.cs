using System.ComponentModel.DataAnnotations;

namespace WMS.API.DTOs.Auth;

public class BootstrapAdminRequestDto
{
    [Required]
    public string Secret { get; set; } = null!;

    [Required]
    public string Username { get; set; } = null!;

    [Required]
    public string FullName { get; set; } = null!;

    [Required]
    public string Password { get; set; } = null!;
}