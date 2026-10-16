using TenderManagement.Application.DTOs.Common;
using TenderManagement.Domain.Entities;

namespace TenderManagement.Application.Interfaces
{
    public interface ITenderRepository : IGenericRepository<Tender>
    {
        Task<(IEnumerable<Tender> Items, int TotalCount)> GetPagedAsync(PaginationQuery query, int? createdByUserId = null);
        Task<List<Tender>> GetDashboardTendersAsync(int? createdByUserId, int limit, bool upcomingOnly);
        Task<Tender?> GetWithDetailsAsync(int tenderId);
        Task<string> GenerateNextTenderNumberAsync();
    }
}
