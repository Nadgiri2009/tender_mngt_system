using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using TenderManagement.Infrastructure.Data;

namespace TenderManagement.API.Authorization
{
    public sealed class PermissionAuthorizationHandler(ApplicationDbContext context)
        : AuthorizationHandler<PermissionRequirement>
    {
        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext authorizationContext,
            PermissionRequirement requirement)
        {
            var userIdValue = authorizationContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdValue, out var userId))
            {
                return;
            }

            var cancellationToken = (authorizationContext.Resource as HttpContext)?.RequestAborted
                ?? CancellationToken.None;

            var hasPermission = await (
                from user in context.Users.AsNoTracking()
                join role in context.Roles.AsNoTracking() on user.RoleId equals role.RoleId
                join rolePermission in context.RolePermissions.AsNoTracking() on role.RoleId equals rolePermission.RoleId
                join permission in context.Permissions.AsNoTracking() on rolePermission.PermissionId equals permission.PermissionId
                where user.UserId == userId
                    && user.IsActive
                    && user.AccountType == "Internal"
                    && role.IsActive
                    && permission.IsActive
                    && permission.PermissionKey == requirement.PermissionKey
                select permission.PermissionId
            ).AnyAsync(cancellationToken);

            if (hasPermission)
            {
                authorizationContext.Succeed(requirement);
            }
        }
    }
}
