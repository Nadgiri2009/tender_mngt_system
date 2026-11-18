using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TenderManagement.Infrastructure.Data;

namespace TenderManagement.API.Controllers
{
    /// <summary>
    /// Exposes read-only options configured in the database.
    /// </summary>
    [ApiController]
    [Route("api/lookups")]
    [AllowAnonymous]
    public class LookupsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public LookupsController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>GET /api/lookups/tender-statuses</summary>
        [HttpGet("tender-statuses")]
        public async Task<IActionResult> GetTenderStatuses(CancellationToken cancellationToken)
        {
            var activeTransitions = _context.WorkflowTransitions.AsNoTracking()
                .Where(transition => transition.IsActive && transition.WorkflowVersion == 2)
                .Select(transition => transition.FromStatus)
                .Union(_context.WorkflowTransitions.AsNoTracking()
                    .Where(transition => transition.IsActive && transition.WorkflowVersion == 2)
                    .Select(transition => transition.ToStatus));
            return Ok(await activeTransitions.OrderBy(status => status).ToListAsync(cancellationToken));
        }

        /// <summary>GET /api/lookups/tender-methods returns active procurement methods.</summary>
        [HttpGet("tender-methods")]
        public async Task<IActionResult> GetTenderMethods(CancellationToken cancellationToken)
        {
            var methods = await _context.TenderMethods.AsNoTracking()
                .Where(method => method.IsActive)
                .OrderBy(method => method.SortOrder)
                .ThenBy(method => method.MethodName)
                .Select(method => method.MethodName)
                .ToListAsync(cancellationToken);
            return Ok(methods);
        }

        /// <summary>GET /api/lookups/tender-types returns active tender types.</summary>
        [HttpGet("tender-types")]
        public async Task<IActionResult> GetTenderTypes(CancellationToken cancellationToken)
        {
            var types = await _context.TenderCategories.AsNoTracking()
                .Where(type => type.IsActive)
                .OrderBy(type => type.SortOrder)
                .ThenBy(type => type.TypeName)
                .Select(type => type.TypeName)
                .ToListAsync(cancellationToken);
            return Ok(types);
        }

        /// <summary>Legacy alias for GET /api/lookups/tender-types.</summary>
        [HttpGet("tender-categories")]
        public Task<IActionResult> GetTenderCategories(CancellationToken cancellationToken) =>
            GetTenderTypes(cancellationToken);

        /// <summary>GET /api/lookups/role-names</summary>
        [HttpGet("role-names")]
        public async Task<IActionResult> GetRoleNames(CancellationToken cancellationToken)
        {
            var roles = await _context.Roles.AsNoTracking()
                .Where(role => role.IsActive)
                .OrderBy(role => role.RoleCode)
                .Select(role => new { role.RoleCode, role.RoleName })
                .ToListAsync(cancellationToken);
            return Ok(roles);
        }
    }
}
