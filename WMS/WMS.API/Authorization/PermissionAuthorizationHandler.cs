using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WMS.API.Data;

namespace WMS.API.Authorization;

public class PermissionAuthorizationHandler
    : AuthorizationHandler<PermissionRequirement>
{
    private readonly WmsDbContext _context;

    public PermissionAuthorizationHandler(WmsDbContext context)
    {
        _context = context;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var userIdClaim = context.User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        Console.WriteLine("=== PERMISSION AUTHORIZATION DEBUG ===");
        Console.WriteLine($"IsAuthenticated: {context.User.Identity?.IsAuthenticated}");
        Console.WriteLine($"UserId Claim: {userIdClaim}");
        Console.WriteLine($"Permission Required: {requirement.PermissionCode}");

        if (!long.TryParse(userIdClaim, out var userId))
            return;

        var hasPermission = await _context.Database
            .SqlQuery<bool>($"""
                SELECT EXISTS (
                    SELECT 1
                    FROM user_roles ur
                    INNER JOIN role_permissions rp
                        ON rp.role_id = ur.role_id
                    INNER JOIN permissions p
                        ON p.permission_id = rp.permission_id
                    WHERE ur.user_id = {userId}
                        AND p.permission_code = {requirement.PermissionCode}
                        AND EXISTS (
                            SELECT 1
                            FROM app_users au
                            WHERE au.user_id = ur.user_id
                              AND au.is_active = TRUE
                        )
                ) AS "Value"
                """)
            .SingleAsync();

        if (hasPermission)
        {
            Console.WriteLine($"Permission Granted: {hasPermission}");
            context.Succeed(requirement);
        }
        if (!hasPermission)
        {
            Console.WriteLine("Permission DENIED");
        }

    }
}