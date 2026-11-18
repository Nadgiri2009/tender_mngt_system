using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenderManagement.API.Extensions;
using TenderManagement.Application.DTOs.Common;
using TenderManagement.Application.DTOs.Workflow;
using TenderManagement.Application.Interfaces;
using TenderManagement.Application.Services;
using TenderManagement.Domain.Enums;

namespace TenderManagement.API.Controllers
{
    [ApiController]
    [Route("api/tenders")]
    [Authorize]
    public class WorkflowController : ControllerBase
    {
        private readonly IWorkflowService _workflowService;

        public WorkflowController(IWorkflowService workflowService)
        {
            _workflowService = workflowService;
        }

        /// <summary>POST /api/tenders/{id}/submit</summary>
        [HttpPost("{id:int}/submit")]
        [Authorize(Roles = $"{RoleNames.JuniorEngineer},{RoleNames.TenderCell},{RoleNames.TenderCommitteeChairman},{RoleNames.DirectorAuditor}")]
        public async Task<IActionResult> Submit(int id, [FromBody] SubmitTenderDto dto)
        {
            var result = await _workflowService.SubmitAsync(id, dto, User.GetUserId(), GetRoleId());
            return Ok(result);
        }

        /// <summary>POST /api/tenders/{id}/actions/{actionName} - execute a configured action for the caller's role.</summary>
        [HttpPost("{id:int}/actions/{actionName}")]
        [Authorize(Roles = $"{RoleNames.JuniorEngineer},{RoleNames.TenderCell},{RoleNames.TenderCommitteeChairman},{RoleNames.DirectorAuditor}")]
        public async Task<IActionResult> ExecuteAction(
            int id, string actionName, [FromBody] WorkflowActionRequest request)
        {
            if (string.IsNullOrWhiteSpace(actionName) || actionName.Length > 50)
                return BadRequest(new { message = "A configured workflow action is required." });

            var result = await _workflowService.ExecuteActionAsync(
                id, actionName, request.Remarks, User.GetUserId(), GetRoleId());
            return Ok(result);
        }

        /// <summary>POST /api/tenders/{id}/approve</summary>
        [HttpPost("{id:int}/approve")]
        [Authorize(Roles = $"{RoleNames.JuniorEngineer},{RoleNames.TenderCell},{RoleNames.TenderCommitteeChairman},{RoleNames.DirectorAuditor}")]
        public async Task<IActionResult> Approve(int id, [FromBody] ApproveRejectDto dto)
        {
            var result = await _workflowService.ApproveAsync(id, dto, User.GetUserId(), GetRoleId());
            return Ok(result);
        }

        /// <summary>POST /api/tenders/{id}/reject</summary>
        [HttpPost("{id:int}/reject")]
        [Authorize(Roles = $"{RoleNames.JuniorEngineer},{RoleNames.TenderCell},{RoleNames.TenderCommitteeChairman},{RoleNames.DirectorAuditor}")]
        public async Task<IActionResult> Reject(int id, [FromBody] ApproveRejectDto dto)
        {
            var result = await _workflowService.RejectAsync(id, dto, User.GetUserId(), GetRoleId());
            return Ok(result);
        }

        /// <summary>GET /api/tenders/{id}/history</summary>
        [HttpGet("{id:int}/history")]
        [Authorize(Roles = $"{RoleNames.JuniorEngineer},{RoleNames.TenderCell},{RoleNames.TenderCommitteeChairman},{RoleNames.DirectorAuditor}")]
        public async Task<IActionResult> History(int id)
        {
            var result = await _workflowService.GetHistoryAsync(id);
            return Ok(result);
        }

        /// <summary>GET /api/tenders/{id}/actions - returns actions configured for the current role and tender state.</summary>
        [HttpGet("{id:int}/actions")]
        [Authorize(Roles = $"{RoleNames.JuniorEngineer},{RoleNames.TenderCell},{RoleNames.TenderCommitteeChairman},{RoleNames.DirectorAuditor}")]
        public async Task<IActionResult> GetAvailableActions(int id)
        {
            var result = await _workflowService.GetAvailableActionsAsync(
                id, User.GetUserId(), GetRoleId());
            return Ok(result);
        }

        private int GetRoleId()
        {
            var roleIdClaim = User.FindFirst("roleId")?.Value;
            return int.TryParse(roleIdClaim, out var roleId) ? roleId : 0;
        }

        /// <summary>GET /api/tenders/workflow-steps - ordered workflow states used by the admin progress bar</summary>
        [HttpGet("workflow-steps")]
        public async Task<IActionResult> GetWorkflowSteps()
        {
            return Ok(await _workflowService.GetWorkflowStepsAsync());
        }
    }

    [ApiController]
    [Route("api/approvals")]
    [Authorize]
    public class ApprovalsController : ControllerBase
    {
        private readonly IWorkflowService _workflowService;

        public ApprovalsController(IWorkflowService workflowService)
        {
            _workflowService = workflowService;
        }

        /// <summary>GET /api/approvals/pending - tenders awaiting the current user's role</summary>
        [HttpGet("pending")]
        [Authorize(Roles = $"{RoleNames.JuniorEngineer},{RoleNames.TenderCell},{RoleNames.TenderCommitteeChairman},{RoleNames.DirectorAuditor}")]
        public async Task<IActionResult> GetPending([FromQuery] PaginationQuery query)
        {
            var roleIdClaim = User.FindFirst("roleId")?.Value;
            if (!int.TryParse(roleIdClaim, out var roleId))
                return Forbid();

            var result = await _workflowService.GetPendingApprovalsForRoleAsync(
                roleId, User.GetUserId(), query);
            return Ok(result);
        }
    }

    [ApiController]
    [Route("api/dashboard")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IWorkflowService _workflowService;

        public DashboardController(IWorkflowService workflowService)
        {
            _workflowService = workflowService;
        }

        /// <summary>GET /api/dashboard/stats</summary>
        [HttpGet("stats")]
        [Authorize(Roles = $"{RoleNames.JuniorEngineer},{RoleNames.TenderCell},{RoleNames.TenderCommitteeChairman},{RoleNames.DirectorAuditor}")]
        public async Task<IActionResult> Stats()
        {
            var roleIdClaim = User.FindFirst("roleId")?.Value;
            var roleId = int.TryParse(roleIdClaim, out var rid) ? rid : 0;
            var result = await _workflowService.GetDashboardStatsAsync(User.GetUserId(), roleId);
            return Ok(result);
        }
    }
}
