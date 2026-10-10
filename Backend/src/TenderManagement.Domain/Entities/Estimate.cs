namespace TenderManagement.Domain.Entities
{
    public class Estimate
    {
        public int EstimateId { get; set; }
        public int TenderId { get; set; }
        public decimal Amount { get; set; }
        public int PreparedBy { get; set; }
        public string? DocumentPath { get; set; }
        public string Status { get; set; } = "Prepared";
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedDate { get; set; }

        public Tender Tender { get; set; } = null!;
    }
}
