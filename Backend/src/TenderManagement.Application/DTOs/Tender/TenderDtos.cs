using System.ComponentModel.DataAnnotations;

namespace TenderManagement.Application.DTOs.Tender
{
    public class CreateTenderDto
    {
        [Required, StringLength(250, MinimumLength = 5)]
        public string Title { get; set; } = string.Empty;

        [StringLength(4000)]
        public string? Description { get; set; }

        [Required, Range(1, int.MaxValue, ErrorMessage = "A valid Department must be selected.")]
        public int? DepartmentId { get; set; }

        [Required, Range(0.01, double.MaxValue, ErrorMessage = "Estimated amount must be greater than zero.")]
        public decimal EstimatedAmount { get; set; }

        [Required, StringLength(50)]
        public string TenderType { get; set; } = string.Empty;

        public DateTime? SubmissionDeadline { get; set; }
    }

    public class UpdateTenderDto
    {
        [Required, StringLength(250, MinimumLength = 5)]
        public string Title { get; set; } = string.Empty;

        [StringLength(4000)]
        public string? Description { get; set; }

        [Required, Range(0.01, double.MaxValue, ErrorMessage = "Estimated amount must be greater than zero.")]
        public decimal EstimatedAmount { get; set; }

        [Required, StringLength(50)]
        public string TenderType { get; set; } = string.Empty;

        public DateTime? SubmissionDeadline { get; set; }
    }

    public class TenderListItemDto
    {
        public int TenderId { get; set; }
        public string TenderNumber { get; set; } = string.Empty;
        public string TrackingId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public decimal EstimatedAmount { get; set; }
        public string CurrentStatus { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }

    public class TenderDetailDto : TenderListItemDto
    {
        public string? Description { get; set; }
        public string TenderType { get; set; } = string.Empty;
        public DateTime? SubmissionDeadline { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public List<BOQItemDto> BOQItems { get; set; } = new();
        public List<TenderDocumentDto> Documents { get; set; } = new();
    }

    public class BOQItemDto
    {
        public int BOQId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal Rate { get; set; }
        public decimal Amount { get; set; }
    }

    public class CreateBOQItemDto
    {
        [Required, Range(1, int.MaxValue)]
        public int TenderId { get; set; }

        [Required, StringLength(200)]
        public string ItemName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required, Range(0.001, double.MaxValue, ErrorMessage = "Quantity must be greater than zero.")]
        public decimal Quantity { get; set; }

        [Required, StringLength(20)]
        public string Unit { get; set; } = string.Empty;

        [Required, Range(0.01, double.MaxValue, ErrorMessage = "Rate must be greater than zero.")]
        public decimal Rate { get; set; }
    }

    public class UpdateBOQItemDto
    {
        [Required, Range(1, int.MaxValue)]
        public int TenderId { get; set; }

        [Required, StringLength(200)]
        public string ItemName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required, Range(0.001, double.MaxValue, ErrorMessage = "Quantity must be greater than zero.")]
        public decimal Quantity { get; set; }

        [Required, StringLength(20)]
        public string Unit { get; set; } = string.Empty;

        [Required, Range(0.01, double.MaxValue, ErrorMessage = "Rate must be greater than zero.")]
        public decimal Rate { get; set; }
    }

    public class TenderDocumentDto
    {
        public int DocumentId { get; set; }
        public string DocumentType { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public DateTime UploadedDate { get; set; }
    }
}
