namespace TenderManagement.Application.DTOs.Workflow
{
    public class SubmitTenderDto
    {
        public string? Remarks { get; set; }
    }

    public class ApproveRejectDto
    {
        public string ApprovalStage { get; set; } = string.Empty;
        public string? Remarks { get; set; }
    }

    public class ApprovalHistoryDto
    {
        public int ApprovalId { get; set; }
        public string ApprovalStage { get; set; } = string.Empty;
        public string? PreviousStatus { get; set; }
        public string NewStatus { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string ActionTakenBy { get; set; } = string.Empty;
        public string? RoleAtAction { get; set; }
        public string? Remarks { get; set; }
        public DateTime ApprovalDate { get; set; }
    }

    public class PendingApprovalDto
    {
        public int TenderId { get; set; }
        public string TenderNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string CurrentStatus { get; set; } = string.Empty;
        public decimal EstimatedAmount { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }

    public class WorkflowStepDto
    {
        public string Status { get; set; } = string.Empty;
        public string StageName { get; set; } = string.Empty;
        public string? AssigneeRole { get; set; }
        public string? AssigneeName { get; set; }
        public string? DepartmentName { get; set; }
        public bool IsCurrent { get; set; }
    }

    public class DashboardStatsDto
    {
        public int TotalTenders { get; set; }
        public int PublishedTenders { get; set; }
        public int PendingApprovals { get; set; }
        public int CompletedTenders { get; set; }
        public int MyTenders { get; set; }
    }
}
