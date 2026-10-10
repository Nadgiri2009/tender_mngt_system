namespace TenderManagement.Domain.Entities
{
    /// <summary>Bill of Quantities line item. Maps to table BOQ.</summary>
    public class BOQItem
    {
        public int BOQId { get; set; }
        public int TenderId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal Rate { get; set; }
        public decimal Amount { get; private set; } // DB computed column, read-only in EF
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public Tender Tender { get; set; } = null!;
    }
}
