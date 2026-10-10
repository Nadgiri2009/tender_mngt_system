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

        [Required, Phone]
        public string Mobile { get; set; } = string.Empty;

        [Required, MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required, Range(1, int.MaxValue)]
        public int RoleId { get; set; }

        public int? DepartmentId { get; set; }
    }
}
