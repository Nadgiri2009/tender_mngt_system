using Microsoft.EntityFrameworkCore;
using TenderManagement.Application.DTOs.Common;
using TenderManagement.Application.Interfaces;
using TenderManagement.Domain.Entities;
using TenderManagement.Infrastructure.Data;

namespace TenderManagement.Infrastructure.Repositories
{
    public class TenderRepository : GenericRepository<Tender>, ITenderRepository
    {
        public TenderRepository(ApplicationDbContext context) : base(context) { }

        public async Task<(IEnumerable<Tender> Items, int TotalCount)> GetPagedAsync(PaginationQuery query, int? createdByUserId = null)
        {
            var q = _dbSet.Include(t => t.Department).Include(t => t.CreatedByUser)
                .Include(t => t.ApprovalHistories).AsQueryable();

            if (createdByUserId.HasValue)
            {
                q = q.Where(t => t.CreatedBy == createdByUserId.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                q = q.Where(t => t.Title.Contains(query.Search) || t.TenderNumber.Contains(query.Search));
            }

            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                q = q.Where(t => t.CurrentStatus == query.Status);
            }

            if (query.DepartmentId.HasValue)
            {
                q = q.Where(t => t.DepartmentId == query.DepartmentId.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.TenderType))
            {
                q = q.Where(t => t.TenderType == query.TenderType);
            }

            if (query.SubmissionDateFrom.HasValue)
            {
                q = q.Where(t => t.SubmissionDeadline >= query.SubmissionDateFrom.Value);
            }

            if (query.SubmissionDateTo.HasValue)
            {
                var dateAfterEnd = query.SubmissionDateTo.Value.Date.AddDays(1);
                q = q.Where(t => t.SubmissionDeadline < dateAfterEnd);
            }

            q = (query.SortBy?.ToLower()) switch
            {
                "title" => query.SortDescending ? q.OrderByDescending(t => t.Title) : q.OrderBy(t => t.Title),
                "amount" => query.SortDescending ? q.OrderByDescending(t => t.EstimatedAmount) : q.OrderBy(t => t.EstimatedAmount),
                _ => query.SortDescending ? q.OrderByDescending(t => t.CreatedDate) : q.OrderBy(t => t.CreatedDate)
            };

            var total = await q.CountAsync();

            var items = await q
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            return (items, total);
        }

        public async Task<List<Tender>> GetDashboardTendersAsync(int? createdByUserId, int limit, bool upcomingOnly)
        {
            var query = _dbSet.Include(t => t.Department).Include(t => t.CreatedByUser)
                .Include(t => t.ApprovalHistories).AsNoTracking();

            if (createdByUserId.HasValue)
            {
                query = query.Where(t => t.CreatedBy == createdByUserId.Value);
            }

            if (upcomingOnly)
            {
                query = query.Where(t => t.SubmissionDeadline >= DateTime.UtcNow && t.CurrentStatus != "Draft")
                    .OrderBy(t => t.SubmissionDeadline);
            }
            else
            {
                query = query.OrderByDescending(t => t.CreatedDate);
            }

            return await query.Take(limit).ToListAsync();
        }

        public async Task<Tender?> GetWithDetailsAsync(int tenderId) =>
            await _dbSet
                .Include(t => t.Department)
                .Include(t => t.CreatedByUser)
                .Include(t => t.BOQItems)
                .Include(t => t.Documents)
                .Include(t => t.ApprovalHistories)
                .FirstOrDefaultAsync(t => t.TenderId == tenderId);

        public async Task<string> GenerateNextTenderNumberAsync()
        {
            var year = DateTime.UtcNow.Year;
            var countThisYear = await _dbSet.CountAsync(t => t.TenderNumber.StartsWith($"TND-{year}-"));
            return $"TND-{year}-{(countThisYear + 1):D5}";
        }
    }
}
