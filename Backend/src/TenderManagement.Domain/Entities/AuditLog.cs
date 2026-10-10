namespace TenderManagement.Domain.Entities
{
    public class AuditLog
    {
        public long LogId { get; set; }
        public int? UserId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public int? EntityId { get; set; }
        public string? Details { get; set; }
        public string? IPAddress { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
