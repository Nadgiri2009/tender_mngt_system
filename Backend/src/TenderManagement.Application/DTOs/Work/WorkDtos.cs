using System.ComponentModel.DataAnnotations;

namespace TenderManagement.Application.DTOs.Work
{
    public class CreateAgreementDto
    {
        [Required, Range(1, int.MaxValue)]
        public int TenderId { get; set; }

        [Required, Range(1, int.MaxValue)]
        public int ContractorId { get; set; }

        [Required, StringLength(50)]
        public string AgreementNo { get; set; } = string.Empty;

        [Required]
        public DateTime AgreementDate { get; set; }

        public string? DocumentPath { get; set; }
    }

    public class CreateWorkOrderDto
    {
        [Required, Range(1, int.MaxValue)]
        public int TenderId { get; set; }

        [Required, Range(1, int.MaxValue)]
        public int ContractorId { get; set; }

        [Required, StringLength(50)]
        public string OrderNo { get; set; } = string.Empty;

        [Required]
        public DateTime IssueDate { get; set; }

        [Required, Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }
    }

    public class UpdateWorkProgressDto
    {
        [Required, Range(1, int.MaxValue)]
        public int TenderId { get; set; }

        [Required, Range(0, 100, ErrorMessage = "Progress percentage must be between 0 and 100.")]
        public decimal ProgressPercentage { get; set; }

        [StringLength(1000)]
        public string? Remarks { get; set; }
    }

    public class CreateMeasurementBookDto
    {
        [Required, Range(1, int.MaxValue)]
        public int TenderId { get; set; }

        [Required, StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required, Range(0.001, double.MaxValue, ErrorMessage = "Quantity must be greater than zero.")]
        public decimal Quantity { get; set; }
    }

    public class CreateBillDto
    {
        [Required, Range(1, int.MaxValue)]
        public int TenderId { get; set; }

        [Required, Range(1, int.MaxValue)]
        public int ContractorId { get; set; }

        [Required, StringLength(50)]
        public string BillNo { get; set; } = string.Empty;

        [Required, Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }
    }

    public class BillDto
    {
        public int BillId { get; set; }
        public int TenderId { get; set; }
        public string ContractorName { get; set; } = string.Empty;
        public string BillNo { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime SubmittedDate { get; set; }
    }

    public class ProcessPaymentDto
    {
        [Required, Range(1, int.MaxValue)]
        public int BillId { get; set; }

        [Required, Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        [Required, StringLength(100)]
        public string TransactionNo { get; set; } = string.Empty;
    }
}
