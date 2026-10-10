using TenderManagement.Application.Common;
using TenderManagement.Application.DTOs.Workflow;
using TenderManagement.Application.Interfaces;
using TenderManagement.Domain.Entities;

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

        public async Task<List<PendingApprovalDto>> GetPendingApprovalsForRoleAsync(int roleId)
        {
            var role = await _unitOfWork.Roles.GetByIdAsync(roleId);
            if (role is null) return new List<PendingApprovalDto>();

            // Statuses currently awaiting THIS role's approval, derived from the engine.
            var pendingStatuses = WorkflowEngine.Transitions
                .Where(kv => kv.Value.ApproveToStatus is not null && WorkflowEngine.IsRoleAllowed(kv.Value.ApproverRoles, role.RoleName))
                .Select(kv => kv.Key)
                .ToHashSet();

            var tenders = await _unitOfWork.Tenders.FindAsync(t => !t.IsDeleted && pendingStatuses.Contains(t.CurrentStatus));

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
            var allTenders = (await _unitOfWork.Tenders.FindAsync(t => !t.IsDeleted)).ToList();

            return new DashboardStatsDto
            {
                TotalTenders = allTenders.Count,
                PublishedTenders = allTenders.Count(t => t.CurrentStatus == Domain.Enums.TenderStatus.TenderPublished),
                PendingApprovals = allTenders.Count(t => t.CurrentStatus.Contains("Pending") || t.CurrentStatus.Contains("Evaluation") || t.CurrentStatus.Contains("Opinion")),
                CompletedTenders = allTenders.Count(t => t.CurrentStatus == Domain.Enums.TenderStatus.PaymentCompleted),
                MyTenders = allTenders.Count(t => t.CreatedBy == userId)
            };
        }
    }
}
