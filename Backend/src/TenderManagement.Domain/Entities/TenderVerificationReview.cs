namespace TenderManagement.Domain.Entities
{
    public class TenderVerificationReview
    {
        public long VerificationReviewId { get; set; }
        public int TenderId { get; set; }
        public int ContractorId { get; set; }
        public string ReviewStage { get; set; } = "Technical";
        public string Decision { get; set; } = string.Empty;
        public int ReviewerId { get; set; }
        public int ReviewerRoleId { get; set; }
        public string? Remarks { get; set; }
        public DateTime ReviewedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
