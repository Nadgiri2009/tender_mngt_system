namespace TenderManagement.Domain.Entities
{
    public class WorkflowTransition
    {
        public int TransitionId { get; set; }
        public string TransitionCode { get; set; } = string.Empty;
        public short WorkflowVersion { get; set; }
        public int StageId { get; set; }
        public string FromStatus { get; set; } = string.Empty;
        public string ActionName { get; set; } = string.Empty;
        public string ToStatus { get; set; } = string.Empty;
        public string ActionStatus { get; set; } = "Completed";
        public int RoleId { get; set; }
        public string PermissionKey { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        public WorkflowStage Stage { get; set; } = null!;
        public Role Role { get; set; } = null!;
    }
}
