namespace TenderManagement.Domain.Entities
{
    public class TechnicalBid
    {
        public int TechnicalBidId { get; set; }
        public int TenderId { get; set; }
        public int ContractorId { get; set; }
        public string? DocumentPath { get; set; }
        public string Status { get; set; } = "Submitted";
        public string? Remarks { get; set; }
        public int? EvaluatedBy { get; set; }
        public DateTime SubmittedDate { get; set; } = DateTime.UtcNow;
        public DateTime? EvaluatedDate { get; set; }

        public Tender Tender { get; set; } = null!;
        public Contractor Contractor { get; set; } = null!;
    }
}
