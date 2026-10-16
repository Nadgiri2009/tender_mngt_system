using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenderManagement.Application.DTOs.Common;
using TenderManagement.Application.DTOs.Tender;
using TenderManagement.Application.Interfaces;
using TenderManagement.Application.Services;
using TenderManagement.Domain.Entities;
using TenderManagement.Domain.Enums;

namespace TenderManagement.API.Controllers
{
    /// <summary>
    /// Aggregate counts used by the public homepage and the admin dashboard, so neither
    /// screen ever needs to hardcode numbers.
    /// </summary>
    [ApiController]
    [Route("api/dashboard")]
    public class PortalDashboardController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenderService _tenderService;

        public PortalDashboardController(IUnitOfWork unitOfWork, ITenderService tenderService)
        {
            _unitOfWork = unitOfWork;
            _tenderService = tenderService;
        }

        /// <summary>GET /api/dashboard/public-stats - safe aggregate counts for the homepage, no auth required</summary>
        [HttpGet("public-stats")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPublicStats()
        {
            var allTenders = await _unitOfWork.Tenders.GetAllAsync();
            var departments = await _unitOfWork.Departments.FindAsync(d => d.IsActive);
            var contractors = await _unitOfWork.Contractors.GetAllAsync();

            var activeStatuses = new[] { TenderStatus.TenderPublished, TenderStatus.TechnicalBidOpening, TenderStatus.TechnicalEvaluation, TenderStatus.FinancialBidOpening, TenderStatus.CAFORateOpinion, TenderStatus.FinancialEvaluation, TenderStatus.MCAOpinion };
            var completedStatuses = new[] { TenderStatus.PaymentCompleted, TenderStatus.AgreementCompleted };

            return Ok(new
            {
                activeTenders = allTenders.Count(t => activeStatuses.Contains(t.CurrentStatus)),
                publishedTenders = allTenders.Count(t => t.CurrentStatus == TenderStatus.TenderPublished),
                closedExpiredTenders = allTenders.Count(t =>
                    t.CurrentStatus == TenderStatus.TenderPublished &&
                    t.SubmissionDeadline.HasValue &&
                    t.SubmissionDeadline.Value < DateTime.UtcNow),
                registeredContractors = contractors.Count(),
                departments = departments.Count(),
                completedProjects = allTenders.Count(t => completedStatuses.Contains(t.CurrentStatus)),
            });
        }

        /// <summary>GET /api/dashboard/public-tenders - only published tender notices, no authentication required</summary>
        [HttpGet("public-tenders")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPublicTenders([FromQuery] PaginationQuery query)
        {
            query.PageNumber = Math.Max(1, query.PageNumber);
            query.PageSize = Math.Clamp(query.PageSize, 1, 20);
            query.Status = TenderStatus.TenderPublished;
            query.SortBy = "createdDate";
            query.SortDescending = true;

            var result = await _tenderService.GetTendersAsync(query);
            return Ok(new TenderManagement.Application.Common.PagedResult<TenderListItemDto>
            {
                Items = result.Items.Select(tender => new TenderListItemDto
                {
                    TenderId = tender.TenderId,
                    TenderNumber = tender.TenderNumber,
                    Title = tender.Title,
                    DepartmentName = tender.DepartmentName,
                    TenderType = tender.TenderType,
                    EstimatedAmount = tender.EstimatedAmount,
                    CurrentStatus = tender.CurrentStatus,
                    PublicationDate = tender.PublicationDate,
                    SubmissionStartDate = tender.SubmissionStartDate,
                    SubmissionDeadline = tender.SubmissionDeadline,
                    CreatedDate = tender.CreatedDate,
                    CanApply = tender.CanApply,
                    ApplicationStatus = tender.ApplicationStatus
                }),
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize
            });
        }

        /// <summary>GET /api/dashboard/public-departments - active department names for the public portal</summary>
        [HttpGet("public-departments")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPublicDepartments()
        {
            var departments = await _unitOfWork.Departments.FindAsync(d => d.IsActive);
            return Ok(departments
                .OrderBy(d => d.DepartmentName)
                .Select(d => new { d.DepartmentId, d.DepartmentName }));
        }

        /// <summary>GET /api/dashboard/admin-stats - Admin only, breakdown by status</summary>
        [HttpGet("admin-stats")]
        [Authorize(Roles = RoleNames.Admin)]
        public async Task<IActionResult> GetAdminStats()
        {
            var allTenders = await _unitOfWork.Tenders.GetAllAsync();
            var departments = await _unitOfWork.Departments.GetAllAsync();
            var contractors = await _unitOfWork.Contractors.GetAllAsync();
            var users = await _unitOfWork.Users.GetAllAsync();

            var statusBreakdown = allTenders
                .GroupBy(t => t.CurrentStatus)
                .Select(g => new { status = g.Key, count = g.Count() })
                .OrderByDescending(g => g.count);
            var recentTenders = await _unitOfWork.Tenders.GetDashboardTendersAsync(null, 5, upcomingOnly: false);
            var upcomingDeadlines = await _unitOfWork.Tenders.GetDashboardTendersAsync(null, 5, upcomingOnly: true);

            return Ok(new
            {
                totalTenders = allTenders.Count(),
                draftTenders = allTenders.Count(t => t.CurrentStatus == TenderStatus.Draft),
                publishedTenders = allTenders.Count(t => t.CurrentStatus == TenderStatus.TenderPublished),
                approvedTenders = allTenders.Count(t => TenderStatusBuckets.IsApproved(t.CurrentStatus)),
                pendingTenders = allTenders.Count(t => TenderStatusBuckets.IsPending(t.CurrentStatus)),
                rejectedTenders = allTenders.Count(t => TenderStatusBuckets.IsRejected(t.CurrentStatus)),
                underReviewTenders = allTenders.Count(t => TenderStatusBuckets.IsUnderReview(t.CurrentStatus)),
                categoryBreakdown = allTenders
                    .GroupBy(t => t.TenderType)
                    .Select(g => new { category = g.Key, tenderCount = g.Count() })
                    .OrderByDescending(g => g.tenderCount)
                    .ThenBy(g => g.category),
                totalDepartments = departments.Count(),
                totalContractors = contractors.Count(),
                totalUsers = users.Count(),
                statusBreakdown,
                recentTenders = recentTenders.Select(MapDashboardTender),
                upcomingDeadlines = upcomingDeadlines.Select(MapDashboardTender),
            });
        }

        private static object MapDashboardTender(Tender tender) => new
        {
            tender.TenderId,
            tender.TenderNumber,
            tender.TrackingId,
            tender.Title,
            departmentName = tender.Department?.DepartmentName ?? string.Empty,
            tender.TenderType,
            tender.EstimatedAmount,
            tender.CurrentStatus,
            publicationDate = tender.ApprovalHistories
                .Where(history => history.NewStatus == TenderStatus.TenderPublished)
                .Select(history => (DateTime?)history.ApprovalDate)
                .Min(),
            tender.SubmissionStartDate,
            tender.SubmissionDeadline,
            assignedRespOfficer = TenderAssignmentMapper.GetAssignedRespOfficer(tender),
            assignedRole = TenderAssignmentMapper.GetAssignedRole(tender.CurrentStatus),
            canApply = TenderApplicationRules.CanApply(tender, DateTime.UtcNow),
            applicationStatus = TenderApplicationRules.GetApplicationStatus(tender, DateTime.UtcNow),
            tender.CreatedDate
        };
    }
}
