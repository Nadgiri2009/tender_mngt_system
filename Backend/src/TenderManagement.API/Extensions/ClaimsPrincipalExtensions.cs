using System.Security.Claims;

namespace TenderManagement.API.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static int GetUserId(this ClaimsPrincipal user)
        {
            var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var id) ? id : 0;
        }

        public static string GetRoleName(this ClaimsPrincipal user) =>
            user.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

        public static int? GetDepartmentId(this ClaimsPrincipal user)
        {
            var value = user.FindFirstValue("departmentId");
            return int.TryParse(value, out var id) ? id : null;
        }
    }
}
