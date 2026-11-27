
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WMS.API.Data;
using WMS.API.DTOs.Auth;
using WMS.API.Services;

namespace WMS.API.Controllers;

[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly WmsDbContext _context;

    public AuthController(
        AuthService authService,
        WmsDbContext context)
    {
        _authService = authService;
        _context = context;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequestDto request)
    {
        var result = await _authService.LoginAsync(request);

        if (result == null)
        {
            return Unauthorized(new
            {
                message = "Invalid username or password."
            });
        }

        return Ok(result);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userIdClaim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!long.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new
            {
                message = "Invalid user identity."
            });
        }

        var user = await _context.AppUsers
            .AsNoTracking()
            .Include(u => u.Roles)
            .Include(u => u.Warehouses)
            .FirstOrDefaultAsync(u =>
                u.UserId == userId && u.IsActive);

        if (user == null)
        {
            return Unauthorized(new
            {
                message = "User account not found or inactive."
            });
        }

        var roles = user.Roles
            .Select(r => r.RoleName)
            .ToList();

        var warehouseIds = user.Warehouses
            .Select(w => w.WarehouseId)
            .ToList();

        var permissions = await _context.Database
            .SqlQuery<string>($"""
                SELECT DISTINCT p.permission_code AS "Value"
                FROM user_roles ur
                INNER JOIN role_permissions rp
                    ON rp.role_id = ur.role_id
                INNER JOIN permissions p
                    ON p.permission_id = rp.permission_id
                WHERE ur.user_id = {userId}
                ORDER BY "Value"
                """)
            .ToListAsync();

        return Ok(new
        {
            UserId = user.UserId,
            Username = user.Username,
            FullName = user.FullName,
            Roles = roles,
            WarehouseIds = warehouseIds,
            Permissions = permissions
        });
    }

    [Authorize(Policy = "Permission:dashboard.view")]
    [HttpGet("dashboard-test")]
    public IActionResult TestDashboardPermission()
    {
        return Ok(new
        {
            message = "Dashboard permission granted."
        });
    }
}
