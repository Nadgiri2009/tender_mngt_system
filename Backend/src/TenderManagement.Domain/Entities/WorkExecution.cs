namespace TenderManagement.Domain.Entities
{
    public class WorkExecution
    {
        public int ExecutionId { get; set; }
        public int TenderId { get; set; }
        public decimal ProgressPercentage { get; set; }
        public string? Remarks { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

        public Tender Tender { get; set; } = null!;
    }
}
