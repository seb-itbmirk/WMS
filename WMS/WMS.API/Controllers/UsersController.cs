using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMS.API.Data;
using WMS.API.DTOs.Users;
using WMS.API.Models;
using WMS.API.Services;

namespace WMS.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly WmsDbContext _context;
    private readonly PasswordService _passwordService;

    public UsersController(
        WmsDbContext context,
        PasswordService passwordService)
    {
        _context = context;
        _passwordService = passwordService;
    }

    // POST: api/users
    [HttpPost]
    [Authorize(Policy = "Permission:users.create")]
    public async Task<IActionResult> CreateUser(
        [FromBody] CreateUserDto request)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            // Check duplicate username
            var usernameExists = await _context.AppUsers
                .AnyAsync(u =>
                    u.Username.ToLower() ==
                    request.Username.Trim().ToLower());

            if (usernameExists)
            {
                return Conflict(new
                {
                    message = "Username already exists."
                });
            }

            // Check duplicate employee code
            if (!string.IsNullOrWhiteSpace(request.EmployeeCode))
            {
                var employeeCodeExists = await _context.AppUsers
                    .AnyAsync(u =>
                        u.EmployeeCode == request.EmployeeCode.Trim());

                if (employeeCodeExists)
                {
                    return Conflict(new
                    {
                        message = "Employee code already exists."
                    });
                }
            }

            // Validate roles
            var roles = await _context.Roles
                .Where(r => request.RoleIds.Contains(r.RoleId))
                .ToListAsync();

            if (roles.Count != request.RoleIds.Distinct().Count())
            {
                return BadRequest(new
                {
                    message = "One or more role IDs are invalid."
                });
            }

            // Validate warehouses
            var warehouseIds = request.WarehouseIds
                .Distinct()
                .ToList();

            var warehouses = await _context.Warehouses
                .Where(w =>
                    warehouseIds.Contains(w.WarehouseId) &&
                    w.IsActive)
                .ToListAsync();

            if (warehouses.Count != warehouseIds.Count)
            {
                return BadRequest(new
                {
                    message =
                        "One or more warehouse IDs are invalid or inactive."
                });
            }

            // Create user
            var user = new AppUser
            {
                EmployeeCode = request.EmployeeCode?.Trim(),
                FullName = request.FullName.Trim(),
                Username = request.Username.Trim(),
                Email = request.Email?.Trim(),
                PasswordHash = _passwordService.HashPassword(
                    request.Password),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,

                Roles = roles,
                Warehouses = warehouses
            };

            _context.AppUsers.Add(user);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return StatusCode(201, new
            {
                message = "User created successfully.",
                userId = user.UserId,
                employeeCode = user.EmployeeCode,
                fullName = user.FullName,
                username = user.Username,
                roles = roles.Select(r => r.RoleName),
                warehouseIds = warehouses.Select(w => w.WarehouseId)
            });
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // GET: api/users/{id}
    [HttpGet("{id:long}")]
    [Authorize(Policy = "Permission:users.view")]
    public async Task<IActionResult> GetUser(long id)
    {
        var user = await _context.AppUsers
            .AsNoTracking()
            .Include(u => u.Roles)
            .Include(u => u.Warehouses)
            .Where(u => u.UserId == id)
            .Select(u => new
            {
                u.UserId,
                u.EmployeeCode,
                u.FullName,
                u.Username,
                u.Email,
                u.IsActive,
                u.CreatedAt,

                Roles = u.Roles.Select(r => new
                {
                    r.RoleId,
                    r.RoleName
                }),

                Warehouses = u.Warehouses.Select(w => new
                {
                    w.WarehouseId,
                    w.WarehouseCode,
                    w.WarehouseName
                })
            })
            .FirstOrDefaultAsync();

        if (user == null)
        {
            return NotFound(new
            {
                message = "User not found."
            });
        }

        return Ok(user);
    }
}