namespace TenderManagement.Domain.Entities
{
    public class TenderDocumentVerificationHistory
    {
        public long DocumentVerificationHistoryId { get; set; }
        public int TenderId { get; set; }
        public int ContractorId { get; set; }
        public int? TechnicalBidDocumentId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string DocumentType { get; set; } = string.Empty;
        public string ReviewStage { get; set; } = "Technical";
        public string VerificationStatus { get; set; } = string.Empty;
        public int ReviewerId { get; set; }
        public int ReviewerRoleId { get; set; }
        public string? Remarks { get; set; }
        public DateTime ReviewedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
