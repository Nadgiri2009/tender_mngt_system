using System.ComponentModel.DataAnnotations;

namespace TenderManagement.Application.DTOs.User
{
    // Used by Admin to create staff accounts (any role) -- the only way to create
    // non-Contractor accounts now that public self-registration is locked to Contractor.
    public class CreateStaffUserDto
    {
        [Required, StringLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, RegularExpression(@"^\d{10}$", ErrorMessage = "Enter a 10-digit mobile number.")]
        public string Mobile { get; set; } = string.Empty;

        [Required, MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required, Range(1, int.MaxValue)]
        public int RoleId { get; set; }

        public int? DepartmentId { get; set; }
    }

    /// <summary>Payload for updating an internal staff account.</summary>
    public sealed record UpdateStaffUserDto
    {
        [Required, StringLength(150)]
        public required string FullName { get; init; }

        [Required, EmailAddress, StringLength(150)]
        public required string Email { get; init; }

        [Required, RegularExpression(@"^\d{10}$", ErrorMessage = "Enter a 10-digit mobile number.")]
        public required string Mobile { get; init; }

        [Required, Range(1, int.MaxValue)]
        public required int RoleId { get; init; }

        public int? DepartmentId { get; init; }
    }

    /// <summary>Payload for resetting a user's password.</summary>
    public sealed record ResetStaffUserPasswordDto
    {
        [Required, MinLength(6)]
        public required string Password { get; init; }
    }
}
