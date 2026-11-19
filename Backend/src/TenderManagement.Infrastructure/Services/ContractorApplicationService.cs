using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TenderManagement.Application.Common;
using TenderManagement.Application.DTOs.Bid;
using TenderManagement.Application.Interfaces;
using TenderManagement.Application.Services;
using TenderManagement.Domain.Entities;
using TenderManagement.Domain.Enums;
using TenderManagement.Infrastructure.Data;

namespace TenderManagement.Infrastructure.Services
{
    public sealed class ContractorApplicationService : IContractorApplicationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileStorageService _fileStorage;
        private readonly IAuditService _audit;

        public ContractorApplicationService(
            ApplicationDbContext context,
            IFileStorageService fileStorage,
            IAuditService audit)
        {
            _context = context;
            _fileStorage = fileStorage;
            _audit = audit;
        }

        public async Task<IReadOnlyList<TenderApplicationRequirementDto>> GetRequirementsAsync(
            int tenderId, bool publicOnly, CancellationToken cancellationToken)
        {
            var tender = await _context.Tenders.AsNoTracking()
                .FirstOrDefaultAsync(item => item.TenderId == tenderId, cancellationToken)
                ?? throw new NotFoundException(nameof(Tender), tenderId);

            if (publicOnly && tender.CurrentStatus != TenderStatus.TenderPublished)
                throw new NotFoundException(nameof(Tender), tenderId);

            return await _context.TenderRequiredBidDocuments.AsNoTracking()
                .Where(item => item.TenderId == tenderId)
                .OrderBy(item => item.DocumentType)
                .Select(item => new TenderApplicationRequirementDto(
                    item.DocumentType,
                    item.Description,
                    item.IsMandatory,
                    item.AllowedExtensions,
                    item.MaxFileSizeBytes))
                .ToListAsync(cancellationToken);
        }

        public async Task SetRequirementsAsync(
            int tenderId, int userId, IReadOnlyList<TenderApplicationRequirementDto> requirements,
            CancellationToken cancellationToken)
        {
            var (user, role) = await GetUserAndRoleAsync(userId, cancellationToken);
            EnsureJuniorEngineer(user, role);
            var tender = await _context.Tenders.FirstOrDefaultAsync(
                item => item.TenderId == tenderId, cancellationToken)
                ?? throw new NotFoundException(nameof(Tender), tenderId);
            if (tender.CurrentStatus != TenderStatus.Draft)
                throw new BusinessRuleException("Document requirements can only be changed while the tender is in Draft.");
            if (await _context.TechnicalBids.AnyAsync(item => item.TenderId == tenderId, cancellationToken))
                throw new BusinessRuleException("Document requirements cannot change after an application has been created.");
            if (requirements.Count > 30 ||
                requirements.Select(item => item.DocumentType.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != requirements.Count)
                throw new BusinessRuleException("Use no more than 30 uniquely named document requirements.");

            foreach (var requirement in requirements)
            {
                if (string.IsNullOrWhiteSpace(requirement.DocumentType) ||
                    requirement.DocumentType.Trim().Length > 160 ||
                    requirement.Description?.Length > 1000 ||
                    requirement.MaxFileSizeBytes is < 1 or > 10_000_000)
                    throw new BusinessRuleException("Each document requirement needs a name, valid description, and a file limit up to 10 MB.");

                if (!AreAllowedExtensionsValid(requirement.AllowedExtensions))
                    throw new BusinessRuleException("Allowed extensions must be selected from the supported document formats.");
            }

            var current = await _context.TenderRequiredBidDocuments
                .Where(item => item.TenderId == tenderId)
                .ToListAsync(cancellationToken);
            _context.TenderRequiredBidDocuments.RemoveRange(current);
            _context.TenderRequiredBidDocuments.AddRange(requirements.Select(requirement =>
                new TenderRequiredBidDocument
                {
                    TenderId = tenderId,
                    DocumentType = requirement.DocumentType.Trim(),
                    Description = NullIfWhiteSpace(requirement.Description),
                    IsMandatory = requirement.IsMandatory,
                    AllowedExtensions = NormalizeExtensions(requirement.AllowedExtensions),
                    MaxFileSizeBytes = requirement.MaxFileSizeBytes,
                    CreatedBy = userId,
                    CreatedAtUtc = DateTime.UtcNow
                }));

            await _context.SaveChangesAsync(cancellationToken);
            await _audit.LogAsync(userId, "UpdateApplicationRequirements", nameof(Tender), tenderId);
        }

        public async Task<ContractorApplicationDetailDto> SaveDraftAsync(
            int tenderId, int userId, IReadOnlyList<ContractorApplicationFile> files,
            CancellationToken cancellationToken)
        {
            var (user, _) = await GetUserAndRoleAsync(userId, cancellationToken);
            EnsureContractor(user);
            var contractor = await _context.Contractors
                .FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken)
                ?? throw new ForbiddenActionException("Complete contractor registration before applying.");
            if (!contractor.IsActive)
                throw new ForbiddenActionException("This contractor profile is inactive.");

            var tender = await _context.Tenders.FirstOrDefaultAsync(
                item => item.TenderId == tenderId, cancellationToken)
                ?? throw new NotFoundException(nameof(Tender), tenderId);
            EnsureApplicationWindow(tender);

            var bid = await _context.TechnicalBids
                .Include(item => item.Documents)
                .FirstOrDefaultAsync(
                    item => item.TenderId == tenderId && item.ContractorId == contractor.ContractorId,
                    cancellationToken);
            if (bid is not null && bid.Status != "Draft")
                throw new BusinessRuleException("An application already exists for this tender and contractor.");
            var isNew = bid is null;
            var now = DateTime.UtcNow;
            bid ??= new TechnicalBid
            {
                TenderId = tenderId,
                ContractorId = contractor.ContractorId,
                Status = "Draft",
                SubmittedDate = now
            };

            var requirements = await _context.TenderRequiredBidDocuments
                .Where(item => item.TenderId == tenderId)
                .ToDictionaryAsync(item => item.DocumentType, StringComparer.OrdinalIgnoreCase, cancellationToken);
            var savedPaths = new List<string>();
            try
            {
                await AttachFilesAsync(
                    bid, contractor.ContractorId, userId, files, requirements, savedPaths, cancellationToken);

                if (isNew)
                {
                    _context.TechnicalBids.Add(bid);
                    _context.ContractorApplicationHistory.Add(NewHistory(
                        bid, userId, user.RoleId, "DraftCreated", null, "Draft", null, false));
                }
                else if (files.Count > 0)
                {
                    _context.ContractorApplicationHistory.Add(NewHistory(
                        bid, userId, user.RoleId, "DocumentsUploaded", bid.Status, bid.Status, null, false));
                }

                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
            {
                await DeleteFilesAsync(savedPaths);
                throw new BusinessRuleException("An application already exists for this tender and contractor.");
            }
            catch
            {
                await DeleteFilesAsync(savedPaths);
                throw;
            }

            await _audit.LogAsync(userId, isNew ? "CreateApplicationDraft" : "UpdateApplicationDraft",
                nameof(TechnicalBid), bid.TechnicalBidId, tender.TenderNumber);
            return await GetDetailsAsync(bid.TechnicalBidId, userId, true, false, cancellationToken);
        }

        public async Task<ContractorApplicationDetailDto> SubmitAsync(
            int applicationId, int userId, bool termsAccepted, string termsVersion,
            CancellationToken cancellationToken)
        {
            if (!termsAccepted)
                throw new BusinessRuleException("Accept the Terms & Conditions before submitting.");
            if (!string.Equals(termsVersion, TenderApplicationRules.TermsVersion, StringComparison.Ordinal))
                throw new BusinessRuleException("The Terms & Conditions have changed. Review them and submit again.");

            var user = await GetUserAsync(userId, cancellationToken);
            EnsureContractor(user);
            var bid = await _context.TechnicalBids
                .Include(item => item.Tender)
                .Include(item => item.Documents)
                .Include(item => item.Contractor).ThenInclude(item => item.User)
                .FirstOrDefaultAsync(item => item.TechnicalBidId == applicationId, cancellationToken)
                ?? throw new NotFoundException(nameof(TechnicalBid), applicationId);
            if (bid.Contractor.UserId != userId)
                throw new NotFoundException(nameof(TechnicalBid), applicationId);
            if (bid.Status != "Draft" && bid.Status != "Shortfall")
                throw new BusinessRuleException("Only a draft or application with an open clarification can be submitted.");
            EnsureApplicationWindow(bid.Tender);

            var requirements = await _context.TenderRequiredBidDocuments.AsNoTracking()
                .Where(item => item.TenderId == bid.TenderId && item.IsMandatory)
                .ToListAsync(cancellationToken);
            var uploadedTypes = bid.Documents.Select(document => document.DocumentType)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var missing = requirements.Where(requirement => !uploadedTypes.Contains(requirement.DocumentType))
                .Select(requirement => requirement.DocumentType)
                .ToList();
            if (missing.Count > 0)
                throw new BusinessRuleException($"Upload all mandatory documents before submitting: {string.Join(", ", missing)}.");

            var previousStatus = bid.Status;
            var now = DateTime.UtcNow;
            bid.Status = "Submitted";
            bid.SubmittedDate = now;
            bid.TermsAcceptedAt = now;
            bid.TermsVersion = termsVersion;
            _context.ContractorApplicationHistory.Add(NewHistory(
                bid, userId, user.RoleId, "Submitted", previousStatus, bid.Status, null, true));

            var jeRoleIds = await _context.Roles.AsNoTracking()
                .Where(role => role.IsActive &&
                    (role.RoleCode == RoleNames.JuniorEngineerCode || role.RoleName == RoleNames.JuniorEngineer))
                .Select(role => role.RoleId)
                .ToListAsync(cancellationToken);
            var jeUsers = await _context.Users
                .Where(account => account.IsActive && account.AccountType == "Internal" &&
                    account.RoleId.HasValue && jeRoleIds.Contains(account.RoleId.Value))
                .Select(account => account.UserId)
                .ToListAsync(cancellationToken);
            foreach (var jeUserId in jeUsers)
            {
                _context.TenderNotifications.Add(new TenderNotification
                {
                    UserId = jeUserId,
                    TenderId = bid.TenderId,
                    NotificationType = "ApplicationSubmitted",
                    Message = $"A contractor submitted application {GetReference(bid)}.",
                    CreatedAtUtc = now
                });
            }

            await _context.SaveChangesAsync(cancellationToken);
            await _audit.LogAsync(userId, "SubmitTenderApplication", nameof(TechnicalBid),
                bid.TechnicalBidId, bid.Tender.TenderNumber);
            return await GetDetailsAsync(applicationId, userId, true, false, cancellationToken);
        }

        public async Task<IReadOnlyList<ContractorApplicationListItemDto>> GetMyApplicationsAsync(
            int userId, CancellationToken cancellationToken)
        {
            var user = await GetUserAsync(userId, cancellationToken);
            EnsureContractor(user);
            var contractorId = await GetContractorIdAsync(userId, cancellationToken);
            var applications = await _context.TechnicalBids.AsNoTracking()
                .Include(item => item.Tender)
                .Include(item => item.Contractor).ThenInclude(item => item.User)
                .Include(item => item.Documents)
                .Where(item => item.ContractorId == contractorId)
                .OrderByDescending(item => item.SubmittedDate)
                .ToListAsync(cancellationToken);
            return await MapListAsync(applications, cancellationToken);
        }

        public async Task<IReadOnlyList<ContractorApplicationListItemDto>> GetApplicationsAsync(
            int userId, bool tenderCell, CancellationToken cancellationToken)
        {
            var (user, role) = await GetUserAndRoleAsync(userId, cancellationToken);
            if (tenderCell)
                EnsureTenderCell(user, role);
            else
                EnsureJuniorEngineer(user, role);

            var query = _context.TechnicalBids.AsNoTracking()
                .Include(item => item.Tender)
                .Include(item => item.Contractor).ThenInclude(item => item.User)
                .Include(item => item.Documents)
                .AsQueryable();
            if (tenderCell)
            {
                var roleId = role!.RoleId;
                query = query.Where(bid => _context.TenderWorkflowAssignments.Any(assignment =>
                    assignment.TenderId == bid.TenderId &&
                    assignment.IsCurrent &&
                    assignment.AssignedRoleId == roleId &&
                    (!assignment.AssignedUserId.HasValue || assignment.AssignedUserId == userId)));
            }

            var applications = await query.OrderByDescending(item => item.SubmittedDate)
                .ToListAsync(cancellationToken);
            return await MapListAsync(applications, cancellationToken);
        }

        public async Task<ContractorApplicationDashboardDto> GetDashboardAsync(
            int userId, CancellationToken cancellationToken)
        {
            var user = await GetUserAsync(userId, cancellationToken);
            EnsureContractor(user);
            var contractor = await _context.Contractors.AsNoTracking()
                .FirstOrDefaultAsync(item => item.UserId == userId && item.IsActive, cancellationToken)
                ?? throw new ForbiddenActionException("Complete contractor registration before using the dashboard.");
            var applications = await GetMyApplicationsAsync(userId, cancellationToken);
            var notifications = await (
                from notification in _context.TenderNotifications.AsNoTracking()
                join tender in _context.Tenders.AsNoTracking() on notification.TenderId equals tender.TenderId
                where notification.UserId == userId
                orderby notification.CreatedAtUtc descending
                select new ContractorNotificationDto(
                    notification.NotificationId,
                    notification.TenderId,
                    tender.TenderNumber,
                    notification.Message,
                    notification.NotificationType,
                    notification.CreatedAtUtc,
                    notification.ReadAtUtc))
                .Take(20)
                .ToListAsync(cancellationToken);
            return new ContractorApplicationDashboardDto(
                user.FullName,
                contractor.Name,
                applications.Count,
                applications.Count(item => item.Status == "Draft"),
                applications.Count(item => item.Status == "Submitted"),
                applications.Count(item => item.Status is "UnderVerification" or "Under Review"),
                applications.Count(item => item.Status == "Shortfall"),
                applications.Count(item => item.Status is "Qualified" or "Approved"),
                applications.Count(item => item.Status is "Disqualified" or "Rejected"),
                notifications,
                applications);
        }

        public async Task MarkNotificationReadAsync(
            long notificationId, int userId, CancellationToken cancellationToken)
        {
            var user = await GetUserAsync(userId, cancellationToken);
            EnsureContractor(user);
            var notification = await _context.TenderNotifications.FirstOrDefaultAsync(
                item => item.NotificationId == notificationId && item.UserId == userId,
                cancellationToken)
                ?? throw new NotFoundException(nameof(TenderNotification), notificationId);
            notification.ReadAtUtc ??= DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<ContractorApplicationDetailDto> GetDetailsAsync(
            int applicationId, int userId, bool contractor, bool tenderCell,
            CancellationToken cancellationToken)
        {
            var bid = await _context.TechnicalBids.AsNoTracking()
                .Include(item => item.Tender).ThenInclude(item => item.Department)
                .Include(item => item.Contractor).ThenInclude(item => item.User)
                .Include(item => item.Documents)
                .FirstOrDefaultAsync(item => item.TechnicalBidId == applicationId, cancellationToken)
                ?? throw new NotFoundException(nameof(TechnicalBid), applicationId);
            await EnsureCanAccessAsync(bid, userId, contractor, tenderCell, cancellationToken);

            var requirements = await GetRequirementsForTenderAsync(bid.TenderId, cancellationToken);
            var documents = bid.Documents.OrderBy(item => item.UploadedDate).ToList();
            var remarksQuery = _context.ContractorApplicationRemarks.AsNoTracking()
                .Where(item => item.TechnicalBidId == applicationId);
            var historyQuery = _context.ContractorApplicationHistory.AsNoTracking()
                .Where(item => item.TechnicalBidId == applicationId);
            if (contractor)
            {
                remarksQuery = remarksQuery.Where(item => item.VisibleToContractor);
                historyQuery = historyQuery.Where(item => item.VisibleToContractor);
            }

            var remarks = await remarksQuery.OrderBy(item => item.CreatedAtUtc).ToListAsync(cancellationToken);
            var history = await historyQuery.OrderBy(item => item.CreatedAtUtc).ToListAsync(cancellationToken);
            var documentIds = documents.Select(item => item.TechnicalBidDocumentId).ToList();
            var verificationHistory = await _context.TenderDocumentVerificationHistory.AsNoTracking()
                .Where(item => item.TechnicalBidDocumentId.HasValue &&
                    documentIds.Contains(item.TechnicalBidDocumentId.Value))
                .OrderByDescending(item => item.ReviewedAtUtc)
                .ToListAsync(cancellationToken);
            var latestVerification = verificationHistory
                .GroupBy(item => item.TechnicalBidDocumentId!.Value)
                .ToDictionary(group => group.Key, group => group.First());
            var authorIds = remarks.Select(item => item.CreatedBy).Concat(history.Select(item => item.CreatedBy))
                .Distinct().ToList();
            var authors = await _context.Users.AsNoTracking()
                .Where(item => authorIds.Contains(item.UserId))
                .ToDictionaryAsync(item => item.UserId, item => item.FullName, cancellationToken);
            var roleIds = remarks.Select(item => item.RoleId).Concat(history.Select(item => item.RoleId))
                .Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
            var roles = await _context.Roles.AsNoTracking()
                .Where(item => roleIds.Contains(item.RoleId))
                .ToDictionaryAsync(item => item.RoleId, item => item.RoleName, cancellationToken);
            var uploaderIds = documents.Where(item => item.UploadedBy.HasValue)
                .Select(item => item.UploadedBy!.Value).Distinct().ToList();
            var uploaders = await _context.Users.AsNoTracking()
                .Where(item => uploaderIds.Contains(item.UserId))
                .ToDictionaryAsync(item => item.UserId, item => item.FullName, cancellationToken);

            return new ContractorApplicationDetailDto(
                bid.TechnicalBidId,
                bid.TenderId,
                bid.Tender.TenderNumber,
                bid.Tender.Title,
                bid.Tender.Department?.DepartmentName ?? string.Empty,
                bid.Tender.EstimatedAmount,
                bid.Contractor.Name,
                bid.Contractor.User?.FullName,
                bid.Contractor.Email,
                bid.Contractor.ContactNo,
                bid.Contractor.Address,
                GetReference(bid),
                bid.Status,
                bid.SubmittedDate,
                bid.Status == "Draft" ? null : bid.SubmittedDate,
                requirements,
                documents.Select(item =>
                {
                    latestVerification.TryGetValue(item.TechnicalBidDocumentId, out var review);
                    return new ContractorApplicationDocumentDto(
                        item.TechnicalBidDocumentId,
                        item.DocumentType,
                        item.FileName,
                        item.UploadedDate,
                        item.UploadedBy.HasValue && uploaders.TryGetValue(item.UploadedBy.Value, out var uploader)
                            ? uploader
                            : bid.Contractor.Name,
                        review?.VerificationStatus,
                        contractor && review?.VerificationStatus == "Verified" ? null : review?.Remarks,
                        review?.ReviewedAtUtc);
                }).ToList(),
                remarks.Select(item => new ContractorApplicationRemarkDto(
                    item.RemarkId,
                    item.RemarkType,
                    item.Message,
                    authors.GetValueOrDefault(item.CreatedBy, "Unknown"),
                    item.RoleId.HasValue ? roles.GetValueOrDefault(item.RoleId.Value) : null,
                    item.VisibleToContractor,
                    item.CreatedAtUtc)).ToList(),
                history.Select(item => new ContractorApplicationHistoryDto(
                    item.HistoryId,
                    item.EventType,
                    item.PreviousStatus,
                    item.NewStatus,
                    item.Remarks,
                    authors.GetValueOrDefault(item.CreatedBy, "Unknown"),
                    item.RoleId.HasValue ? roles.GetValueOrDefault(item.RoleId.Value) : null,
                    item.VisibleToContractor,
                    item.CreatedAtUtc)).ToList());
        }

        public async Task AddDocumentsAsync(
            int applicationId, int userId, bool contractor, bool tenderCell,
            IReadOnlyList<ContractorApplicationFile> files, CancellationToken cancellationToken)
        {
            var bid = await _context.TechnicalBids
                .Include(item => item.Tender)
                .Include(item => item.Contractor)
                .Include(item => item.Documents)
                .FirstOrDefaultAsync(item => item.TechnicalBidId == applicationId, cancellationToken)
                ?? throw new NotFoundException(nameof(TechnicalBid), applicationId);
            await EnsureCanAccessAsync(bid, userId, contractor, tenderCell, cancellationToken);
            if (!contractor)
                throw new ForbiddenActionException("Only the contractor can upload application documents.");
            if (bid.Status is not ("Draft" or "Shortfall"))
                throw new BusinessRuleException("Documents may only be added to a draft or an application with an open clarification.");

            var requirements = await _context.TenderRequiredBidDocuments
                .Where(item => item.TenderId == bid.TenderId)
                .ToDictionaryAsync(item => item.DocumentType, StringComparer.OrdinalIgnoreCase, cancellationToken);
            var paths = new List<string>();
            try
            {
                await AttachFilesAsync(bid, bid.ContractorId, userId, files, requirements, paths, cancellationToken);
                if (files.Count > 0)
                {
                    _context.ContractorApplicationHistory.Add(NewHistory(
                        bid, userId, null, "DocumentsUploaded", bid.Status, bid.Status, null, bid.Status == "Shortfall"));
                }
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                await DeleteFilesAsync(paths);
                throw;
            }

            await _audit.LogAsync(userId, "UploadApplicationDocuments", nameof(TechnicalBid), applicationId);
        }

        public async Task<ContractorApplicationDetailDto> AddRemarkAsync(
            int applicationId, int userId, bool contractor, bool tenderCell,
            AddContractorApplicationRemarkRequest request, CancellationToken cancellationToken)
        {
            var (user, role) = await GetUserAndRoleAsync(userId, cancellationToken);
            var bid = await _context.TechnicalBids
                .Include(item => item.Tender)
                .Include(item => item.Contractor)
                .FirstOrDefaultAsync(item => item.TechnicalBidId == applicationId, cancellationToken)
                ?? throw new NotFoundException(nameof(TechnicalBid), applicationId);
            await EnsureCanAccessAsync(bid, userId, contractor, tenderCell, cancellationToken);
            var message = request.Message?.Trim();
            if (string.IsNullOrWhiteSpace(message) || message.Length > 2000)
                throw new BusinessRuleException("Remark must contain 1 to 2000 characters.");

            string remarkType;
            bool visibleToContractor;
            if (contractor)
            {
                if (!string.Equals(request.RemarkType, "Response", StringComparison.OrdinalIgnoreCase) ||
                    bid.Status != "Shortfall")
                    throw new ForbiddenActionException("Contractors may respond only to an open clarification.");
                remarkType = "Response";
                visibleToContractor = true;
            }
            else
            {
                if (tenderCell)
                    EnsureTenderCell(user, role);
                else
                    EnsureJuniorEngineer(user, role);
                if (request.RemarkType is not ("Internal" or "Clarification"))
                    throw new BusinessRuleException("Remark type must be Internal or Clarification.");
                remarkType = request.RemarkType;
                visibleToContractor = remarkType == "Clarification";
            }

            var previousStatus = bid.Status;
            if (remarkType == "Clarification")
                bid.Status = "Shortfall";
            var now = DateTime.UtcNow;
            _context.ContractorApplicationRemarks.Add(new ContractorApplicationRemark
            {
                TechnicalBidId = applicationId,
                RemarkType = remarkType,
                Message = message,
                CreatedBy = userId,
                RoleId = user.RoleId,
                VisibleToContractor = visibleToContractor,
                CreatedAtUtc = now
            });
            _context.ContractorApplicationHistory.Add(NewHistory(
                bid, userId, user.RoleId, remarkType, previousStatus, bid.Status, message, visibleToContractor));

            if (remarkType == "Clarification" && bid.Contractor.UserId.HasValue)
            {
                _context.TenderNotifications.Add(new TenderNotification
                {
                    UserId = bid.Contractor.UserId.Value,
                    TenderId = bid.TenderId,
                    NotificationType = "ApplicationClarification",
                    Message = $"A clarification is required for application {GetReference(bid)}.",
                    CreatedAtUtc = now
                });
            }

            await _context.SaveChangesAsync(cancellationToken);
            await _audit.LogAsync(userId, "AddApplicationRemark", nameof(ContractorApplicationRemark), applicationId, remarkType);
            return await GetDetailsAsync(applicationId, userId, contractor, tenderCell, cancellationToken);
        }

        public async Task<ContractorApplicationDetailDto> ReviewDocumentAsync(
            int applicationId, int documentId, int userId, ReviewApplicationDocumentRequest request,
            CancellationToken cancellationToken)
        {
            var (user, role) = await GetUserAndRoleAsync(userId, cancellationToken);
            EnsureTenderCell(user, role);
            var bid = await _context.TechnicalBids
                .Include(item => item.Tender)
                .Include(item => item.Contractor)
                .FirstOrDefaultAsync(item => item.TechnicalBidId == applicationId, cancellationToken)
                ?? throw new NotFoundException(nameof(TechnicalBid), applicationId);
            await EnsureCanAccessAsync(bid, userId, false, true, cancellationToken);
            var document = await _context.TechnicalBidDocuments.FirstOrDefaultAsync(
                item => item.TechnicalBidDocumentId == documentId && item.TechnicalBidId == applicationId,
                cancellationToken)
                ?? throw new NotFoundException(nameof(TechnicalBidDocument), documentId);

            var status = request.VerificationStatus?.Trim();
            if (status is not ("Verified" or "Shortfall" or "Rejected"))
                throw new BusinessRuleException("Document status must be Verified, Shortfall, or Rejected.");
            var remarks = NullIfWhiteSpace(request.Remarks);
            if (status != "Verified" && string.IsNullOrWhiteSpace(remarks))
                throw new BusinessRuleException("Provide remarks for a shortfall or rejected document.");

            var previousStatus = bid.Status;
            if (status == "Verified" && bid.Status == "Submitted")
                bid.Status = "UnderVerification";
            else if (status == "Shortfall")
                bid.Status = "Shortfall";
            else if (status == "Rejected")
                bid.Status = "Disqualified";

            var now = DateTime.UtcNow;
            _context.TenderDocumentVerificationHistory.Add(new TenderDocumentVerificationHistory
            {
                TenderId = bid.TenderId,
                ContractorId = bid.ContractorId,
                TechnicalBidDocumentId = documentId,
                FileName = document.FileName,
                FilePath = document.FilePath,
                DocumentType = document.DocumentType,
                ReviewStage = "Technical",
                VerificationStatus = status,
                ReviewerId = userId,
                ReviewerRoleId = role!.RoleId,
                Remarks = remarks,
                ReviewedAtUtc = now
            });
            _context.ContractorApplicationHistory.Add(NewHistory(
                bid, userId, role.RoleId, "Document" + status, previousStatus, bid.Status, remarks, true));

            if (status is "Shortfall" or "Rejected")
            {
                _context.ContractorApplicationRemarks.Add(new ContractorApplicationRemark
                {
                    TechnicalBidId = applicationId,
                    RemarkType = status == "Shortfall" ? "Clarification" : "Decision",
                    Message = remarks!,
                    CreatedBy = userId,
                    RoleId = role.RoleId,
                    VisibleToContractor = true,
                    CreatedAtUtc = now
                });
                if (bid.Contractor.UserId.HasValue)
                {
                    _context.TenderNotifications.Add(new TenderNotification
                    {
                        UserId = bid.Contractor.UserId.Value,
                        TenderId = bid.TenderId,
                        NotificationType = status == "Shortfall" ? "ApplicationClarification" : "ApplicationDecision",
                        Message = status == "Shortfall"
                            ? $"A document clarification is required for application {GetReference(bid)}."
                            : $"A document was rejected for application {GetReference(bid)}.",
                        CreatedAtUtc = now
                    });
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            await _audit.LogAsync(userId, "VerifyApplicationDocument", nameof(TechnicalBidDocument), documentId, status);
            return await GetDetailsAsync(applicationId, userId, false, true, cancellationToken);
        }

        public async Task<ContractorApplicationDetailDto> ReviewApplicationAsync(
            int applicationId, int userId, bool tenderCell,
            ReviewContractorApplicationRequest request, CancellationToken cancellationToken)
        {
            var (user, role) = await GetUserAndRoleAsync(userId, cancellationToken);
            if (tenderCell)
                EnsureTenderCell(user, role);
            else
                EnsureJuniorEngineer(user, role);

            var bid = await _context.TechnicalBids
                .Include(item => item.Tender)
                .Include(item => item.Contractor)
                .FirstOrDefaultAsync(item => item.TechnicalBidId == applicationId, cancellationToken)
                ?? throw new NotFoundException(nameof(TechnicalBid), applicationId);
            await EnsureCanAccessAsync(bid, userId, false, tenderCell, cancellationToken);
            var decision = request.Decision?.Trim();
            if (decision is not ("Qualified" or "Disqualified"))
                throw new BusinessRuleException("Decision must be Qualified or Disqualified.");
            var remarks = NullIfWhiteSpace(request.Remarks);
            if (decision == "Disqualified" && string.IsNullOrWhiteSpace(remarks))
                throw new BusinessRuleException("Provide a reason when disqualifying an application.");

            if (decision == "Qualified")
            {
                var requiredTypes = await _context.TenderRequiredBidDocuments.AsNoTracking()
                    .Where(item => item.TenderId == bid.TenderId && item.IsMandatory)
                    .Select(item => item.DocumentType)
                    .ToListAsync(cancellationToken);
                var documents = await _context.TechnicalBidDocuments.AsNoTracking()
                    .Where(item => item.TechnicalBidId == applicationId)
                    .Select(item => new { item.TechnicalBidDocumentId, item.DocumentType })
                    .ToListAsync(cancellationToken);
                var documentIds = documents.Select(document => document.TechnicalBidDocumentId).ToList();
                var verificationHistory = await _context.TenderDocumentVerificationHistory.AsNoTracking()
                    .Where(item => item.TechnicalBidDocumentId.HasValue &&
                        documentIds.Contains(item.TechnicalBidDocumentId.Value))
                    .OrderByDescending(item => item.ReviewedAtUtc)
                    .ToListAsync(cancellationToken);
                var latest = verificationHistory
                    .GroupBy(item => item.TechnicalBidDocumentId!.Value)
                    .Select(group => group.First())
                    .ToList();
                var verifiedTypes = latest.Where(item => item.VerificationStatus == "Verified")
                    .Select(item => documents.First(document =>
                        document.TechnicalBidDocumentId == item.TechnicalBidDocumentId).DocumentType)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                if (requiredTypes.Any(type => !verifiedTypes.Contains(type)))
                    throw new BusinessRuleException("Verify every mandatory document before qualifying this application.");
            }

            var previousStatus = bid.Status;
            var now = DateTime.UtcNow;
            bid.Status = decision;
            bid.Remarks = remarks;
            bid.EvaluatedBy = userId;
            bid.EvaluatedDate = now;
            _context.TenderVerificationReviews.Add(new TenderVerificationReview
            {
                TenderId = bid.TenderId,
                ContractorId = bid.ContractorId,
                ReviewStage = "Technical",
                Decision = decision,
                ReviewerId = userId,
                ReviewerRoleId = role!.RoleId,
                Remarks = remarks,
                ReviewedAtUtc = now
            });
            _context.ContractorApplicationHistory.Add(NewHistory(
                bid, userId, role.RoleId, "TechnicalReview", previousStatus, decision, remarks, true));
            if (!string.IsNullOrWhiteSpace(remarks))
            {
                _context.ContractorApplicationRemarks.Add(new ContractorApplicationRemark
                {
                    TechnicalBidId = applicationId,
                    RemarkType = "Decision",
                    Message = remarks,
                    CreatedBy = userId,
                    RoleId = role.RoleId,
                    VisibleToContractor = true,
                    CreatedAtUtc = now
                });
            }
            if (bid.Contractor.UserId.HasValue)
            {
                _context.TenderNotifications.Add(new TenderNotification
                {
                    UserId = bid.Contractor.UserId.Value,
                    TenderId = bid.TenderId,
                    NotificationType = "ApplicationDecision",
                    Message = $"Application {GetReference(bid)} was {decision.ToLowerInvariant()}.",
                    CreatedAtUtc = now
                });
            }

            await _context.SaveChangesAsync(cancellationToken);
            await _audit.LogAsync(userId, "ReviewContractorApplication", nameof(TechnicalBid), applicationId, decision);
            return await GetDetailsAsync(applicationId, userId, false, tenderCell, cancellationToken);
        }

        public async Task<(Stream Content, string FileName)> OpenDocumentAsync(
            int applicationId, int documentId, int userId, bool contractor, bool tenderCell,
            CancellationToken cancellationToken)
        {
            var bid = await _context.TechnicalBids.AsNoTracking()
                .Include(item => item.Contractor).ThenInclude(item => item.User)
                .FirstOrDefaultAsync(item => item.TechnicalBidId == applicationId, cancellationToken)
                ?? throw new NotFoundException(nameof(TechnicalBid), applicationId);
            await EnsureCanAccessAsync(bid, userId, contractor, tenderCell, cancellationToken);
            var document = await _context.TechnicalBidDocuments.AsNoTracking()
                .FirstOrDefaultAsync(item =>
                    item.TechnicalBidDocumentId == documentId && item.TechnicalBidId == applicationId,
                    cancellationToken)
                ?? throw new NotFoundException(nameof(TechnicalBidDocument), documentId);
            var content = await _fileStorage.OpenPrivateFileAsync(document.FilePath)
                ?? throw new NotFoundException(nameof(TechnicalBidDocument), documentId);
            return (content, document.FileName);
        }

        private async Task AttachFilesAsync(
            TechnicalBid bid, int contractorId, int userId, IReadOnlyList<ContractorApplicationFile> files,
            IReadOnlyDictionary<string, TenderRequiredBidDocument> requirements,
            ICollection<string> savedPaths, CancellationToken cancellationToken)
        {
            if (files.Count > 10)
                throw new BusinessRuleException("Upload no more than 10 documents per request.");

            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!requirements.TryGetValue(file.DocumentType, out var requirement))
                    throw new BusinessRuleException($"'{file.DocumentType}' is not a document requirement for this tender.");
                if (file.Length <= 0 || file.Length > requirement.MaxFileSizeBytes)
                    throw new BusinessRuleException($"'{file.FileName}' exceeds the size limit for {requirement.DocumentType}.");
                if (string.IsNullOrWhiteSpace(file.FileName) || Path.GetFileName(file.FileName).Length > 260)
                    throw new BusinessRuleException("A document file name is invalid or too long.");

                var extension = Path.GetExtension(file.FileName);
                var allowed = GetAllowedExtensions(requirement.AllowedExtensions);
                if (!allowed.Contains(extension))
                    throw new BusinessRuleException($"File type '{extension}' is not permitted for {requirement.DocumentType}.");

                var saved = await _fileStorage.SavePrivateFileAsync(
                    file.Content,
                    Path.GetFileName(file.FileName),
                    Path.Combine("tender-applications", bid.TenderId.ToString(), contractorId.ToString()));
                savedPaths.Add(saved.FilePath);
                bid.Documents.Add(new TechnicalBidDocument
                {
                    DocumentType = requirement.DocumentType,
                    FileName = Path.GetFileName(file.FileName),
                    FilePath = saved.FilePath,
                    UploadedDate = DateTime.UtcNow,
                    UploadedBy = userId
                });
            }
        }

        private async Task<IReadOnlyList<ContractorApplicationListItemDto>> MapListAsync(
            IReadOnlyList<TechnicalBid> applications, CancellationToken cancellationToken)
        {
            var tenderIds = applications.Select(item => item.TenderId).Distinct().ToList();
            var requirements = await _context.TenderRequiredBidDocuments.AsNoTracking()
                .Where(item => tenderIds.Contains(item.TenderId) && item.IsMandatory)
                .ToListAsync(cancellationToken);
            var requirementsByTender = requirements.GroupBy(item => item.TenderId)
                .ToDictionary(group => group.Key, group => group.Select(item => item.DocumentType)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase));

            return applications.Select(item =>
            {
                var required = requirementsByTender.GetValueOrDefault(item.TenderId)
                    ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var uploaded = item.Documents.Select(document => document.DocumentType)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                return new ContractorApplicationListItemDto(
                    item.TechnicalBidId,
                    item.TenderId,
                    item.Tender.TenderNumber,
                    item.Tender.Title,
                    item.ContractorId,
                    item.Contractor.Name,
                    GetReference(item),
                    item.Status,
                    item.SubmittedDate,
                    item.Status == "Draft" ? null : item.SubmittedDate,
                    item.Documents.Count,
                    required.Count(documentType => !uploaded.Contains(documentType)));
            }).ToList();
        }

        private async Task EnsureCanAccessAsync(
            TechnicalBid bid, int userId, bool contractor, bool tenderCell, CancellationToken cancellationToken)
        {
            if (contractor)
            {
                var user = await GetUserAsync(userId, cancellationToken);
                EnsureContractor(user);
                if (bid.Contractor.UserId != userId)
                    throw new NotFoundException(nameof(TechnicalBid), bid.TechnicalBidId);
                return;
            }

            var (internalUser, role) = await GetUserAndRoleAsync(userId, cancellationToken);
            if (tenderCell)
            {
                EnsureTenderCell(internalUser, role);
                var assigned = await _context.TenderWorkflowAssignments.AnyAsync(assignment =>
                    assignment.TenderId == bid.TenderId &&
                    assignment.IsCurrent &&
                    assignment.AssignedRoleId == role!.RoleId &&
                    (!assignment.AssignedUserId.HasValue || assignment.AssignedUserId == userId),
                    cancellationToken);
                if (!assigned)
                    throw new NotFoundException(nameof(TechnicalBid), bid.TechnicalBidId);
            }
            else
            {
                EnsureJuniorEngineer(internalUser, role);
            }
        }

        private async Task<List<TenderApplicationRequirementDto>> GetRequirementsForTenderAsync(
            int tenderId, CancellationToken cancellationToken) =>
            await _context.TenderRequiredBidDocuments.AsNoTracking()
                .Where(item => item.TenderId == tenderId)
                .OrderBy(item => item.DocumentType)
                .Select(item => new TenderApplicationRequirementDto(
                    item.DocumentType, item.Description, item.IsMandatory, item.AllowedExtensions, item.MaxFileSizeBytes))
                .ToListAsync(cancellationToken);

        private async Task<int> GetContractorIdAsync(int userId, CancellationToken cancellationToken)
        {
            var contractorId = await _context.Contractors.AsNoTracking()
                .Where(item => item.UserId == userId && item.IsActive)
                .Select(item => item.ContractorId)
                .FirstOrDefaultAsync(cancellationToken);
            return contractorId > 0
                ? contractorId
                : throw new ForbiddenActionException("Complete contractor registration before using applications.");
        }

        private async Task<User> GetUserAsync(int userId, CancellationToken cancellationToken) =>
            await _context.Users.FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken)
            ?? throw new ForbiddenActionException("The authenticated account is unavailable.");

        private async Task<(User User, Role? Role)> GetUserAndRoleAsync(
            int userId, CancellationToken cancellationToken)
        {
            var user = await GetUserAsync(userId, cancellationToken);
            var role = user.RoleId.HasValue
                ? await _context.Roles.FirstOrDefaultAsync(item => item.RoleId == user.RoleId.Value, cancellationToken)
                : null;
            return (user, role);
        }

        private static void EnsureContractor(User user)
        {
            if (!user.IsActive || user.AccountType != "ExternalBidder" || user.RoleId.HasValue)
                throw new ForbiddenActionException("Only an active contractor account can access contractor applications.");
        }

        private static void EnsureJuniorEngineer(User user, Role? role)
        {
            if (!user.IsActive || user.AccountType != "Internal" || role is null || !role.IsActive ||
                !RoleNames.IsJuniorEngineerRole(role.RoleCode, role.RoleName))
                throw new ForbiddenActionException("Only an active Junior Engineer can manage all contractor applications.");
        }

        private static void EnsureTenderCell(User user, Role? role)
        {
            if (!user.IsActive || user.AccountType != "Internal" || role is null || !role.IsActive ||
                !string.Equals(role.RoleCode, RoleNames.TenderCellCode, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(role.RoleName, RoleNames.TenderCell, StringComparison.OrdinalIgnoreCase))
                throw new ForbiddenActionException("Only an active Tender Cell user can review assigned applications.");
        }

        private static void EnsureApplicationWindow(Tender tender)
        {
            if (tender.CurrentStatus != TenderStatus.TenderPublished ||
                !TenderApplicationRules.CanApply(tender, DateTime.UtcNow))
                throw new BusinessRuleException(
                    $"Applications are not available: {TenderApplicationRules.GetApplicationStatus(tender, DateTime.UtcNow)}.");
        }

        private static ContractorApplicationHistory NewHistory(
            TechnicalBid bid, int userId, int? roleId, string eventType,
            string? previousStatus, string? newStatus, string? remarks, bool visibleToContractor) => new()
        {
            TechnicalBid = bid,
            EventType = eventType,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            Remarks = remarks,
            CreatedBy = userId,
            RoleId = roleId,
            VisibleToContractor = visibleToContractor,
            CreatedAtUtc = DateTime.UtcNow
        };

        private static string GetReference(TechnicalBid bid) =>
            $"TAPP-{bid.SubmittedDate:yyyy}-{bid.TechnicalBidId:D6}";

        private static HashSet<string> GetAllowedExtensions(string? value) =>
            (string.IsNullOrWhiteSpace(value)
                ? DocumentUploadRules.AllowedExtensions
                : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(item => item.StartsWith('.') ? item : $".{item}")
                    .ToHashSet(StringComparer.OrdinalIgnoreCase));

        private static bool AreAllowedExtensionsValid(string? value) =>
            string.IsNullOrWhiteSpace(value) || GetAllowedExtensions(value).All(DocumentUploadRules.AllowedExtensions.Contains);

        private static string? NormalizeExtensions(string? value) =>
            string.IsNullOrWhiteSpace(value)
                ? null
                : string.Join(",", GetAllowedExtensions(value).OrderBy(item => item, StringComparer.OrdinalIgnoreCase));

        private static string? NullIfWhiteSpace(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
            exception.InnerException is SqlException { Number: 2601 or 2627 };

        private async Task DeleteFilesAsync(IEnumerable<string> paths)
        {
            foreach (var path in paths)
                await _fileStorage.DeleteFileAsync(path);
        }
    }
}
