using TenderManagement.Application.DTOs.Workflow;

namespace TenderManagement.Application.Interfaces
{
    public interface IWorkflowService
    {
        Task<ApprovalHistoryDto> SubmitAsync(int tenderId, SubmitTenderDto dto, int currentUserId, string roleName);
        Task<ApprovalHistoryDto> ApproveAsync(int tenderId, ApproveRejectDto dto, int currentUserId, string roleName);
        Task<ApprovalHistoryDto> RejectAsync(int tenderId, ApproveRejectDto dto, int currentUserId, string roleName);
        Task<List<ApprovalHistoryDto>> GetHistoryAsync(int tenderId);
        Task<List<PendingApprovalDto>> GetPendingApprovalsForRoleAsync(int roleId);
        Task<DashboardStatsDto> GetDashboardStatsAsync(int userId, int roleId);
    }
}
