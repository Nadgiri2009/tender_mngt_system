using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenderManagement.Domain.Enums;

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
        /// <summary>GET /api/lookups/tender-statuses</summary>
        [HttpGet("tender-statuses")]
        public IActionResult GetTenderStatuses() => Ok(GetConstStringValues(typeof(TenderStatus)));

        /// <summary>GET /api/lookups/tender-types</summary>
        [HttpGet("tender-types")]
        public IActionResult GetTenderTypes() => Ok(GetConstStringValues(typeof(TenderType)));

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
