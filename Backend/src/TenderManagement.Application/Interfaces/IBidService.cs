using TenderManagement.Application.DTOs.Bid;

namespace TenderManagement.Application.Interfaces
{
    public interface IBidService
    {
        Task<TechnicalBidDto> SubmitTechnicalBidAsync(SubmitTechnicalBidDto dto);
        Task<TechnicalBidDto> EvaluateTechnicalBidAsync(EvaluateTechnicalBidDto dto, int currentUserId);
        Task<List<TechnicalBidDto>> GetTechnicalBidsAsync(int tenderId);

        Task<FinancialBidDto> SubmitFinancialBidAsync(SubmitFinancialBidDto dto);
        Task<List<FinancialBidDto>> GetFinancialBidsRankedAsync(int tenderId);
        Task<FinancialBidDto> SelectContractorAsync(SelectContractorDto dto, int currentUserId);
    }
}
