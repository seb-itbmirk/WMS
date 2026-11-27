
namespace WMS.API.DTOs.Auth;

public class LoginResponseDto
{
    public string AccessToken { get; set; } = null!;

    public string TokenType { get; set; } = "Bearer";

    public DateTime ExpiresAtUtc { get; set; }

    public long UserId { get; set; }

    public string Username { get; set; } = null!;

    public string FullName { get; set; } = null!;

    // Roles assigned to the user.
    public List<string> Roles { get; set; } = new();

    // Warehouses the user is assigned to.
    public List<long> WarehouseIds { get; set; } = new();

    // Effective permissions granted through the user's roles.
    public List<string> Permissions { get; set; } = new();
}
