namespace TenderManagement.Domain.Entities
{
    public class FinancialBid
    {
        public int FinancialBidId { get; set; }
        public int TenderId { get; set; }
        public int ContractorId { get; set; }
        public decimal BidAmount { get; set; }
        public int? Rank { get; set; }
        public string Status { get; set; } = "Submitted";
        public DateTime SubmittedDate { get; set; } = DateTime.UtcNow;

        public Tender Tender { get; set; } = null!;
        public Contractor Contractor { get; set; } = null!;
    }
}
