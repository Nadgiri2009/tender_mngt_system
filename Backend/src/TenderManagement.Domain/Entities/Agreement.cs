namespace TenderManagement.Domain.Entities
{
    public class Agreement
    {
        public int AgreementId { get; set; }
        public int TenderId { get; set; }
        public int ContractorId { get; set; }
        public string AgreementNo { get; set; } = string.Empty;
        public DateTime AgreementDate { get; set; }
        public string? DocumentPath { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public Tender Tender { get; set; } = null!;
        public Contractor Contractor { get; set; } = null!;
    }
}
