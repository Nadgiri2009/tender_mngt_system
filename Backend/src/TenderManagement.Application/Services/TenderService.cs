using TenderManagement.Application.Common;
using TenderManagement.Application.DTOs.Common;
using TenderManagement.Application.DTOs.Tender;
using TenderManagement.Application.Interfaces;
using TenderManagement.Domain.Entities;
using TenderManagement.Domain.Enums;

namespace TenderManagement.Application.Services
{
    public class TenderService : ITenderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileStorageService _fileStorageService;
        private readonly IAuditService _auditService;

        public TenderService(IUnitOfWork unitOfWork, IFileStorageService fileStorageService, IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _fileStorageService = fileStorageService;
            _auditService = auditService;
        }

        public async Task<PagedResult<TenderListItemDto>> GetTendersAsync(PaginationQuery query, int? createdByUserId = null)
        {
            var (items, total) = await _unitOfWork.Tenders.GetPagedAsync(query, createdByUserId);

            var dtos = items.Select(MapToListItemDto);

            return new PagedResult<TenderListItemDto>
            {
                Items = dtos,
                TotalCount = total,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize
            };
        }

        public async Task<TenderDetailDto> GetTenderByIdAsync(
            int id,
            int? createdByUserId = null,
            bool allowPublishedForOtherOwners = false)
        {
            var tender = await _unitOfWork.Tenders.GetWithDetailsAsync(id)
                ?? throw new NotFoundException(nameof(Tender), id);

            if (createdByUserId.HasValue &&
                tender.CreatedBy != createdByUserId.Value &&
                !(allowPublishedForOtherOwners && tender.CurrentStatus == TenderStatus.TenderPublished))
            {
                throw new NotFoundException(nameof(Tender), id);
            }

            return MapToDetailDto(tender);
        }

        public async Task<TenderDetailDto> CreateTenderAsync(CreateTenderDto dto, int currentUserId)
        {
            if (dto.DepartmentId is null)
            {
                throw new BusinessRuleException("A valid Department must be selected.");
            }

            if (dto.SubmissionStartDate.HasValue && !dto.SubmissionDeadline.HasValue)
            {
                throw new BusinessRuleException("A submission deadline is required when an application start date is set.");
            }

            if (dto.SubmissionStartDate.HasValue && dto.SubmissionDeadline.HasValue &&
                dto.SubmissionDeadline.Value <= dto.SubmissionStartDate.Value)
            {
                throw new BusinessRuleException("Submission deadline must be after the application start date.");
            }

            var user = await _unitOfWork.Users.GetByIdAsync(currentUserId)
                ?? throw new NotFoundException(nameof(User), currentUserId);
            var role = await _unitOfWork.Roles.GetByIdAsync(user.RoleId);
            if (string.Equals(role?.RoleName, RoleNames.DepartmentOfficer, StringComparison.OrdinalIgnoreCase) &&
                (!user.DepartmentId.HasValue || dto.DepartmentId.Value != user.DepartmentId.Value))
            {
                throw new BusinessRuleException("Department officers may create tenders only for their assigned department.");
            }

            var department = await _unitOfWork.Departments.GetByIdAsync(dto.DepartmentId.Value)
                ?? throw new NotFoundException(nameof(Department), dto.DepartmentId.Value);

            var tender = new Tender
            {
                TenderNumber = await _unitOfWork.Tenders.GenerateNextTenderNumberAsync(),
                TrackingId = Guid.NewGuid().ToString(),
                Title = dto.Title,
                Description = dto.Description,
                DepartmentId = dto.DepartmentId.Value,
                EstimatedAmount = dto.EstimatedAmount,
                TenderType = dto.TenderType,
                SubmissionStartDate = dto.SubmissionStartDate,
                SubmissionDeadline = dto.SubmissionDeadline,
                CurrentStatus = TenderStatus.Draft,
                CreatedBy = currentUserId,
                CreatedDate = DateTime.UtcNow
            };

            await _unitOfWork.Tenders.AddAsync(tender);
            await _unitOfWork.CompleteAsync();

            await _auditService.LogAsync(currentUserId, "Create", nameof(Tender), tender.TenderId);

            var saved = await _unitOfWork.Tenders.GetWithDetailsAsync(tender.TenderId);
            return MapToDetailDto(saved!);
        }

        public async Task<TenderDetailDto> UpdateTenderAsync(int id, UpdateTenderDto dto, int currentUserId)
        {
            var tender = await _unitOfWork.Tenders.GetByIdAsync(id)
                ?? throw new NotFoundException(nameof(Tender), id);

            if (tender.CurrentStatus != TenderStatus.Draft)
            {
                throw new BusinessRuleException("Only tenders in Draft status can be edited.");
            }

            tender.Title = dto.Title;
            tender.Description = dto.Description;
            tender.EstimatedAmount = dto.EstimatedAmount;
            tender.TenderType = dto.TenderType;
            tender.SubmissionDeadline = dto.SubmissionDeadline;
            tender.ModifiedBy = currentUserId;
            tender.ModifiedDate = DateTime.UtcNow;

            _unitOfWork.Tenders.Update(tender);
            await _unitOfWork.CompleteAsync();

            await _auditService.LogAsync(currentUserId, "Update", nameof(Tender), tender.TenderId);

            var saved = await _unitOfWork.Tenders.GetWithDetailsAsync(tender.TenderId);
            return MapToDetailDto(saved!);
        }

        public async Task DeleteTenderAsync(int id, int currentUserId)
        {
            var tender = await _unitOfWork.Tenders.GetByIdAsync(id)
                ?? throw new NotFoundException(nameof(Tender), id);

            if (tender.CurrentStatus != TenderStatus.Draft)
            {
                throw new BusinessRuleException("Only tenders in Draft status can be deleted.");
            }

            // Soft delete - preserves the audit trail.
            tender.IsDeleted = true;
            tender.ModifiedBy = currentUserId;
            tender.ModifiedDate = DateTime.UtcNow;

            _unitOfWork.Tenders.Update(tender);
            await _unitOfWork.CompleteAsync();

            await _auditService.LogAsync(currentUserId, "Delete", nameof(Tender), id);
        }

        public async Task<BOQItemDto> AddBOQItemAsync(CreateBOQItemDto dto)
        {
            _ = await _unitOfWork.Tenders.GetByIdAsync(dto.TenderId)
                ?? throw new NotFoundException(nameof(Tender), dto.TenderId);

            var item = new BOQItem
            {
                TenderId = dto.TenderId,
                ItemName = dto.ItemName,
                Description = dto.Description,
                Quantity = dto.Quantity,
                Unit = dto.Unit,
                Rate = dto.Rate,
                CreatedDate = DateTime.UtcNow
            };

            await _unitOfWork.BOQItems.AddAsync(item);
            await _unitOfWork.CompleteAsync();

            return new BOQItemDto
            {
                BOQId = item.BOQId,
                ItemName = item.ItemName,
                Description = item.Description,
                Quantity = item.Quantity,
                Unit = item.Unit,
                Rate = item.Rate,
                Amount = item.Quantity * item.Rate
            };
        }

        public async Task<List<BOQItemDto>> GetBOQItemsAsync(int tenderId)
        {
            var items = await _unitOfWork.BOQItems.FindAsync(b => b.TenderId == tenderId);
            return items.Select(b => new BOQItemDto
            {
                BOQId = b.BOQId,
                ItemName = b.ItemName,
                Description = b.Description,
                Quantity = b.Quantity,
                Unit = b.Unit,
                Rate = b.Rate,
                Amount = b.Quantity * b.Rate
            }).ToList();
        }

        public async Task<BOQItemDto> UpdateBOQItemAsync(int boqId, UpdateBOQItemDto dto)
        {
            var item = await _unitOfWork.BOQItems.GetByIdAsync(boqId)
                ?? throw new NotFoundException(nameof(BOQItem), boqId);

            if (item.TenderId != dto.TenderId)
            {
                throw new BusinessRuleException("BOQ item tender mismatch.");
            }

            item.ItemName = dto.ItemName;
            item.Description = dto.Description;
            item.Quantity = dto.Quantity;
            item.Unit = dto.Unit;
            item.Rate = dto.Rate;

            _unitOfWork.BOQItems.Update(item);
            await _unitOfWork.CompleteAsync();

            return new BOQItemDto
            {
                BOQId = item.BOQId,
                ItemName = item.ItemName,
                Description = item.Description,
                Quantity = item.Quantity,
                Unit = item.Unit,
                Rate = item.Rate,
                Amount = item.Quantity * item.Rate
            };
        }

        public async Task DeleteBOQItemAsync(int boqId)
        {
            var item = await _unitOfWork.BOQItems.GetByIdAsync(boqId)
                ?? throw new NotFoundException(nameof(BOQItem), boqId);

            _unitOfWork.BOQItems.Remove(item);
            await _unitOfWork.CompleteAsync();
        }

        public async Task<TenderDocumentDto> UploadDocumentAsync(int tenderId, string documentType, Stream fileStream, string fileName, int uploadedBy)
        {
            _ = await _unitOfWork.Tenders.GetByIdAsync(tenderId)
                ?? throw new NotFoundException(nameof(Tender), tenderId);

            var (savedFileName, filePath) = await _fileStorageService.SaveFileAsync(fileStream, fileName, $"tenders/{tenderId}");

            var document = new TenderDocument
            {
                TenderId = tenderId,
                DocumentType = documentType,
                FileName = savedFileName,
                FilePath = filePath,
                UploadedBy = uploadedBy,
                UploadedDate = DateTime.UtcNow
            };

            await _unitOfWork.TenderDocuments.AddAsync(document);
            await _unitOfWork.CompleteAsync();

            await _auditService.LogAsync(uploadedBy, "Upload", nameof(TenderDocument), document.DocumentId);

            return new TenderDocumentDto
            {
                DocumentId = document.DocumentId,
                DocumentType = document.DocumentType,
                FileName = document.FileName,
                FilePath = document.FilePath,
                UploadedDate = document.UploadedDate
            };
        }

        public async Task DeleteDocumentAsync(int documentId, int deletedBy)
        {
            var document = await _unitOfWork.TenderDocuments.GetByIdAsync(documentId)
                ?? throw new NotFoundException(nameof(TenderDocument), documentId);

            await _fileStorageService.DeleteFileAsync(document.FilePath);
            _unitOfWork.TenderDocuments.Remove(document);
            await _unitOfWork.CompleteAsync();

            await _auditService.LogAsync(deletedBy, "DeleteDocument", nameof(TenderDocument), document.DocumentId);
        }

        public async Task<List<TenderDocumentDto>> GetDocumentsAsync(int tenderId)
        {
            var docs = await _unitOfWork.TenderDocuments.FindAsync(d => d.TenderId == tenderId);
            return docs.Select(d => new TenderDocumentDto
            {
                DocumentId = d.DocumentId,
                DocumentType = d.DocumentType,
                FileName = d.FileName,
                FilePath = d.FilePath,
                UploadedDate = d.UploadedDate
            }).ToList();
        }

        private static TenderListItemDto MapToListItemDto(Tender t) => new()
        {
            TenderId = t.TenderId,
            TenderNumber = t.TenderNumber,
            TrackingId = t.TrackingId,
            Title = t.Title,
            DepartmentName = t.Department?.DepartmentName ?? string.Empty,
            TenderType = t.TenderType,
            EstimatedAmount = t.EstimatedAmount,
            CurrentStatus = t.CurrentStatus,
            PublicationDate = t.ApprovalHistories
                .Where(h => h.NewStatus == TenderStatus.TenderPublished)
                .Select(h => (DateTime?)h.ApprovalDate)
                .Min(),
            SubmissionStartDate = t.SubmissionStartDate,
            SubmissionDeadline = t.SubmissionDeadline,
            AssignedRespOfficer = TenderAssignmentMapper.GetAssignedRespOfficer(t),
            AssignedRole = TenderAssignmentMapper.GetAssignedRole(t.CurrentStatus),
            CreatedDate = t.CreatedDate,
            CanApply = TenderApplicationRules.CanApply(t, DateTime.UtcNow),
            ApplicationStatus = TenderApplicationRules.GetApplicationStatus(t, DateTime.UtcNow)
        };

        private static TenderDetailDto MapToDetailDto(Tender t) => new()
        {
            TenderId = t.TenderId,
            TenderNumber = t.TenderNumber,
            TrackingId = t.TrackingId,
            Title = t.Title,
            Description = t.Description,
            DepartmentName = t.Department?.DepartmentName ?? string.Empty,
            EstimatedAmount = t.EstimatedAmount,
            CurrentStatus = t.CurrentStatus,
            TenderType = t.TenderType,
            PublicationDate = t.ApprovalHistories
                .Where(h => h.NewStatus == TenderStatus.TenderPublished)
                .Select(h => (DateTime?)h.ApprovalDate)
                .Min(),
            SubmissionStartDate = t.SubmissionStartDate,
            SubmissionDeadline = t.SubmissionDeadline,
            AssignedRespOfficer = TenderAssignmentMapper.GetAssignedRespOfficer(t),
            AssignedRole = TenderAssignmentMapper.GetAssignedRole(t.CurrentStatus),
            CreatedDate = t.CreatedDate,
            CanApply = TenderApplicationRules.CanApply(t, DateTime.UtcNow),
            ApplicationStatus = TenderApplicationRules.GetApplicationStatus(t, DateTime.UtcNow),
            CreatedByName = t.CreatedByUser?.FullName ?? string.Empty,
            BOQItems = t.BOQItems.Select(b => new BOQItemDto
            {
                BOQId = b.BOQId,
                ItemName = b.ItemName,
                Description = b.Description,
                Quantity = b.Quantity,
                Unit = b.Unit,
                Rate = b.Rate,
                Amount = b.Quantity * b.Rate
            }).ToList(),
            Documents = t.Documents.Select(d => new TenderDocumentDto
            {
                DocumentId = d.DocumentId,
                DocumentType = d.DocumentType,
                FileName = d.FileName,
                FilePath = d.FilePath,
                UploadedDate = d.UploadedDate
            }).ToList()
        };
    }
}
