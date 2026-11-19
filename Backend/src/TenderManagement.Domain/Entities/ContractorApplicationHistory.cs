namespace TenderManagement.Domain.Entities
{
    public class ContractorApplicationHistory
    {
        public long HistoryId { get; set; }
        public int TechnicalBidId { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string? PreviousStatus { get; set; }
        public string? NewStatus { get; set; }
        public string? Remarks { get; set; }
        public int CreatedBy { get; set; }
        public int? RoleId { get; set; }
        public bool VisibleToContractor { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public TechnicalBid? TechnicalBid { get; set; }
    }
}
