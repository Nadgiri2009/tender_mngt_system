using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenderManagement.Application.Interfaces;
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

        public PortalDashboardController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>GET /api/dashboard/public-stats - safe aggregate counts for the homepage, no auth required</summary>
        [HttpGet("public-stats")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPublicStats()
        {
            var allTenders = await _unitOfWork.Tenders.GetAllAsync();
            var departments = await _unitOfWork.Departments.GetAllAsync();
            var contractors = await _unitOfWork.Contractors.GetAllAsync();

            var activeStatuses = new[] { TenderStatus.TenderPublished, TenderStatus.TechnicalBidOpening, TenderStatus.TechnicalEvaluation, TenderStatus.FinancialBidOpening, TenderStatus.CAFORateOpinion, TenderStatus.FinancialEvaluation, TenderStatus.MCAOpinion };
            var completedStatuses = new[] { TenderStatus.PaymentCompleted, TenderStatus.AgreementCompleted };

            return Ok(new
            {
                activeTenders = allTenders.Count(t => activeStatuses.Contains(t.CurrentStatus)),
                publishedTenders = allTenders.Count(t => t.CurrentStatus != TenderStatus.Draft),
                registeredContractors = contractors.Count(),
                departments = departments.Count(),
                completedProjects = allTenders.Count(t => completedStatuses.Contains(t.CurrentStatus)),
            });
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

            return Ok(new
            {
                totalTenders = allTenders.Count(),
                draftTenders = allTenders.Count(t => t.CurrentStatus == TenderStatus.Draft),
                publishedTenders = allTenders.Count(t => t.CurrentStatus != TenderStatus.Draft),
                totalDepartments = departments.Count(),
                totalContractors = contractors.Count(),
                totalUsers = users.Count(),
                statusBreakdown,
            });
        }
    }
}
