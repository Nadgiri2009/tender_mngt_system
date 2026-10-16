using System.ComponentModel.DataAnnotations;

namespace TenderManagement.Application.DTOs.Bid
{
    public class SubmitTechnicalBidDto
    {
        [Required, Range(1, int.MaxValue)]
        public int TenderId { get; set; }

        [Required, Range(1, int.MaxValue)]
        public int ContractorId { get; set; }

        public string? DocumentPath { get; set; }
    }

    public class EvaluateTechnicalBidDto
    {
        [Required, Range(1, int.MaxValue)]
        public int TechnicalBidId { get; set; }

        [Required, RegularExpression("Qualified|Disqualified", ErrorMessage = "Status must be 'Qualified' or 'Disqualified'.")]
        public string Status { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Remarks { get; set; }
    }

    public class TechnicalBidDto
    {
        public int TechnicalBidId { get; set; }
        public int TenderId { get; set; }
        public int ContractorId { get; set; }
        public string ContractorName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Remarks { get; set; }
        public DateTime SubmittedDate { get; set; }
    }

    public class TenderApplicationDto
    {
        public int TechnicalBidId { get; set; }
        public int TenderId { get; set; }
        public string ApplicationReference { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime SubmittedDate { get; set; }
        public List<string> Documents { get; set; } = new();
    }

    public class SubmitFinancialBidDto
    {
        [Required, Range(1, int.MaxValue)]
        public int TenderId { get; set; }

        [Required, Range(1, int.MaxValue)]
        public int ContractorId { get; set; }

        [Required, Range(0.01, double.MaxValue, ErrorMessage = "Bid amount must be greater than zero.")]
        public decimal BidAmount { get; set; }
    }

    public class FinancialBidDto
    {
        public int FinancialBidId { get; set; }
        public int TenderId { get; set; }
        public int ContractorId { get; set; }
        public string ContractorName { get; set; } = string.Empty;
        public decimal BidAmount { get; set; }
        public int? Rank { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class SelectContractorDto
    {
        [Required, Range(1, int.MaxValue)]
        public int TenderId { get; set; }

        [Required, Range(1, int.MaxValue)]
        public int ContractorId { get; set; }

        [StringLength(1000)]
        public string? Remarks { get; set; }
    }
}
