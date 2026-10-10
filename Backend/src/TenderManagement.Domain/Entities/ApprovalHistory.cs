namespace TenderManagement.Domain.Entities
{
    /// <summary>
    /// Every workflow action (submit / approve / reject) is recorded here.
    /// This is the audit trail required by the "Workflow Engine" requirement:
    /// TenderId, PreviousStatus, NewStatus, ActionTakenBy, Role, Remarks, Date.
    /// </summary>
    public class ApprovalHistory
    {
        public int ApprovalId { get; set; }
        public int TenderId { get; set; }
        public string ApprovalStage { get; set; } = string.Empty;
        public string? PreviousStatus { get; set; }
        public string NewStatus { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // Approved / Rejected / Submitted
        public int ApprovedBy { get; set; }
        public string? RoleAtAction { get; set; }
        public string? Remarks { get; set; }
        public DateTime ApprovalDate { get; set; } = DateTime.UtcNow;

        public Tender Tender { get; set; } = null!;
        public User ApprovedByUser { get; set; } = null!;
    }
}
