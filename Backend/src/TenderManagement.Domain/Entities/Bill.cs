namespace TenderManagement.Domain.Entities
{
    public class Bill
    {
        public int BillId { get; set; }
        public int TenderId { get; set; }
        public int ContractorId { get; set; }
        public string BillNo { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = "Submitted";
        public DateTime SubmittedDate { get; set; } = DateTime.UtcNow;
        public int? ApprovedBy { get; set; }
        public DateTime? ApprovedDate { get; set; }

        public Tender Tender { get; set; } = null!;
        public Contractor Contractor { get; set; } = null!;
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
