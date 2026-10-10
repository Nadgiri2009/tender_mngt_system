using TenderManagement.Application.Common;
using TenderManagement.Application.DTOs.Common;
using TenderManagement.Application.DTOs.Tender;

namespace TenderManagement.Application.Interfaces
{
    public interface ITenderService
    {
        Task<PagedResult<TenderListItemDto>> GetTendersAsync(PaginationQuery query);
        Task<TenderDetailDto> GetTenderByIdAsync(int id);
        Task<TenderDetailDto> CreateTenderAsync(CreateTenderDto dto, int currentUserId);
        Task<TenderDetailDto> UpdateTenderAsync(int id, UpdateTenderDto dto, int currentUserId);
        Task DeleteTenderAsync(int id, int currentUserId);
        Task<BOQItemDto> AddBOQItemAsync(CreateBOQItemDto dto);
        Task<BOQItemDto> UpdateBOQItemAsync(int boqId, UpdateBOQItemDto dto);
        Task DeleteBOQItemAsync(int boqId);
        Task<List<BOQItemDto>> GetBOQItemsAsync(int tenderId);
        Task<TenderDocumentDto> UploadDocumentAsync(int tenderId, string documentType, Stream fileStream, string fileName, int uploadedBy);
        Task DeleteDocumentAsync(int documentId, int deletedBy);
        Task<List<TenderDocumentDto>> GetDocumentsAsync(int tenderId);
    }
}
