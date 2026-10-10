namespace TenderManagement.Domain.Entities
{
    public class Contractor
    {
        public int ContractorId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string RegistrationNo { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? ContactNo { get; set; }
        public string? Email { get; set; }
        public int? UserId { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public User? User { get; set; }
        public ICollection<TechnicalBid> TechnicalBids { get; set; } = new List<TechnicalBid>();
        public ICollection<FinancialBid> FinancialBids { get; set; } = new List<FinancialBid>();
    }
}
