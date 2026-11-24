using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WMS.API.DTOs.Auth;
using WMS.API.Services;

namespace WMS.API.Controllers;

[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
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
    public IActionResult GetCurrentUser()
    {
        return Ok(new
        {
            UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
            Username = User.FindFirstValue(ClaimTypes.Name),
            Roles = User.FindAll(ClaimTypes.Role)
                .Select(r => r.Value)
                .ToList()
        });
    }

    // ADD THE NEW METHOD HERE, INSIDE THE CLASS
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