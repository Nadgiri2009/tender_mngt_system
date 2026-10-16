namespace TenderManagement.Domain.Entities
{
    public class Tender
    {
        public int TenderId { get; set; }
        public string TenderNumber { get; set; } = string.Empty;
        // TrackingId is a stable external identifier used to track the tender lifecycle
        public string TrackingId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DepartmentId { get; set; }
        public decimal EstimatedAmount { get; set; }
        public string TenderType { get; set; } = string.Empty;
        public string CurrentStatus { get; set; } = Enums.TenderStatus.Draft;
        public DateTime? SubmissionStartDate { get; set; }
        public DateTime? SubmissionDeadline { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public int? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public bool IsDeleted { get; set; }

        public Department? Department { get; set; } = null!;
        public User? CreatedByUser { get; set; }
        public ICollection<Estimate> Estimates { get; set; } = new List<Estimate>();
        public ICollection<ApprovalHistory> ApprovalHistories { get; set; } = new List<ApprovalHistory>();
        public ICollection<TenderDocument> Documents { get; set; } = new List<TenderDocument>();
        public ICollection<BOQItem> BOQItems { get; set; } = new List<BOQItem>();
        public ICollection<TechnicalBid> TechnicalBids { get; set; } = new List<TechnicalBid>();
        public ICollection<FinancialBid> FinancialBids { get; set; } = new List<FinancialBid>();
    }
}
