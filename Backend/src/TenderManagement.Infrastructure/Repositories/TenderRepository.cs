using Microsoft.EntityFrameworkCore;
using TenderManagement.Application.DTOs.Common;
using TenderManagement.Application.Interfaces;
using TenderManagement.Domain.Entities;
using TenderManagement.Domain.Enums;
using TenderManagement.Infrastructure.Data;

namespace TenderManagement.Infrastructure.Repositories
{
    public class TenderRepository : GenericRepository<Tender>, ITenderRepository
    {
        public TenderRepository(ApplicationDbContext context) : base(context) { }

        public async Task<(IEnumerable<Tender> Items, int TotalCount)> GetPagedAsync(
            PaginationQuery query,
            int? createdByUserId = null,
            IReadOnlyCollection<int>? allowedTenderIds = null)
        {
            var q = _dbSet.Include(t => t.Department).Include(t => t.CreatedByUser)
                .Include(t => t.ApprovalHistories).AsQueryable();

            if (createdByUserId.HasValue)
            {
                q = q.Where(t => t.CreatedBy == createdByUserId.Value);
            }

            if (allowedTenderIds is not null)
            {
                q = q.Where(t => allowedTenderIds.Contains(t.TenderId));
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                q = q.Where(t => t.Title.Contains(query.Search) || t.TenderNumber.Contains(query.Search));
            }

            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                q = q.Where(t => t.CurrentStatus == query.Status);
            }

            if (!string.IsNullOrWhiteSpace(query.StatusGroup))
            {
                q = query.StatusGroup.Trim().ToLowerInvariant() switch
                {
                    TenderStatusBuckets.ApprovedGroup => q.Where(t =>
                        t.CurrentStatus.Contains("Approved") ||
                        TenderStatusBuckets.ApprovedAdditionalStatuses.Contains(t.CurrentStatus)),
                    TenderStatusBuckets.PendingGroup => q.Where(t =>
                        t.CurrentStatus.Contains("Pending") ||
                        TenderStatusBuckets.PendingAdditionalStatuses.Contains(t.CurrentStatus)),
                    TenderStatusBuckets.RejectedGroup => q.Where(t =>
                        t.CurrentStatus == TenderStatus.Rejected),
                    TenderStatusBuckets.UnderReviewGroup => q.Where(t =>
                        TenderStatusBuckets.UnderReviewStatuses.Contains(t.CurrentStatus)),
                    TenderStatusBuckets.ClosedExpiredGroup => q.Where(t =>
                        t.CurrentStatus == TenderStatus.TenderPublished &&
                        t.SubmissionDeadline.HasValue &&
                        t.SubmissionDeadline.Value < DateTime.UtcNow),
                    _ => q
                };
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

        public async Task<(IEnumerable<Tender> Items, int TotalCount)> GetAssignedPendingPageAsync(
            PaginationQuery query,
            IReadOnlyCollection<string> pendingStatuses,
            int roleId,
            int userId,
            int? departmentId,
            bool isJuniorEngineer)
        {
            var assignedTenders =
                from tender in _dbSet.AsNoTracking()
                join assignment in _context.TenderWorkflowAssignments.AsNoTracking()
                    on tender.TenderId equals assignment.TenderId
                where tender.WorkflowVersion == 2 &&
                    pendingStatuses.Contains(tender.CurrentStatus) &&
                    assignment.IsCurrent &&
                    assignment.ShortfallId == null &&
                    assignment.AssignedRoleId == roleId &&
                    (!assignment.AssignedUserId.HasValue || assignment.AssignedUserId == userId) &&
                    (isJuniorEngineer || !departmentId.HasValue || assignment.DepartmentId == departmentId.Value)
                select tender;

            var pagedQuery = assignedTenders
                .Distinct()
                .Include(tender => tender.Department)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                pagedQuery = pagedQuery.Where(tender =>
                    tender.TenderNumber.Contains(query.Search) ||
                    tender.Title.Contains(query.Search));
            }

            var total = await pagedQuery.CountAsync();
            var items = await pagedQuery
                .OrderByDescending(tender => tender.CreatedDate)
                .ThenBy(tender => tender.TenderId)
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            return (items, total);
        }

        public async Task<List<Tender>> GetDashboardTendersAsync(
            int? createdByUserId,
            int limit,
            bool upcomingOnly,
            IReadOnlyCollection<int>? allowedTenderIds = null)
        {
            var query = _dbSet.Include(t => t.Department).Include(t => t.CreatedByUser)
                .Include(t => t.ApprovalHistories).AsNoTracking();

            if (allowedTenderIds is not null)
            {
                query = query.Where(t => allowedTenderIds.Contains(t.TenderId));
            }

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
