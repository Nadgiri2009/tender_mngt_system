using TenderManagement.Application.DTOs.Announcement;

namespace TenderManagement.Application.Interfaces
{
    public interface IAnnouncementService
    {
        Task<List<AnnouncementDto>> GetPublishedAsync();
        Task<List<AnnouncementDto>> GetAllAsync();
        Task<AnnouncementDto> GetByIdAsync(int id);
        Task<AnnouncementDto> CreateAsync(CreateAnnouncementDto dto, int createdBy);
        Task<AnnouncementDto> UpdateAsync(int id, UpdateAnnouncementDto dto, int modifiedBy);
        Task DeleteAsync(int id);
    }
}
