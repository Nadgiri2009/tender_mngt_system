using TenderManagement.Application.Common;
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

        public async Task<ApprovalHistoryDto> SubmitAsync(int tenderId, SubmitTenderDto dto, int currentUserId, string roleName)
        {
            var tender = await _unitOfWork.Tenders.GetByIdAsync(tenderId)
                ?? throw new NotFoundException(nameof(Tender), tenderId);

            if (!WorkflowEngine.Transitions.TryGetValue(tender.CurrentStatus, out var transition) || transition.SubmitToStatus is null)
            {
                throw new BusinessRuleException($"No submit action is available from status '{tender.CurrentStatus}'.");
            }

            if (!WorkflowEngine.IsRoleAllowed(transition.SubmitRoles, roleName))
            {
                throw new ForbiddenActionException($"Role '{roleName}' is not permitted to submit a tender at status '{tender.CurrentStatus}'.");
            }

            await _unitOfWork.ExecuteWorkflowTransitionAsync(
                tenderId, transition.ApprovalStage, transition.SubmitToStatus, "Submitted", currentUserId, roleName, dto.Remarks);

            await _auditService.LogAsync(currentUserId, "Submit", nameof(Tender), tenderId, transition.SubmitToStatus);

            return new ApprovalHistoryDto
            {
                ApprovalStage = transition.ApprovalStage,
                PreviousStatus = tender.CurrentStatus,
                NewStatus = transition.SubmitToStatus,
                Status = "Submitted",
                RoleAtAction = roleName,
                Remarks = dto.Remarks,
                ApprovalDate = DateTime.UtcNow
            };
        }

        public async Task<ApprovalHistoryDto> ApproveAsync(int tenderId, ApproveRejectDto dto, int currentUserId, string roleName)
        {
            var tender = await _unitOfWork.Tenders.GetByIdAsync(tenderId)
                ?? throw new NotFoundException(nameof(Tender), tenderId);

            if (!WorkflowEngine.Transitions.TryGetValue(tender.CurrentStatus, out var transition) || transition.ApproveToStatus is null)
            {
                throw new BusinessRuleException($"No approval action is available from status '{tender.CurrentStatus}'.");
            }

            if (!WorkflowEngine.IsRoleAllowed(transition.ApproverRoles, roleName))
            {
                throw new ForbiddenActionException($"Role '{roleName}' is not authorized to approve at status '{tender.CurrentStatus}'.");
            }

            await _unitOfWork.ExecuteWorkflowTransitionAsync(
                tenderId, transition.ApprovalStage, transition.ApproveToStatus, "Approved", currentUserId, roleName, dto.Remarks);

            await _auditService.LogAsync(currentUserId, "Approve", nameof(Tender), tenderId, transition.ApproveToStatus);

            return new ApprovalHistoryDto
            {
                ApprovalStage = transition.ApprovalStage,
                PreviousStatus = tender.CurrentStatus,
                NewStatus = transition.ApproveToStatus,
                Status = "Approved",
                RoleAtAction = roleName,
                Remarks = dto.Remarks,
                ApprovalDate = DateTime.UtcNow
            };
        }

        public async Task<ApprovalHistoryDto> RejectAsync(int tenderId, ApproveRejectDto dto, int currentUserId, string roleName)
        {
            var tender = await _unitOfWork.Tenders.GetByIdAsync(tenderId)
                ?? throw new NotFoundException(nameof(Tender), tenderId);

            if (!WorkflowEngine.Transitions.TryGetValue(tender.CurrentStatus, out var transition) || transition.ApproveToStatus is null)
            {
                throw new BusinessRuleException($"No reject action is available from status '{tender.CurrentStatus}'.");
            }

            if (!WorkflowEngine.IsRoleAllowed(transition.ApproverRoles, roleName))
            {
                throw new ForbiddenActionException($"Role '{roleName}' is not authorized to reject at status '{tender.CurrentStatus}'.");
            }

            var rejectStatus = transition.RejectToStatus ?? Domain.Enums.TenderStatus.Rejected;

            await _unitOfWork.ExecuteWorkflowTransitionAsync(
                tenderId, transition.ApprovalStage, rejectStatus, "Rejected", currentUserId, roleName, dto.Remarks);

            await _auditService.LogAsync(currentUserId, "Reject", nameof(Tender), tenderId, rejectStatus);

            return new ApprovalHistoryDto
            {
                ApprovalStage = transition.ApprovalStage,
                PreviousStatus = tender.CurrentStatus,
                NewStatus = rejectStatus,
                Status = "Rejected",
                RoleAtAction = roleName,
                Remarks = dto.Remarks,
                ApprovalDate = DateTime.UtcNow
            };
        }

        public async Task<List<ApprovalHistoryDto>> GetHistoryAsync(int tenderId)
        {
            var history = await _unitOfWork.ApprovalHistories.FindAsync(a => a.TenderId == tenderId);

            var result = new List<ApprovalHistoryDto>();
            foreach (var h in history.OrderBy(h => h.ApprovalDate))
            {
                var user = await _unitOfWork.Users.GetByIdAsync(h.ApprovedBy);
                result.Add(new ApprovalHistoryDto
                {
                    ApprovalId = h.ApprovalId,
                    ApprovalStage = h.ApprovalStage,
                    PreviousStatus = h.PreviousStatus,
                    NewStatus = h.NewStatus,
                    Status = h.Status,
                    ActionTakenBy = user?.FullName ?? "Unknown",
                    RoleAtAction = h.RoleAtAction,
                    Remarks = h.Remarks,
                    ApprovalDate = h.ApprovalDate
                });
            }
            return result;
        }

        public async Task<List<PendingApprovalDto>> GetPendingApprovalsForRoleAsync(int roleId, int userId)
        {
            var role = await _unitOfWork.Roles.GetByIdAsync(roleId);
            if (role is null) throw new NotFoundException(nameof(Role), roleId);

            // Statuses currently awaiting THIS role's approval, derived from the engine.
            var pendingStatuses = WorkflowEngine.Transitions
                .Where(kv => kv.Value.ApproveToStatus is not null && WorkflowEngine.IsRoleAllowed(kv.Value.ApproverRoles, role.RoleName))
                .Select(kv => kv.Key)
                .ToHashSet();

            var tenders = await _unitOfWork.Tenders.FindAsync(t =>
                !t.IsDeleted &&
                pendingStatuses.Contains(t.CurrentStatus) &&
                (role.RoleName != RoleNames.DepartmentOfficer || t.CreatedBy == userId));

            return tenders.Select(t => new PendingApprovalDto
            {
                TenderId = t.TenderId,
                TenderNumber = t.TenderNumber,
                Title = t.Title,
                CurrentStatus = t.CurrentStatus,
                EstimatedAmount = t.EstimatedAmount,
                DepartmentName = t.Department?.DepartmentName ?? string.Empty,
                CreatedDate = t.CreatedDate
            }).OrderByDescending(t => t.CreatedDate).ToList();
        }

        public async Task<DashboardStatsDto> GetDashboardStatsAsync(int userId, int roleId)
        {
            var role = await _unitOfWork.Roles.GetByIdAsync(roleId);
            if (role is null) throw new NotFoundException(nameof(Role), roleId);
            var createdByUserId = role.RoleName == RoleNames.DepartmentOfficer ? userId : (int?)null;
            var allTenders = (await _unitOfWork.Tenders.FindAsync(t =>
                !t.IsDeleted && (!createdByUserId.HasValue || t.CreatedBy == createdByUserId.Value))).ToList();
            var recentTenders = await _unitOfWork.Tenders.GetDashboardTendersAsync(createdByUserId, 5, upcomingOnly: false);
            var upcomingDeadlines = await _unitOfWork.Tenders.GetDashboardTendersAsync(createdByUserId, 5, upcomingOnly: true);
            var now = DateTime.UtcNow;

            return new DashboardStatsDto
            {
                TotalTenders = allTenders.Count,
                ApprovedTenders = allTenders.Count(t => TenderStatusBuckets.IsApproved(t.CurrentStatus)),
                PendingTenders = allTenders.Count(t => TenderStatusBuckets.IsPending(t.CurrentStatus)),
                RejectedTenders = allTenders.Count(t => TenderStatusBuckets.IsRejected(t.CurrentStatus)),
                UnderReviewTenders = allTenders.Count(t => TenderStatusBuckets.IsUnderReview(t.CurrentStatus)),
                PublishedTenders = allTenders.Count(t => t.CurrentStatus == TenderStatus.TenderPublished),
                DraftTenders = allTenders.Count(t => t.CurrentStatus == TenderStatus.Draft),
                ClosedExpiredTenders = allTenders.Count(t =>
                    t.CurrentStatus == TenderStatus.TenderPublished &&
                    t.SubmissionDeadline.HasValue &&
                    t.SubmissionDeadline.Value < now),
                PendingApprovals = allTenders.Count(t => TenderStatusBuckets.IsPending(t.CurrentStatus) || TenderStatusBuckets.IsUnderReview(t.CurrentStatus)),
                CompletedTenders = allTenders.Count(t => t.CurrentStatus == TenderStatus.PaymentCompleted),
                MyTenders = allTenders.Count(t => t.CreatedBy == userId),
                CategoryBreakdown = allTenders
                    .GroupBy(t => t.TenderType)
                    .Select(g => new DashboardCategoryCountDto { Category = g.Key, TenderCount = g.Count() })
                    .OrderByDescending(g => g.TenderCount)
                    .ThenBy(g => g.Category)
                    .ToList(),
                RecentTenders = recentTenders.Select(MapDashboardTender).ToList(),
                UpcomingDeadlines = upcomingDeadlines.Select(MapDashboardTender).ToList()
            };
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
