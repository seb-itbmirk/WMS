
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using WMS.API.Data;
using WMS.API.DTOs.Auth;

namespace WMS.API.Services;

public class AuthService
{
    private readonly WmsDbContext _context;
    private readonly PasswordService _passwordService;
    private readonly IConfiguration _configuration;

    public AuthService(
        WmsDbContext context,
        PasswordService passwordService,
        IConfiguration configuration)
    {
        _context = context;
        _passwordService = passwordService;
        _configuration = configuration;
    }

    public async Task<LoginResponseDto?> LoginAsync(
        LoginRequestDto request)
    {
        var username = request.Username.Trim();

        var user = await _context.AppUsers
            .AsNoTracking()
            .Include(u => u.Roles)
            .Include(u => u.Warehouses)
            .FirstOrDefaultAsync(u => u.Username == username);

        // Reject missing or inactive accounts.
        if (user == null || !user.IsActive)
            return null;

        // Verify the submitted password.
        var isPasswordValid = _passwordService.VerifyPassword(
            request.Password,
            user.PasswordHash);

        if (!isPasswordValid)
            return null;

        // Get the user's assigned roles.
        var roles = user.Roles
            .Select(r => r.RoleName)
            .ToList();

        // Get the user's assigned warehouses.
        var warehouseIds = user.Warehouses
            .Select(w => w.WarehouseId)
            .ToList();

        // Get all permissions granted through the user's roles.
        var permissions = await _context.Database
            .SqlQuery<string>($"""
                SELECT DISTINCT p.permission_code AS "Value"
                FROM user_roles ur
                INNER JOIN role_permissions rp
                    ON rp.role_id = ur.role_id
                INNER JOIN permissions p
                    ON p.permission_id = rp.permission_id
                WHERE ur.user_id = {user.UserId}
                ORDER BY "Value"
                """)
            .ToListAsync();

        // Generate authentication token.
        var accessToken = GenerateJwtToken(user, roles);

        return new LoginResponseDto
        {
            AccessToken = accessToken,
            TokenType = "Bearer",
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(60),
            UserId = user.UserId,
            Username = user.Username,
            FullName = user.FullName,
            Roles = roles,
            WarehouseIds = warehouseIds,
            Permissions = permissions
        };
    }

    private string GenerateJwtToken(
        Models.AppUser user,
        List<string> roles)
    {
        var jwtKey = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException(
                "JWT Key is missing.");

        var jwtIssuer = _configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException(
                "JWT Issuer is missing.");

        var jwtAudience = _configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException(
                "JWT Audience is missing.");

        var keyBytes = Convert.FromBase64String(jwtKey);

        var signingKey = new SymmetricSecurityKey(keyBytes);

        var credentials = new SigningCredentials(
            signingKey,
            SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(
                ClaimTypes.NameIdentifier,
                user.UserId.ToString()),

            new(
                ClaimTypes.Name,
                user.Username),

            new(
                JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString())
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(60);

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
