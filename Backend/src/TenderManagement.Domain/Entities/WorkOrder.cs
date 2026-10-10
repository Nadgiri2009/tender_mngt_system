namespace TenderManagement.Domain.Entities
{
    public class WorkOrder
    {
        public int WorkOrderId { get; set; }
        public int TenderId { get; set; }
        public int ContractorId { get; set; }
        public string OrderNo { get; set; } = string.Empty;
        public DateTime IssueDate { get; set; }
        public decimal Amount { get; set; }
        public DateTime? CompletionDate { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public Tender Tender { get; set; } = null!;
        public Contractor Contractor { get; set; } = null!;
    }
}
