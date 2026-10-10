using TenderManagement.Domain.Common;

namespace TenderManagement.Domain.Entities
{
    public class Announcement : AuditableEntity
    {
        public int AnnouncementId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Body { get; set; }
        public bool IsPublished { get; set; }
        public DateTime? PublishDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public bool IsDeleted { get; set; }

        public User? CreatedByUser { get; set; }
    }
}
