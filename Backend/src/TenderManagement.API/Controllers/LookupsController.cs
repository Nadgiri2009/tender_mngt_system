using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TenderManagement.Domain.Enums;
using TenderManagement.Infrastructure.Data;

namespace TenderManagement.API.Controllers
{
    /// <summary>
    /// Exposes read-only option lists that are defined once, in the Domain enums, so the
    /// frontend never needs to hand-maintain a duplicate copy of TenderStatus / TenderType.
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
        public IActionResult GetTenderStatuses() => Ok(GetConstStringValues(typeof(TenderStatus)));

        /// <summary>GET /api/lookups/tender-types returns active procurement methods.</summary>
        [HttpGet("tender-types")]
        public async Task<IActionResult> GetTenderTypes(CancellationToken cancellationToken)
        {
            var methods = await _context.TenderMethods.AsNoTracking()
                .Where(method => method.IsActive)
                .OrderBy(method => method.SortOrder)
                .ThenBy(method => method.MethodName)
                .Select(method => method.MethodName)
                .ToListAsync(cancellationToken);
            return Ok(methods);
        }

        /// <summary>GET /api/lookups/tender-categories returns active tender categories.</summary>
        [HttpGet("tender-categories")]
        public async Task<IActionResult> GetTenderCategories(CancellationToken cancellationToken)
        {
            var categories = await _context.TenderCategories.AsNoTracking()
                .Where(category => category.IsActive)
                .OrderBy(category => category.SortOrder)
                .ThenBy(category => category.TypeName)
                .Select(category => category.TypeName)
                .ToListAsync(cancellationToken);
            return Ok(categories);
        }

        /// <summary>GET /api/lookups/role-names</summary>
        [HttpGet("role-names")]
        public IActionResult GetRoleNames() => Ok(GetConstStringValues(typeof(RoleNames)));

        private static List<string> GetConstStringValues(Type type) =>
            type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
                .Select(f => (string)f.GetRawConstantValue()!)
                .ToList();
    }
}
