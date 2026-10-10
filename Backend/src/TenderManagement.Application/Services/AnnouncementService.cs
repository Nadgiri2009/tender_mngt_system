using TenderManagement.Application.Common;
using TenderManagement.Application.DTOs.Announcement;
using TenderManagement.Application.Interfaces;
using TenderManagement.Domain.Entities;

namespace TenderManagement.Application.Services
{
    public class AnnouncementService : IAnnouncementService
    {
        private readonly IUnitOfWork _unitOfWork;

        public AnnouncementService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<List<AnnouncementDto>> GetPublishedAsync()
        {
            var now = DateTime.UtcNow;
            var items = await _unitOfWork.Announcements.FindAsync(a =>
                a.IsPublished &&
                (a.PublishDate == null || a.PublishDate <= now) &&
                (a.ExpiryDate == null || a.ExpiryDate >= now));

            return items
                .OrderByDescending(a => a.PublishDate ?? a.CreatedDate)
                .Select(MapToDto)
                .ToList();
        }

        public async Task<List<AnnouncementDto>> GetAllAsync()
        {
            var items = await _unitOfWork.Announcements.GetAllAsync();
            return items.OrderByDescending(a => a.CreatedDate).Select(MapToDto).ToList();
        }

        public async Task<AnnouncementDto> GetByIdAsync(int id)
        {
            var item = await _unitOfWork.Announcements.GetByIdAsync(id)
                ?? throw new NotFoundException(nameof(Announcement), id);
            return MapToDto(item);
        }

        public async Task<AnnouncementDto> CreateAsync(CreateAnnouncementDto dto, int createdBy)
        {
            var startDate = dto.StartDate ?? dto.PublishDate ?? (dto.IsPublished ? DateTime.UtcNow : null);
            if (startDate.HasValue && dto.ExpiryDate.HasValue && startDate > dto.ExpiryDate)
            {
                throw new BusinessRuleException("Announcement start date cannot be later than the expiry date.");
            }

            var entity = new Announcement
            {
                Title = dto.Title,
                Body = dto.Body,
                IsPublished = dto.IsPublished,
                PublishDate = startDate,
                ExpiryDate = dto.ExpiryDate,
                CreatedBy = createdBy,
            };

            await _unitOfWork.Announcements.AddAsync(entity);
            await _unitOfWork.CompleteAsync();
            return MapToDto(entity);
        }

        public async Task<AnnouncementDto> UpdateAsync(int id, UpdateAnnouncementDto dto, int modifiedBy)
        {
            var entity = await _unitOfWork.Announcements.GetByIdAsync(id)
                ?? throw new NotFoundException(nameof(Announcement), id);

            var startDate = dto.StartDate ?? dto.PublishDate ?? entity.PublishDate ?? (dto.IsPublished ? DateTime.UtcNow : null);
            if (startDate.HasValue && dto.ExpiryDate.HasValue && startDate > dto.ExpiryDate)
            {
                throw new BusinessRuleException("Announcement start date cannot be later than the expiry date.");
            }

            entity.Title = dto.Title;
            entity.Body = dto.Body;
            entity.IsPublished = dto.IsPublished;
            entity.PublishDate = startDate;
            entity.ExpiryDate = dto.ExpiryDate;
            entity.ModifiedBy = modifiedBy;
            entity.ModifiedDate = DateTime.UtcNow;

            _unitOfWork.Announcements.Update(entity);
            await _unitOfWork.CompleteAsync();
            return MapToDto(entity);
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _unitOfWork.Announcements.GetByIdAsync(id)
                ?? throw new NotFoundException(nameof(Announcement), id);

            entity.IsDeleted = true;
            _unitOfWork.Announcements.Update(entity);
            await _unitOfWork.CompleteAsync();
        }

        private static AnnouncementDto MapToDto(Announcement a) => new()
        {
            AnnouncementId = a.AnnouncementId,
            Title = a.Title,
            Body = a.Body,
            IsPublished = a.IsPublished,
            StartDate = a.PublishDate,
            PublishDate = a.PublishDate,
            ExpiryDate = a.ExpiryDate,
            CreatedByName = a.CreatedByUser?.FullName ?? string.Empty,
            CreatedDate = a.CreatedDate,
        };
    }
}
