using TenderManagement.Application.DTOs.Bid;

namespace TenderManagement.Application.Interfaces
{
    public interface IBidService
    {
        Task<TechnicalBidDto> SubmitTechnicalBidAsync(SubmitTechnicalBidDto dto, int currentUserId, bool isAdmin);
        Task<TenderApplicationDto> SubmitApplicationAsync(
            int tenderId,
            int userId,
            string termsVersion,
            IReadOnlyList<(string FileName, Stream Content)> files);
        Task<TechnicalBidDto> EvaluateTechnicalBidAsync(EvaluateTechnicalBidDto dto, int currentUserId);
        Task<List<TechnicalBidDto>> GetTechnicalBidsAsync(int tenderId, int currentUserId, bool tenderCell);

        Task<FinancialBidDto> SubmitFinancialBidAsync(SubmitFinancialBidDto dto, int currentUserId);
        Task<List<FinancialBidDto>> SaveJeFinancialDataAsync(SaveJeFinancialDataDto dto, int currentUserId);
        Task<List<FinancialBidDto>> GetFinancialBidsRankedAsync(int tenderId);
        Task<FinancialBidDto> SelectContractorAsync(SelectContractorDto dto, int currentUserId);
    }
}
