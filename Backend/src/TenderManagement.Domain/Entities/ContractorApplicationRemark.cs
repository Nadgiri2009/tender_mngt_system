namespace TenderManagement.Domain.Entities
{
    public class ContractorApplicationRemark
    {
        public long RemarkId { get; set; }
        public int TechnicalBidId { get; set; }
        public string RemarkType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int CreatedBy { get; set; }
        public int? RoleId { get; set; }
        public bool VisibleToContractor { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
