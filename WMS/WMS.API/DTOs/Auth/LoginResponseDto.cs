
namespace WMS.API.DTOs.Auth;

public class LoginResponseDto
{
    public string AccessToken { get; set; } = null!;

    public string TokenType { get; set; } = "Bearer";

    public DateTime ExpiresAtUtc { get; set; }

    public long UserId { get; set; }

    public string Username { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public List<string> Roles { get; set; } = new();

    public List<long> WarehouseIds { get; set; } = new();
}
