using Microsoft.AspNetCore.Authorization;

namespace TenderManagement.API.Authorization
{
    public sealed class PermissionRequirement(string permissionKey) : IAuthorizationRequirement
    {
        public string PermissionKey { get; } = permissionKey;
    }
}
