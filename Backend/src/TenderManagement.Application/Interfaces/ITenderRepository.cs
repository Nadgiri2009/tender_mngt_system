using TenderManagement.Application.DTOs.Common;
using TenderManagement.Domain.Entities;

namespace TenderManagement.Application.Interfaces
{
    public interface ITenderRepository : IGenericRepository<Tender>
    {
        Task<(IEnumerable<Tender> Items, int TotalCount)> GetPagedAsync(
            PaginationQuery query,
            int? createdByUserId = null,
            IReadOnlyCollection<int>? allowedTenderIds = null);
        Task<(IEnumerable<Tender> Items, int TotalCount)> GetAssignedPendingPageAsync(
            PaginationQuery query,
            IReadOnlyCollection<string> pendingStatuses,
            int roleId,
            int userId,
            int? departmentId,
            bool isJuniorEngineer);
        Task<List<Tender>> GetDashboardTendersAsync(
            int? createdByUserId,
            int limit,
            bool upcomingOnly,
            IReadOnlyCollection<int>? allowedTenderIds = null);
        Task<Tender?> GetWithDetailsAsync(int tenderId);
        Task<string> GenerateNextTenderNumberAsync();
    }
}
