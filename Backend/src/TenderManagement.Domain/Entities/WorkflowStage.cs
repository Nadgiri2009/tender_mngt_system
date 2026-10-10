namespace TenderManagement.Domain.Entities
{
    /// <summary>
    /// Admin-configurable workflow: which role approves a tender sitting at
    /// StatusOnPending, and what status it moves to on approve/reject.
    /// </summary>
    public class WorkflowStage
    {
        public int StageId { get; set; }
        public int StageOrder { get; set; }
        public string StageName { get; set; } = string.Empty;
        public string StatusOnPending { get; set; } = string.Empty;
        public string StatusOnApprove { get; set; } = string.Empty;
        public string StatusOnReject { get; set; } = "Rejected";
        public int ApproverRoleId { get; set; }
        public bool IsActive { get; set; } = true;

        public Role ApproverRole { get; set; } = null!;
    }
}
