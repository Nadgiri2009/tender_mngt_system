namespace TenderManagement.Domain.Entities
{
    public class TenderWorkflowAssignment
    {
        public long AssignmentId { get; set; }
        public int TenderId { get; set; }
        public long? ShortfallId { get; set; }
        public int StageId { get; set; }
        public int AssignedRoleId { get; set; }
        public int? AssignedUserId { get; set; }
        public int DepartmentId { get; set; }
        public DateTime AssignedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
        public bool IsCurrent { get; set; }

        public Tender Tender { get; set; } = null!;
        public WorkflowStage Stage { get; set; } = null!;
        public Role AssignedRole { get; set; } = null!;
        public User? AssignedUser { get; set; }
        public Department Department { get; set; } = null!;
    }
}
