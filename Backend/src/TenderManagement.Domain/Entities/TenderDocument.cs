namespace TenderManagement.Domain.Entities
{
    public class TenderDocument
    {
        public int DocumentId { get; set; }
        public int TenderId { get; set; }
        public string DocumentType { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public int UploadedBy { get; set; }
        public DateTime UploadedDate { get; set; } = DateTime.UtcNow;

        public Tender Tender { get; set; } = null!;
    }
}
