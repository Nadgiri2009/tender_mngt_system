namespace TenderManagement.Domain.Entities
{
    public class RolePermission
    {
        public int RoleId { get; set; }
        public int PermissionId { get; set; }
        public int? AssignedBy { get; set; }
        public DateTime AssignedAtUtc { get; set; }
        public Role? Role { get; set; }
        public Permission? Permission { get; set; }
        public User? AssignedByUser { get; set; }
    }
}
