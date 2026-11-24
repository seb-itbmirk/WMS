
using System.ComponentModel.DataAnnotations;

namespace WMS.API.DTOs.Auth;

public class LoginRequestDto
{
    [Required]
    public string Username { get; set; } = null!;

    [Required]
    public string Password { get; set; } = null!;
}
