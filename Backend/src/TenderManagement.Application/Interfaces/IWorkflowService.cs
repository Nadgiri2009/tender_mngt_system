using TenderManagement.Application.Common;
using TenderManagement.Application.DTOs.Common;
using TenderManagement.Application.DTOs.Tender;
using TenderManagement.Application.DTOs.Workflow;

namespace TenderManagement.Application.Interfaces
{
    public interface IWorkflowService
    {
        Task<ApprovalHistoryDto> SubmitAsync(int tenderId, SubmitTenderDto dto, int currentUserId, int roleId);
        Task<ApprovalHistoryDto> ExecuteActionAsync(int tenderId, string actionName, string? remarks, int currentUserId, int roleId);
        Task<ApprovalHistoryDto> ApproveAsync(int tenderId, ApproveRejectDto dto, int currentUserId, int roleId);
        Task<ApprovalHistoryDto> RejectAsync(int tenderId, ApproveRejectDto dto, int currentUserId, int roleId);
        Task<List<ApprovalHistoryDto>> GetHistoryAsync(int tenderId);
        Task<PagedResult<PendingApprovalDto>> GetPendingApprovalsForRoleAsync(
            int roleId,
            int userId,
            PaginationQuery query);
        Task<List<WorkflowActionOptionDto>> GetAvailableActionsAsync(int tenderId, int userId, int roleId);
        Task<DashboardStatsDto> GetDashboardStatsAsync(int userId, int roleId);
        Task<List<WorkflowStepDto>> GetWorkflowStepsAsync();
    }
}
