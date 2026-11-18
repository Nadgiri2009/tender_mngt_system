using System.ComponentModel.DataAnnotations;

namespace TenderManagement.Application.DTOs.Role
{
    /// <summary>Permission keys requested for one of the four fixed internal roles.</summary>
    public sealed record UpdateRolePermissionsRequest
    {
        [Required]
        public required IReadOnlyList<string> PermissionKeys { get; init; }
    }
}
