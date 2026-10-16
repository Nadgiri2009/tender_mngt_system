namespace TenderManagement.Domain.Entities
{
    public class TechnicalBidDocument
    {
        public int TechnicalBidDocumentId { get; set; }
        public int TechnicalBidId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public DateTime UploadedDate { get; set; } = DateTime.UtcNow;

        public TechnicalBid TechnicalBid { get; set; } = null!;
    }
}
