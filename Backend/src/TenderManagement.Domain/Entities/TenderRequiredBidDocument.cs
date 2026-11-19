namespace TenderManagement.Domain.Entities
{
    public class TenderRequiredBidDocument
    {
        public int TenderId { get; set; }
        public string DocumentType { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsMandatory { get; set; } = true;
        public string? AllowedExtensions { get; set; }
        public int MaxFileSizeBytes { get; set; } = 10_000_000;
        public int CreatedBy { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
