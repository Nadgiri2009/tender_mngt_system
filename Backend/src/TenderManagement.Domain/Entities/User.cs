namespace TenderManagement.Domain.Entities
{
    public class User
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Mobile { get; set; }
        public string PasswordHash { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public int? DepartmentId { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public int? CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public int? ModifiedBy { get; set; }

        public Role Role { get; set; } = null!;
        public Department? Department { get; set; }
    }
}
