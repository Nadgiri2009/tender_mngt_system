using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenderManagement.API.Extensions;
using TenderManagement.Application.DTOs.Workflow;
using TenderManagement.Application.Interfaces;
using TenderManagement.Application.Services;

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
        public async Task<IActionResult> Submit(int id, [FromBody] SubmitTenderDto dto)
        {
            var result = await _workflowService.SubmitAsync(id, dto, User.GetUserId(), User.GetRoleName());
            return Ok(result);
        }

        /// <summary>POST /api/tenders/{id}/approve</summary>
        [HttpPost("{id:int}/approve")]
        public async Task<IActionResult> Approve(int id, [FromBody] ApproveRejectDto dto)
        {
            var result = await _workflowService.ApproveAsync(id, dto, User.GetUserId(), User.GetRoleName());
            return Ok(result);
        }

        /// <summary>POST /api/tenders/{id}/reject</summary>
        [HttpPost("{id:int}/reject")]
        public async Task<IActionResult> Reject(int id, [FromBody] ApproveRejectDto dto)
        {
            var result = await _workflowService.RejectAsync(id, dto, User.GetUserId(), User.GetRoleName());
            return Ok(result);
        }

        /// <summary>GET /api/tenders/{id}/history</summary>
        [HttpGet("{id:int}/history")]
        public async Task<IActionResult> History(int id)
        {
            var result = await _workflowService.GetHistoryAsync(id);
            return Ok(result);
        }

        /// <summary>GET /api/tenders/workflow-steps - ordered workflow states used by the admin progress bar</summary>
        [HttpGet("workflow-steps")]
        public IActionResult GetWorkflowSteps()
        {
            var items = WorkflowEngine.Transitions
                .Select(kvp => new WorkflowStepDto
                {
                    Status = kvp.Key,
                    StageName = kvp.Value.ApprovalStage,
                    AssigneeRole = kvp.Value.SubmitRoles.FirstOrDefault() ?? kvp.Value.ApproverRoles.FirstOrDefault(),
                    DepartmentName = null,
                    IsCurrent = false,
                })
                .ToList();

            return Ok(items);
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
        public async Task<IActionResult> GetPending()
        {
            // Role id is resolved from the JWT role name via a lightweight lookup in the service;
            // for simplicity we accept either roleId query param or infer from claims downstream.
            var roleIdClaim = User.FindFirst("roleId")?.Value;
            var roleId = int.TryParse(roleIdClaim, out var rid) ? rid : 0;
            var result = await _workflowService.GetPendingApprovalsForRoleAsync(roleId, User.GetUserId());
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
        public async Task<IActionResult> Stats()
        {
            var roleIdClaim = User.FindFirst("roleId")?.Value;
            var roleId = int.TryParse(roleIdClaim, out var rid) ? rid : 0;
            var result = await _workflowService.GetDashboardStatsAsync(User.GetUserId(), roleId);
            return Ok(result);
        }
    }
}
