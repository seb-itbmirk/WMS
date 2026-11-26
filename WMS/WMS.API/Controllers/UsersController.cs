
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
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

    private static readonly string[] WarehouseScopedRoles =
    {
        "Warehouse Manager",
        "Warehouse Supervisor",
        "Warehouse Staff",
        "QC Inspector",
        "Picker / Packer",
        "Inventory Controller",
        "Auditor"
    };

    public UsersController(
        WmsDbContext context,
        PasswordService passwordService)
    {
        _context = context;
        _passwordService = passwordService;
    }

    private async Task<(AppUser? User, string? Error)> GetCurrentAdministrator()
    {
        var userIdClaim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!long.TryParse(userIdClaim, out var userId))
            return (null, "Invalid authenticated user.");

        var user = await _context.AppUsers
            .Include(u => u.Roles)
            .Include(u => u.Warehouses)
            .FirstOrDefaultAsync(u =>
                u.UserId == userId && u.IsActive);

        if (user == null)
            return (null, "Authenticated user was not found or is inactive.");

        var isSuperAdmin = user.Roles.Any(r =>
            r.RoleName.Equals(
                "Super Admin",
                StringComparison.OrdinalIgnoreCase));

        var isSystemAdmin = user.Roles.Any(r =>
            r.RoleName.Equals(
                "System Admin",
                StringComparison.OrdinalIgnoreCase));

        if (!isSuperAdmin && !isSystemAdmin)
            return (null, "Only Super Admin or System Admin can create accounts.");

        return (user, null);
    }

    private static bool IsSuperAdmin(AppUser user)
    {
        return user.Roles.Any(r =>
            r.RoleName.Equals(
                "Super Admin",
                StringComparison.OrdinalIgnoreCase));
    }

    // GET: api/users/assignable-roles
    // Returns only roles the current administrator may assign.
    [HttpGet("assignable-roles")]
    [Authorize(Policy = "Permission:users.invite")]
    public async Task<IActionResult> GetAssignableRoles()
    {
        var (administrator, error) =
            await GetCurrentAdministrator();

        if (administrator == null)
        {
            return Forbid();
        }

        var isSuperAdmin = IsSuperAdmin(administrator);

        var roles = await _context.Roles
            .AsNoTracking()
            .OrderBy(r => r.RoleName)
            .ToListAsync();

        var assignableRoles = roles
            .Where(role =>
            {
                var name = role.RoleName.Trim();

                // Integration accounts must use a separate
                // machine-to-machine provisioning process.
                if (name.Equals(
                    "Integration Service",
                    StringComparison.OrdinalIgnoreCase))
                    return false;

                // Only Super Admin can assign Super Admin.
                if (name.Equals(
                    "Super Admin",
                    StringComparison.OrdinalIgnoreCase))
                    return isSuperAdmin;

                // Only Super Admin can assign System Admin.
                if (name.Equals(
                    "System Admin",
                    StringComparison.OrdinalIgnoreCase))
                    return isSuperAdmin;

                return true;
            })
            .Select(r => new
            {
                r.RoleId,
                r.RoleName,
                r.Description
            })
            .ToList();

        return Ok(assignableRoles);
    }

    // POST: api/users
    // Only authorized higher-level administrators can create accounts.
    [HttpPost]
    [Authorize(Policy = "Permission:users.invite")]
    public async Task<IActionResult> CreateUser(
        [FromBody] CreateUserDto request)
    {
        var (administrator, error) =
            await GetCurrentAdministrator();

        if (administrator == null)
        {
            return Forbid();
        }

        var isSuperAdmin = IsSuperAdmin(administrator);

        if (string.IsNullOrWhiteSpace(request.Username) ||
            string.IsNullOrWhiteSpace(request.FullName) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new
            {
                message = "Full name, username, and password are required."
            });
        }

        if (request.Password.Length < 8)
        {
            return BadRequest(new
            {
                message = "Password must contain at least 8 characters."
            });
        }

        if (request.RoleIds == null || request.RoleIds.Count == 0)
        {
            return BadRequest(new
            {
                message = "At least one role must be assigned."
            });
        }

        var username = request.Username.Trim();

        var usernameExists = await _context.AppUsers
            .AnyAsync(u =>
                u.Username.ToLower() == username.ToLower());

        if (usernameExists)
        {
            return Conflict(new
            {
                message = "Username already exists."
            });
        }

        if (!string.IsNullOrWhiteSpace(request.EmployeeCode))
        {
            var employeeCode = request.EmployeeCode.Trim();

            var employeeCodeExists = await _context.AppUsers
                .AnyAsync(u => u.EmployeeCode == employeeCode);

            if (employeeCodeExists)
            {
                return Conflict(new
                {
                    message = "Employee code already exists."
                });
            }
        }

        var requestedRoleIds = request.RoleIds
            .Distinct()
            .ToList();

        var roles = await _context.Roles
            .Where(r => requestedRoleIds.Contains(r.RoleId))
            .ToListAsync();

        if (roles.Count != requestedRoleIds.Count)
        {
            return BadRequest(new
            {
                message = "One or more role IDs are invalid."
            });
        }

        // Never create machine-to-machine accounts through
        // the normal interactive user-management screen.
        if (roles.Any(r => r.RoleName.Equals(
            "Integration Service",
            StringComparison.OrdinalIgnoreCase)))
        {
            return BadRequest(new
            {
                message = "Integration Service accounts must be provisioned separately."
            });
        }

        // System Admin cannot grant either administrator role.
        if (!isSuperAdmin &&
            roles.Any(r =>
                r.RoleName.Equals(
                    "Super Admin",
                    StringComparison.OrdinalIgnoreCase) ||
                r.RoleName.Equals(
                    "System Admin",
                    StringComparison.OrdinalIgnoreCase)))
        {
            return Forbid();
        }

        // Do not combine global administrator roles with
        // other roles on the same account.
        var hasGlobalAdminRole = roles.Any(r =>
            r.RoleName.Equals(
                "Super Admin",
                StringComparison.OrdinalIgnoreCase) ||
            r.RoleName.Equals(
                "System Admin",
                StringComparison.OrdinalIgnoreCase));

        if (hasGlobalAdminRole && roles.Count > 1)
        {
            return BadRequest(new
            {
                message = "Global administrator accounts must have only one administrator role."
            });
        }

        var warehouseIds = (request.WarehouseIds ?? new List<long>())
            .Distinct()
            .ToList();

        var requiresWarehouse = roles.Any(r =>
            WarehouseScopedRoles.Contains(
                r.RoleName,
                StringComparer.OrdinalIgnoreCase));

        if (requiresWarehouse && warehouseIds.Count == 0)
        {
            return BadRequest(new
            {
                message = "At least one warehouse must be assigned to this role."
            });
        }

        // Validate active warehouses.
        var warehouses = await _context.Warehouses
            .Where(w =>
                warehouseIds.Contains(w.WarehouseId) &&
                w.IsActive)
            .ToListAsync();

        if (warehouses.Count != warehouseIds.Count)
        {
            return BadRequest(new
            {
                message = "One or more warehouse IDs are invalid or inactive."
            });
        }

        // Temporary System Admin scope restriction:
        // The current database does not yet have organization
        // membership tables, so assigned warehouses are used
        // as the available scope boundary.
        if (!isSuperAdmin)
        {
            var administratorWarehouseIds =
                administrator.Warehouses
                    .Select(w => w.WarehouseId)
                    .ToHashSet();

            var unauthorizedWarehouse = warehouseIds.Any(id =>
                !administratorWarehouseIds.Contains(id));

            if (unauthorizedWarehouse)
            {
                return Forbid();
            }
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            var newUser = new AppUser
            {
                EmployeeCode = request.EmployeeCode?.Trim(),
                FullName = request.FullName.Trim(),
                Username = username,
                Email = request.Email?.Trim(),
                PasswordHash = _passwordService.HashPassword(
                    request.Password),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                Roles = roles,
                Warehouses = warehouses
            };

            _context.AppUsers.Add(newUser);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return StatusCode(201, new
            {
                message = "User account created successfully.",
                userId = newUser.UserId,
                employeeCode = newUser.EmployeeCode,
                fullName = newUser.FullName,
                username = newUser.Username,
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
