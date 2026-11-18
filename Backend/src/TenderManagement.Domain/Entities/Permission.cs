namespace TenderManagement.Domain.Entities
{
    public class Permission
    {
        public int PermissionId { get; set; }
        public string PermissionKey { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}
