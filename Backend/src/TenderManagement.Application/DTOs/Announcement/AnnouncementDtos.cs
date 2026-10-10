using System.ComponentModel.DataAnnotations;

namespace TenderManagement.Application.DTOs.Announcement
{
    public class AnnouncementDto
    {
        public int AnnouncementId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Body { get; set; }
        public bool IsPublished { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? PublishDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }

    public class CreateAnnouncementDto
    {
        [Required, StringLength(250, MinimumLength = 3)]
        public string Title { get; set; } = string.Empty;

        [StringLength(4000)]
        public string? Body { get; set; }

        public bool IsPublished { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? PublishDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }

    public class UpdateAnnouncementDto : CreateAnnouncementDto
    {
    }
}
