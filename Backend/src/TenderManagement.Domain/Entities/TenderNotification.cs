namespace TenderManagement.Domain.Entities
{
    public class TenderNotification
    {
        public long NotificationId { get; set; }
        public int UserId { get; set; }
        public int TenderId { get; set; }
        public long? ShortfallId { get; set; }
        public string NotificationType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? ReadAtUtc { get; set; }
    }
}
