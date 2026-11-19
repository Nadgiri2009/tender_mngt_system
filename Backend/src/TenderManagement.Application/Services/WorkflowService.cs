using TenderManagement.Application.Common;
using TenderManagement.Application.DTOs.Common;
using TenderManagement.Application.DTOs.Tender;
using TenderManagement.Application.DTOs.Workflow;
using TenderManagement.Application.Interfaces;
using TenderManagement.Domain.Entities;
using TenderManagement.Domain.Enums;

namespace TenderManagement.Application.Services
{
    public class WorkflowService : IWorkflowService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;

        public WorkflowService(IUnitOfWork unitOfWork, IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
        }

        public Task<ApprovalHistoryDto> SubmitAsync(
            int tenderId, SubmitTenderDto dto, int currentUserId, int roleId) =>
            ExecuteConfiguredTransitionAsync(tenderId, "Submit", null,
                currentUserId, roleId, dto.Remarks);

        public Task<ApprovalHistoryDto> ApproveAsync(
            int tenderId, ApproveRejectDto dto, int currentUserId, int roleId) =>
            ExecuteConfiguredTransitionAsync(tenderId, "Approve", null,
                currentUserId, roleId, dto.Remarks);

        public Task<ApprovalHistoryDto> RejectAsync(
            int tenderId, ApproveRejectDto dto, int currentUserId, int roleId) =>
            ExecuteConfiguredTransitionAsync(tenderId, "Reject", null,
                currentUserId, roleId, dto.Remarks);

        public Task<ApprovalHistoryDto> ExecuteActionAsync(
            int tenderId, string actionName, string? remarks, int currentUserId, int roleId) =>
            ExecuteConfiguredTransitionAsync(tenderId, actionName, null,
                currentUserId, roleId, remarks);

        private async Task<ApprovalHistoryDto> ExecuteConfiguredTransitionAsync(
            int tenderId,
            string actionName,
            string? transitionCode,
            int currentUserId,
            int roleId,
            string? remarks)
        {
            var tender = await _unitOfWork.Tenders.GetByIdAsync(tenderId)
                ?? throw new NotFoundException(nameof(Tender), tenderId);
            var user = await _unitOfWork.Users.GetByIdAsync(currentUserId)
                ?? throw new ForbiddenActionException("The authenticated account is unavailable.");
            var role = user.AccountType == "Internal" && user.RoleId == roleId
                ? await _unitOfWork.Roles.GetByIdAsync(roleId)
                : null;
            if (role is null || !role.IsActive)
                throw new ForbiddenActionException("The authenticated role is not permitted to act on this workflow.");
            if (RoleNames.IsJuniorEngineerRole(role.RoleCode, role.RoleName) &&
                (!user.DepartmentId.HasValue || tender.DepartmentId != user.DepartmentId.Value))
            {
                throw new ForbiddenActionException("Your account is not assigned to this tender's department.");
            }

            var assignments = await _unitOfWork.TenderWorkflowAssignments.FindAsync(assignment =>
                assignment.TenderId == tenderId &&
                assignment.ShortfallId == null &&
                assignment.IsCurrent &&
                assignment.AssignedRoleId == roleId &&
                assignment.DepartmentId == tender.DepartmentId &&
                (!assignment.AssignedUserId.HasValue || assignment.AssignedUserId == currentUserId) &&
                (!user.DepartmentId.HasValue || assignment.DepartmentId == user.DepartmentId.Value));
            if (assignments.Count() != 1)
            {
                throw new ForbiddenActionException("The tender is not assigned to your workflow role or department.");
            }

            var matches = await _unitOfWork.WorkflowTransitions.FindAsync(transition =>
                transition.IsActive &&
                transition.WorkflowVersion == 2 &&
                transition.FromStatus == tender.CurrentStatus &&
                transition.ActionName == actionName &&
                transition.RoleId == roleId &&
                (transitionCode == null || transition.TransitionCode == transitionCode));
            var configuredTransition = matches.SingleOrDefault();
            if (configuredTransition is null)
                throw new ForbiddenActionException(
                    $"No active '{actionName}' transition is configured for the current status and role.");

            var stage = await _unitOfWork.WorkflowStages.GetByIdAsync(configuredTransition.StageId)
                ?? throw new InvalidOperationException("The configured workflow stage no longer exists.");

            if (string.Equals(actionName, "EnterFinancialData", StringComparison.OrdinalIgnoreCase))
            {
                if (!RoleNames.IsJuniorEngineerRole(role.RoleCode, role.RoleName))
                    throw new ForbiddenActionException("Only an active Junior Engineer can enter financial bid data.");
                if (tender.CurrentStatus != TenderStatus.FinancialBidOpeningApproved)
                    throw new BusinessRuleException("Financial bid data entry is not available at the current tender stage.");
                var financialBids = await _unitOfWork.FinancialBids.FindAsync(
                    bid => bid.TenderId == tenderId);
                if (!financialBids.Any())
                    throw new BusinessRuleException("Enter at least one financial bid amount before advancing the workflow.");
            }

            await _unitOfWork.ExecuteWorkflowTransitionAsync(
                tenderId,
                configuredTransition.TransitionCode,
                tender.CurrentStatus,
                currentUserId,
                roleId,
                remarks);

            await _auditService.LogAsync(currentUserId, actionName, nameof(Tender), tenderId,
                configuredTransition.ToStatus);

            return new ApprovalHistoryDto
            {
                ApprovalStage = stage.StageName,
                PreviousStatus = tender.CurrentStatus,
                NewStatus = configuredTransition.ToStatus,
                Status = configuredTransition.ActionStatus,
                RoleAtAction = role.RoleName,
                Remarks = remarks,
                ApprovalDate = DateTime.UtcNow
            };
        }

        public async Task<List<ApprovalHistoryDto>> GetHistoryAsync(int tenderId)
        {
            var history = await _unitOfWork.ApprovalHistories.FindAsync(a => a.TenderId == tenderId);
            var result = new List<ApprovalHistoryDto>();
            foreach (var item in history.OrderBy(item => item.ApprovalDate))
            {
                var user = await _unitOfWork.Users.GetByIdAsync(item.ApprovedBy);
                result.Add(new ApprovalHistoryDto
                {
                    ApprovalId = item.ApprovalId,
                    ApprovalStage = item.ApprovalStage,
                    PreviousStatus = item.PreviousStatus,
                    NewStatus = item.NewStatus,
                    Status = item.Status,
                    ActionTakenBy = user?.FullName ?? "Unknown",
                    RoleAtAction = item.RoleAtAction,
                    Remarks = item.Remarks,
                    ApprovalDate = item.ApprovalDate
                });
            }
            return result;
        }

        public async Task<PagedResult<PendingApprovalDto>> GetPendingApprovalsForRoleAsync(
            int roleId,
            int userId,
            PaginationQuery query)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId)
                ?? throw new ForbiddenActionException("The authenticated account is unavailable.");
            var role = await _unitOfWork.Roles.GetByIdAsync(roleId)
                ?? throw new NotFoundException(nameof(Role), roleId);
            if (!user.IsActive || user.AccountType != "Internal" || user.RoleId != roleId || !role.IsActive)
                throw new ForbiddenActionException("The authenticated role is not permitted to view this approval queue.");

            var roleTransitions = await GetPermittedTransitionsAsync(roleId);
            var pendingStatuses = roleTransitions.Select(transition => transition.FromStatus).ToHashSet();
            query.PageNumber = Math.Max(1, query.PageNumber);
            query.PageSize = Math.Clamp(query.PageSize, 1, 100);
            if (pendingStatuses.Count == 0)
            {
                return new PagedResult<PendingApprovalDto>
                {
                    Items = Array.Empty<PendingApprovalDto>(),
                    TotalCount = 0,
                    PageNumber = query.PageNumber,
                    PageSize = query.PageSize
                };
            }

            var (tenders, total) = await _unitOfWork.Tenders.GetAssignedPendingPageAsync(
                query,
                pendingStatuses.ToArray(),
                roleId,
                userId,
                user.DepartmentId,
                RoleNames.IsJuniorEngineerRole(role.RoleCode, role.RoleName));

            var items = tenders.Select(tender => new PendingApprovalDto
            {
                TenderId = tender.TenderId,
                TenderNumber = tender.TenderNumber,
                Title = tender.Title,
                CurrentStatus = tender.CurrentStatus,
                EstimatedAmount = tender.EstimatedAmount,
                DepartmentName = tender.Department?.DepartmentName ?? string.Empty,
                CreatedDate = tender.CreatedDate,
                AvailableActions = roleTransitions
                    .Where(transition => transition.FromStatus == tender.CurrentStatus)
                    .Select(transition => transition.ActionName)
                    .Distinct()
                    .OrderBy(action => action)
                    .ToList()
            }).ToArray();

            return new PagedResult<PendingApprovalDto>
            {
                Items = items,
                TotalCount = total,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize
            };
        }

        public async Task<DashboardStatsDto> GetDashboardStatsAsync(int userId, int roleId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId)
                ?? throw new ForbiddenActionException("The authenticated account is unavailable.");
            var role = await _unitOfWork.Roles.GetByIdAsync(roleId)
                ?? throw new NotFoundException(nameof(Role), roleId);
            if (!user.IsActive || user.AccountType != "Internal" ||
                user.RoleId != roleId || !role.IsActive)
            {
                throw new ForbiddenActionException("The authenticated role is not permitted to view this dashboard.");
            }

            var isJuniorEngineer = RoleNames.IsJuniorEngineerRole(role.RoleCode, role.RoleName);
            IReadOnlyCollection<int>? allowedTenderIds = null;
            if (!isJuniorEngineer)
            {
                var assignments = await _unitOfWork.TenderWorkflowAssignments.FindAsync(assignment =>
                    assignment.IsCurrent &&
                    assignment.ShortfallId == null &&
                    assignment.AssignedRoleId == roleId &&
                    (!assignment.AssignedUserId.HasValue || assignment.AssignedUserId == userId) &&
                    (!user.DepartmentId.HasValue || assignment.DepartmentId == user.DepartmentId.Value));
                allowedTenderIds = assignments.Select(assignment => assignment.TenderId).Distinct().ToArray();
            }

            var allTenders = allowedTenderIds is null
                ? (await _unitOfWork.Tenders.FindAsync(tender => !tender.IsDeleted)).ToList()
                : (await _unitOfWork.Tenders.FindAsync(tender =>
                    !tender.IsDeleted && allowedTenderIds.Contains(tender.TenderId))).ToList();
            var recentTenders = await _unitOfWork.Tenders.GetDashboardTendersAsync(
                null, 5, upcomingOnly: false, allowedTenderIds: allowedTenderIds);
            var upcomingDeadlines = await _unitOfWork.Tenders.GetDashboardTendersAsync(
                null, 5, upcomingOnly: true, allowedTenderIds: allowedTenderIds);

            return new DashboardStatsDto
            {
                TotalTenders = allTenders.Count,
                ApprovedTenders = allTenders.Count(tender => TenderStatusBuckets.IsApproved(tender.CurrentStatus)),
                PendingTenders = allTenders.Count(tender => TenderStatusBuckets.IsPending(tender.CurrentStatus)),
                RejectedTenders = allTenders.Count(tender => TenderStatusBuckets.IsRejected(tender.CurrentStatus)),
                UnderReviewTenders = allTenders.Count(tender => TenderStatusBuckets.IsUnderReview(tender.CurrentStatus)),
                PublishedTenders = allTenders.Count(tender => tender.CurrentStatus == TenderStatus.TenderPublished),
                DraftTenders = allTenders.Count(tender => tender.CurrentStatus == TenderStatus.Draft),
                ClosedExpiredTenders = allTenders.Count(tender =>
                    tender.CurrentStatus == TenderStatus.TenderPublished &&
                    tender.SubmissionDeadline.HasValue &&
                    tender.SubmissionDeadline.Value < DateTime.UtcNow),
                PendingApprovals = allTenders.Count(tender =>
                    TenderStatusBuckets.IsPending(tender.CurrentStatus) ||
                    TenderStatusBuckets.IsUnderReview(tender.CurrentStatus)),
                CompletedTenders = allTenders.Count(tender => tender.CurrentStatus == TenderStatus.TenderAgreementGenerated),
                MyTenders = allTenders.Count(tender => tender.CreatedBy == userId),
                CategoryBreakdown = allTenders
                    .GroupBy(tender => tender.TenderType)
                    .Select(group => new DashboardCategoryCountDto
                    {
                        Category = group.Key,
                        TenderCount = group.Count()
                    })
                    .OrderByDescending(group => group.TenderCount)
                    .ThenBy(group => group.Category)
                    .ToList(),
                RecentTenders = recentTenders.Select(MapDashboardTender).ToList(),
                UpcomingDeadlines = upcomingDeadlines.Select(MapDashboardTender).ToList()
            };
        }

        public async Task<List<WorkflowStepDto>> GetWorkflowStepsAsync()
        {
            var transitions = (await _unitOfWork.WorkflowTransitions.FindAsync(
                transition => transition.IsActive && transition.WorkflowVersion == 2))
                .OrderBy(transition => transition.StageId)
                .ToList();
            var roles = (await _unitOfWork.Roles.GetAllAsync()).ToDictionary(role => role.RoleId);
            var steps = new List<WorkflowStepDto>();

            foreach (var transition in transitions)
            {
                var stage = await _unitOfWork.WorkflowStages.GetByIdAsync(transition.StageId);
                steps.Add(new WorkflowStepDto
                {
                    Status = transition.FromStatus,
                    StageName = stage?.StageName ?? transition.TransitionCode,
                    AssigneeRole = roles.TryGetValue(transition.RoleId, out var role) ? role.RoleName : null,
                    IsCurrent = false
                });
            }

            return steps;
        }

        public async Task<List<WorkflowActionOptionDto>> GetAvailableActionsAsync(
            int tenderId, int userId, int roleId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId)
                ?? throw new ForbiddenActionException("The authenticated account is unavailable.");
            var role = user.AccountType == "Internal" && user.RoleId == roleId
                ? await _unitOfWork.Roles.GetByIdAsync(roleId)
                : null;
            if (role is null || !role.IsActive)
                throw new ForbiddenActionException("The authenticated role is not permitted to act on this workflow.");

            var tender = await _unitOfWork.Tenders.GetByIdAsync(tenderId)
                ?? throw new NotFoundException(nameof(Tender), tenderId);
            var isJuniorEngineer = RoleNames.IsJuniorEngineerRole(role.RoleCode, role.RoleName);
            if (isJuniorEngineer &&
                (!user.DepartmentId.HasValue || tender.DepartmentId != user.DepartmentId.Value))
            {
                return new List<WorkflowActionOptionDto>();
            }

            var assignments = await _unitOfWork.TenderWorkflowAssignments.FindAsync(assignment =>
                assignment.TenderId == tenderId &&
                assignment.ShortfallId == null &&
                assignment.IsCurrent &&
                assignment.AssignedRoleId == roleId &&
                (!assignment.AssignedUserId.HasValue || assignment.AssignedUserId == userId) &&
                (!user.DepartmentId.HasValue || assignment.DepartmentId == user.DepartmentId.Value));
            if (tender.WorkflowVersion != 2 || assignments.Count() != 1)
                return new List<WorkflowActionOptionDto>();

            var permittedTransitionIds = (await GetPermittedTransitionsAsync(roleId))
                .Select(transition => transition.TransitionId)
                .ToHashSet();
            var transitions = await _unitOfWork.WorkflowTransitions.FindAsync(transition =>
                transition.IsActive && transition.WorkflowVersion == 2 &&
                transition.FromStatus == tender.CurrentStatus &&
                transition.RoleId == roleId &&
                permittedTransitionIds.Contains(transition.TransitionId));
            var result = new List<WorkflowActionOptionDto>();

            foreach (var transition in transitions)
            {
                var stage = await _unitOfWork.WorkflowStages.GetByIdAsync(transition.StageId);
                if (stage is not null && stage.IsActive)
                {
                    result.Add(new WorkflowActionOptionDto(
                        transition.ActionName,
                        stage.StageName,
                        transition.FromStatus,
                        transition.ToStatus));
                }
            }

            return result.OrderBy(action => action.ActionName).ToList();
        }

        private async Task<List<WorkflowTransition>> GetPermittedTransitionsAsync(int roleId)
        {
            var transitions = (await _unitOfWork.WorkflowTransitions.FindAsync(transition =>
                transition.IsActive && transition.WorkflowVersion == 2 && transition.RoleId == roleId)).ToList();
            if (transitions.Count == 0)
                return transitions;

            var roleGrants = await _unitOfWork.RolePermissions.FindAsync(grant => grant.RoleId == roleId);
            var permissionIds = roleGrants.Select(grant => grant.PermissionId).ToHashSet();
            var permissionKeys = (await _unitOfWork.Permissions.FindAsync(permission =>
                permission.IsActive && permissionIds.Contains(permission.PermissionId)))
                .Select(permission => permission.PermissionKey)
                .ToHashSet(StringComparer.Ordinal);

            return transitions.Where(transition => permissionKeys.Contains(transition.PermissionKey)).ToList();
        }

        private static TenderListItemDto MapDashboardTender(Tender tender) => new()
        {
            TenderId = tender.TenderId,
            TenderNumber = tender.TenderNumber,
            TrackingId = tender.TrackingId,
            Title = tender.Title,
            DepartmentName = tender.Department?.DepartmentName ?? string.Empty,
            TenderType = tender.TenderType,
            EstimatedAmount = tender.EstimatedAmount,
            CurrentStatus = tender.CurrentStatus,
            PublicationDate = tender.ApprovalHistories
                .Where(history => history.NewStatus == TenderStatus.TenderPublished)
                .Select(history => (DateTime?)history.ApprovalDate)
                .Min(),
            SubmissionStartDate = tender.SubmissionStartDate,
            SubmissionDeadline = tender.SubmissionDeadline,
            AssignedRespOfficer = TenderAssignmentMapper.GetAssignedRespOfficer(tender),
            AssignedRole = TenderAssignmentMapper.GetAssignedRole(tender.CurrentStatus),
            CreatedDate = tender.CreatedDate,
            CanApply = TenderApplicationRules.CanApply(tender, DateTime.UtcNow),
            ApplicationStatus = TenderApplicationRules.GetApplicationStatus(tender, DateTime.UtcNow)
        };
    }
}
