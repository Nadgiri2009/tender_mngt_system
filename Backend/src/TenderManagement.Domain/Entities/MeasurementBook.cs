namespace TenderManagement.Domain.Entities
{
    public class MeasurementBook
    {
        public int MBId { get; set; }
        public int TenderId { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public DateTime MeasurementDate { get; set; } = DateTime.UtcNow;
        public int? RecordedBy { get; set; }
        public int? ApprovedBy { get; set; }
        public string ApprovalStatus { get; set; } = "Pending";

        public Tender Tender { get; set; } = null!;
    }
}
