namespace TenderManagement.Domain.Entities
{
    public class Payment
    {
        public int PaymentId { get; set; }
        public int BillId { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
        public string TransactionNo { get; set; } = string.Empty;
        public int? ProcessedBy { get; set; }

        public Bill Bill { get; set; } = null!;
    }
}
