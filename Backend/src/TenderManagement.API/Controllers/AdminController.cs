using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using TenderManagement.API.Extensions;
using TenderManagement.Application.Common;
using TenderManagement.Application.DTOs.Role;
using TenderManagement.Application.DTOs.User;
using TenderManagement.Application.Interfaces;
using TenderManagement.Domain.Entities;
using TenderManagement.Domain.Enums;
using TenderManagement.Infrastructure.Data;

namespace TenderManagement.API.Controllers
{
    [ApiController]
    [Route("api/departments")]
    [Authorize]
    public class DepartmentsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public DepartmentsController(
            IUnitOfWork unitOfWork,
            ApplicationDbContext context,
            IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _context = context;
            _auditService = auditService;
        }

        /// <summary>GET /api/departments</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok((await _unitOfWork.Departments.GetAllAsync()).OrderBy(d => d.DepartmentName));

        /// <summary>GET /api/departments/paged - searchable server-side department list.</summary>
        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            CancellationToken cancellationToken = default)
        {
            pageNumber = Math.Max(pageNumber, 1);
            pageSize = Math.Clamp(pageSize, 1, 100);
            var query = _context.Departments.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(department =>
                    department.DepartmentName.Contains(term) ||
                    (department.DepartmentCode != null && department.DepartmentCode.Contains(term)) ||
                    (department.HeadOfficerName != null && department.HeadOfficerName.Contains(term)));
            }

            var totalCount = await query.CountAsync(cancellationToken);
            var result = new PagedResult<Department>
            {
                Items = await query
                    .OrderBy(department => department.DepartmentName)
                    .ThenBy(department => department.DepartmentId)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken),
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
            return Ok(result);
        }

        /// <summary>GET /api/departments/{id}</summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var department = await _unitOfWork.Departments.GetByIdAsync(id);
            if (department is null) return NotFound();
            return Ok(department);
        }

        /// <summary>POST /api/departments - Admin only</summary>
        [HttpPost]
        [Authorize(Policy = "masterdata.manage")]
        public async Task<IActionResult> Create([FromBody] Department department)
        {
            if (string.IsNullOrWhiteSpace(department.DepartmentName))
            {
                return BadRequest(new { message = "Department name is required." });
            }

            var duplicateName = await _unitOfWork.Departments.FindAsync(d => d.DepartmentName.Trim() == department.DepartmentName.Trim() && d.DepartmentId != department.DepartmentId);
            if (duplicateName.Any())
            {
                return Conflict(new { message = "A department with this name already exists." });
            }

            if (!string.IsNullOrWhiteSpace(department.DepartmentCode))
            {
                var duplicateCode = await _unitOfWork.Departments.FindAsync(d => d.DepartmentCode != null && d.DepartmentCode.Trim() == department.DepartmentCode.Trim() && d.DepartmentId != department.DepartmentId);
                if (duplicateCode.Any())
                {
                    return Conflict(new { message = "A department with this code already exists." });
                }
            }

            if (department.DepartmentId != 0)
            {
                department.DepartmentId = 0;
            }

            await _unitOfWork.Departments.AddAsync(department);
            await _unitOfWork.CompleteAsync();
            await _auditService.LogAsync(User.GetUserId(), "Create", nameof(Department), department.DepartmentId);
            return Ok(department);
        }

        /// <summary>PUT /api/departments/{id} - Admin only</summary>
        [HttpPut("{id:int}")]
        [Authorize(Policy = "masterdata.manage")]
        public async Task<IActionResult> Update(int id, [FromBody] Department updatedDepartment)
        {
            var department = await _unitOfWork.Departments.GetByIdAsync(id);
            if (department is null) return NotFound();

            if (string.IsNullOrWhiteSpace(updatedDepartment.DepartmentName))
            {
                return BadRequest(new { message = "Department name is required." });
            }

            var duplicateName = await _unitOfWork.Departments.FindAsync(d => d.DepartmentName.Trim() == updatedDepartment.DepartmentName.Trim() && d.DepartmentId != id);
            if (duplicateName.Any())
            {
                return Conflict(new { message = "A department with this name already exists." });
            }

            if (!string.IsNullOrWhiteSpace(updatedDepartment.DepartmentCode))
            {
                var duplicateCode = await _unitOfWork.Departments.FindAsync(d => d.DepartmentCode != null && d.DepartmentCode.Trim() == updatedDepartment.DepartmentCode.Trim() && d.DepartmentId != id);
                if (duplicateCode.Any())
                {
                    return Conflict(new { message = "A department with this code already exists." });
                }
            }

            department.DepartmentName = updatedDepartment.DepartmentName.Trim();
            department.DepartmentCode = string.IsNullOrWhiteSpace(updatedDepartment.DepartmentCode) ? null : updatedDepartment.DepartmentCode.Trim();
            department.HeadOfficerName = string.IsNullOrWhiteSpace(updatedDepartment.HeadOfficerName) ? null : updatedDepartment.HeadOfficerName.Trim();
            department.IsActive = updatedDepartment.IsActive;

            _unitOfWork.Departments.Update(department);
            await _unitOfWork.CompleteAsync();
            await _auditService.LogAsync(User.GetUserId(), "Update", nameof(Department), id);
            return Ok(department);
        }

        /// <summary>DELETE /api/departments/{id} - delete only when no records reference it.</summary>
        [HttpDelete("{id:int}")]
        [Authorize(Policy = "masterdata.manage")]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var department = await _unitOfWork.Departments.GetByIdAsync(id);
            if (department is null) return NotFound();

            var referenceCount = await _context.Database.SqlQuery<long>($"""
                SELECT
                    (SELECT COUNT_BIG(*) FROM dbo.Users WHERE DepartmentId={id}) +
                    (SELECT COUNT_BIG(*) FROM dbo.Tender WHERE DepartmentId={id}) +
                    (SELECT COUNT_BIG(*) FROM dbo.TenderLifecycleHistory WHERE DepartmentId={id}) +
                    (SELECT COUNT_BIG(*) FROM dbo.TenderWorkflowAssignments WHERE DepartmentId={id}) +
                    (SELECT COUNT_BIG(*) FROM dbo.TenderWorkflowHistory WHERE DepartmentId={id})
                    AS [Value]
                """).SingleAsync(cancellationToken);

            if (referenceCount > 0)
            {
                department.IsActive = false;
                _unitOfWork.Departments.Update(department);
                await _unitOfWork.CompleteAsync();
                await _auditService.LogAsync(User.GetUserId(), "Deactivate", nameof(Department), id);
                return Ok(new { message = "This department has existing records, so it was deactivated instead of deleted." });
            }

            _unitOfWork.Departments.Remove(department);
            await _unitOfWork.CompleteAsync();
            await _auditService.LogAsync(User.GetUserId(), "Delete", nameof(Department), id);
            return Ok(new { message = "Department deleted successfully." });
        }
    }

    [ApiController]
    [Route("api/roles")]
    [Authorize]
    public class RolesController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public RolesController(IUnitOfWork unitOfWork, ApplicationDbContext context, IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _context = context;
            _auditService = auditService;
        }

        /// <summary>GET /api/roles</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll() =>
            Ok((await _unitOfWork.Roles.GetAllAsync()).Where(role => role.IsActive));

        /// <summary>GET /api/roles/permissions - read the fixed role-permission matrix.</summary>
        [HttpGet("permissions")]
        [Authorize(Policy = "roles.manage")]
        public async Task<IActionResult> GetPermissionMatrix(CancellationToken cancellationToken)
        {
            var roles = await _context.Roles.AsNoTracking()
                .Where(role => role.IsActive)
                .OrderBy(role => role.RoleCode)
                .Select(role => new { role.RoleId, role.RoleCode, role.RoleName })
                .ToListAsync(cancellationToken);
            var permissions = await _context.Permissions.AsNoTracking()
                .Where(permission => permission.IsActive)
                .OrderBy(permission => permission.PermissionKey)
                .Select(permission => new { permission.PermissionKey, permission.Description })
                .ToListAsync(cancellationToken);
            var grants = await _context.RolePermissions.AsNoTracking()
                .Where(grant => roles.Select(role => role.RoleId).Contains(grant.RoleId))
                .Select(grant => new { grant.RoleId, grant.Permission!.PermissionKey })
                .ToListAsync(cancellationToken);

            return Ok(new
            {
                roles = roles.Select(role => new
                {
                    role.RoleId,
                    role.RoleCode,
                    role.RoleName,
                    permissionKeys = grants.Where(grant => grant.RoleId == role.RoleId)
                        .Select(grant => grant.PermissionKey)
                        .OrderBy(key => key)
                        .ToArray()
                }),
                permissions
            });
        }

        /// <summary>PUT /api/roles/{id}/permissions - update grants without adding or removing roles.</summary>
        [HttpPut("{id:int}/permissions")]
        [Authorize(Policy = "roles.manage")]
        public async Task<IActionResult> UpdatePermissions(
            int id,
            [FromBody] UpdateRolePermissionsRequest request,
            CancellationToken cancellationToken)
        {
            var role = await _context.Roles.SingleOrDefaultAsync(
                item => item.RoleId == id && item.IsActive,
                cancellationToken);
            if (role is null) return NotFound();
            if (role.RoleCode is not ("Junior Engineer (JE)" or "TENDER_CELL" or "CHAIRMAN" or "DIRECTOR_AUDITOR"))
            {
                return Conflict(new { message = "Only the four approved roles can be managed." });
            }

            var requestedKeys = request.PermissionKeys
                .Where(key => !string.IsNullOrWhiteSpace(key))
                .Select(key => key.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToHashSet(StringComparer.Ordinal);
            if (role.RoleCode == "Junior Engineer (JE)")
            {
                requestedKeys.UnionWith(new[] { "users.manage", "masterdata.manage", "roles.manage" });
            }

            var availableKeys = await _context.Permissions.AsNoTracking()
                .Where(permission => permission.IsActive)
                .Select(permission => permission.PermissionKey)
                .ToListAsync(cancellationToken);
            var unknownKeys = requestedKeys.Except(availableKeys, StringComparer.Ordinal).ToArray();
            if (unknownKeys.Length > 0)
            {
                return BadRequest(new { message = "One or more permission keys are not in the active catalog.", permissionKeys = unknownKeys });
            }

            var permissions = await _context.Permissions
                .Where(permission => permission.IsActive && requestedKeys.Contains(permission.PermissionKey))
                .Select(permission => permission.PermissionId)
                .ToListAsync(cancellationToken);
            var currentGrants = await _context.RolePermissions
                .Where(grant => grant.RoleId == id)
                .ToListAsync(cancellationToken);
            var requestedPermissionIds = permissions.ToHashSet();
            _context.RolePermissions.RemoveRange(currentGrants.Where(grant => !requestedPermissionIds.Contains(grant.PermissionId)));

            var currentPermissionIds = currentGrants.Select(grant => grant.PermissionId).ToHashSet();
            foreach (var permissionId in requestedPermissionIds.Except(currentPermissionIds))
            {
                _context.RolePermissions.Add(new RolePermission
                {
                    RoleId = id,
                    PermissionId = permissionId,
                    AssignedBy = User.GetUserId(),
                    AssignedAtUtc = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync(cancellationToken);
            await _auditService.LogAsync(
                User.GetUserId(),
                "UpdatePermissions",
                nameof(Role),
                id,
                string.Join(",", requestedKeys.OrderBy(key => key)),
                cancellationToken);

            return Ok(new { role.RoleId, role.RoleCode, permissionKeys = requestedKeys.OrderBy(key => key) });
        }

        /// <summary>GET /api/roles/registration - returns only the self-registration role</summary>
        [AllowAnonymous]
        [HttpGet("registration")]
        public IActionResult GetRegistrationRole()
        {
            return Ok(new { accountType = "ExternalBidder", roleName = "External bidder" });
        }
    }

    [ApiController]
    [Route("api/contractors")]
    [Authorize]
    public class ContractorsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        public ContractorsController(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        /// <summary>GET /api/contractors</summary>
        [HttpGet]
        [Authorize(Roles = $"{RoleNames.JuniorEngineer},{RoleNames.TenderCell},{RoleNames.TenderCommitteeChairman},{RoleNames.DirectorAuditor}")]
        public async Task<IActionResult> GetAll() => Ok(await _unitOfWork.Contractors.GetAllAsync());

        /// <summary>
        /// GET /api/contractors/me - resolves the calling Contractor-role user's own Contractor
        /// record from their UserId claim. Added so the Bid submission UI can discover the
        /// caller's ContractorId without asking a contractor to pick themselves from a list.
        /// Previously missing: no endpoint linked a logged-in Contractor user back to their
        /// Contractors row, even though Contractor.UserId already models that link.
        /// </summary>
        [HttpGet("me")]
        [Authorize(Roles = RoleNames.Contractor)]
        public async Task<IActionResult> GetMyContractorProfile()
        {
            var userId = User.GetUserId();
            var matches = await _unitOfWork.Contractors.FindAsync(c => c.UserId == userId);
            var contractor = matches.FirstOrDefault();
            if (contractor is null)
            {
                return NotFound(new { message = "No contractor profile is linked to this account yet. Contact an administrator to link your Contractor record." });
            }
            return Ok(contractor);
        }

        /// <summary>POST /api/contractors</summary>
        [HttpPost]
        [Authorize(Roles = RoleNames.Disabled)]
        public async Task<IActionResult> Create([FromBody] Contractor contractor)
        {
            await _unitOfWork.Contractors.AddAsync(contractor);
            await _unitOfWork.CompleteAsync();
            return Ok(contractor);
        }
    }

    [ApiController]
    [Route("api/users")]
    [Authorize(Policy = "users.manage")]
    public class UsersController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public UsersController(IUnitOfWork unitOfWork, ApplicationDbContext context, IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _context = context;
            _auditService = auditService;
        }

        /// <summary>GET /api/users - Admin: manage users</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _unitOfWork.Users.GetAllAsync();
            var roles = (await _unitOfWork.Roles.GetAllAsync()).ToDictionary(r => r.RoleId, r => r.RoleName);
            var departments = (await _unitOfWork.Departments.GetAllAsync()).ToDictionary(d => d.DepartmentId, d => d.DepartmentName);

            var result = users.Select(u => new
            {
                u.UserId,
                u.FullName,
                u.Email,
                u.Mobile,
                RoleName = u.RoleId.HasValue ? roles.GetValueOrDefault(u.RoleId.Value, "External bidder") : "External bidder",
                DepartmentName = u.DepartmentId.HasValue ? departments.GetValueOrDefault(u.DepartmentId.Value) : null,
                u.AccountType,
                u.IsActive,
                u.CreatedDate,
            }).OrderByDescending(u => u.CreatedDate);

            return Ok(result);
        }

        /// <summary>GET /api/users/paged - searchable server-side user list.</summary>
        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            CancellationToken cancellationToken = default)
        {
            pageNumber = Math.Max(pageNumber, 1);
            pageSize = Math.Clamp(pageSize, 1, 100);
            var query = _context.Users.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(user =>
                    user.FullName.Contains(term) ||
                    user.Email.Contains(term) ||
                    (user.Mobile != null && user.Mobile.Contains(term)) ||
                    (user.Role != null && user.Role.RoleName.Contains(term)) ||
                    (user.Department != null && user.Department.DepartmentName.Contains(term)));
            }

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderByDescending(user => user.CreatedDate)
                .ThenByDescending(user => user.UserId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(user => new
                {
                    user.UserId,
                    user.FullName,
                    user.Email,
                    user.Mobile,
                    RoleName = user.RoleId.HasValue ? user.Role!.RoleName : "External bidder",
                    DepartmentName = user.Department != null ? user.Department.DepartmentName : null,
                    user.AccountType,
                    user.IsActive,
                    user.CreatedDate
                })
                .ToListAsync(cancellationToken);

            return Ok(new PagedResult<object>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            });
        }

        /// <summary>
        /// POST /api/users - create a staff account with any role (Admin only). This is the only
        /// way to create non-Contractor accounts now that public self-registration
        /// (AuthService.RegisterAsync) is locked to the Contractor role for security.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateStaffUserDto dto)
        {
            var existing = await _unitOfWork.Users.FindAsync(u => u.Email == dto.Email);
            if (existing.Any())
            {
                throw new BusinessRuleException("A user with this email already exists.");
            }

            var role = await _unitOfWork.Roles.GetByIdAsync(dto.RoleId)
                ?? throw new NotFoundException(nameof(Role), dto.RoleId);
            if (!role.IsActive || role.RoleCode is not ("Junior Engineer (JE)" or "TENDER_CELL" or "CHAIRMAN" or "DIRECTOR_AUDITOR"))
            {
                throw new BusinessRuleException("Staff accounts can only use one of the four approved internal roles.");
            }

            if (dto.DepartmentId.HasValue)
            {
                var department = await _unitOfWork.Departments.GetByIdAsync(dto.DepartmentId.Value)
                    ?? throw new NotFoundException(nameof(Department), dto.DepartmentId.Value);
                if (!department.IsActive) throw new BusinessRuleException("Select an active department.");
            }

            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                Mobile = dto.Mobile,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                AccountType = "Internal",
                RoleId = role.RoleId,
                DepartmentId = dto.DepartmentId,
                IsActive = true,
            };

            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.CompleteAsync();
            await _auditService.LogAsync(User.GetUserId(), "Create", nameof(User), user.UserId);

            return Ok(new
            {
                user.UserId,
                user.FullName,
                user.Email,
                user.Mobile,
                RoleName = role.RoleName,
                DepartmentName = dto.DepartmentId.HasValue
                    ? (await _unitOfWork.Departments.GetByIdAsync(dto.DepartmentId.Value))?.DepartmentName
                    : null,
                user.AccountType,
                user.IsActive,
                user.CreatedDate,
            });
        }

        /// <summary>PUT /api/users/{id} - update staff profile and one of the four fixed role assignments.</summary>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateStaffUserDto dto)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(id);
            if (user is null) return NotFound();
            if (user.AccountType != "Internal")
            {
                return Conflict(new { message = "External bidder accounts cannot be changed through staff management." });
            }

            var duplicateEmail = await _unitOfWork.Users.FindAsync(
                existing => existing.Email == dto.Email && existing.UserId != id);
            if (duplicateEmail.Any()) return Conflict(new { message = "A user with this email already exists." });

            var role = await _unitOfWork.Roles.GetByIdAsync(dto.RoleId)
                ?? throw new NotFoundException(nameof(Role), dto.RoleId);
            if (!role.IsActive || role.RoleCode is not ("Junior Engineer (JE)" or "TENDER_CELL" or "CHAIRMAN" or "DIRECTOR_AUDITOR"))
            {
                throw new BusinessRuleException("Staff accounts can only use one of the four approved internal roles.");
            }

            if (dto.DepartmentId.HasValue)
            {
                var department = await _unitOfWork.Departments.GetByIdAsync(dto.DepartmentId.Value)
                    ?? throw new NotFoundException(nameof(Department), dto.DepartmentId.Value);
                if (!department.IsActive) throw new BusinessRuleException("Select an active department.");
            }

            var currentRole = user.RoleId.HasValue
                ? await _unitOfWork.Roles.GetByIdAsync(user.RoleId.Value)
                : null;
            if (currentRole?.RoleCode == "Junior Engineer (JE)" && role.RoleCode != "Junior Engineer (JE)")
            {
                var activejeCount = await _context.Users.CountAsync(
                    existing => existing.IsActive && existing.AccountType == "Internal" &&
                        existing.Role != null && existing.Role.RoleCode == "Junior Engineer (JE)");
                if (activejeCount <= 1)
                {
                    return Conflict(new { message = "The last active Junior Engineer (JE) Main Admin cannot be reassigned." });
                }
            }

            user.FullName = dto.FullName.Trim();
            user.Email = dto.Email.Trim();
            user.Mobile = dto.Mobile;
            user.RoleId = role.RoleId;
            user.DepartmentId = dto.DepartmentId;
            user.ModifiedBy = User.GetUserId();
            user.ModifiedDate = DateTime.UtcNow;
            _unitOfWork.Users.Update(user);
            await _unitOfWork.CompleteAsync();
            await _auditService.LogAsync(User.GetUserId(), "Update", nameof(User), id);

            return Ok(new { user.UserId, user.FullName, user.Email, user.Mobile, roleName = role.RoleName, user.DepartmentId, user.IsActive });
        }

        /// <summary>PUT /api/users/{id}/toggle-active</summary>
        [HttpPut("{id:int}/toggle-active")]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(id);
            if (user is null) return NotFound();

            var activating = !user.IsActive;
            if (!activating && id == User.GetUserId())
            {
                return Conflict(new { message = "You cannot deactivate your own account." });
            }

            var role = user.RoleId.HasValue ? await _unitOfWork.Roles.GetByIdAsync(user.RoleId.Value) : null;
            if (!activating && role?.RoleCode == "Junior Engineer (JE)")
            {
                var activejeCount = await _context.Users.CountAsync(
                    existing => existing.IsActive && existing.AccountType == "Internal" &&
                        existing.Role != null && existing.Role.RoleCode == "Junior Engineer (JE)");
                if (activejeCount <= 1)
                {
                    return Conflict(new { message = "The last active Junior Engineer (JE) Main Admin cannot be deactivated." });
                }
            }

            user.IsActive = activating;
            user.ModifiedBy = User.GetUserId();
            user.ModifiedDate = DateTime.UtcNow;
            _unitOfWork.Users.Update(user);
            await _unitOfWork.CompleteAsync();
            await _auditService.LogAsync(User.GetUserId(), activating ? "Activate" : "Deactivate", nameof(User), id);
            return Ok(new { user.UserId, user.IsActive });
        }

        /// <summary>PUT /api/users/{id}/reset-password - reset a local account credential.</summary>
        [HttpPut("{id:int}/reset-password")]
        public async Task<IActionResult> ResetPassword(int id, [FromBody] ResetStaffUserPasswordDto request)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(id);
            if (user is null) return NotFound();
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            user.ModifiedBy = User.GetUserId();
            user.ModifiedDate = DateTime.UtcNow;
            _unitOfWork.Users.Update(user);
            await _unitOfWork.CompleteAsync();
            await _auditService.LogAsync(User.GetUserId(), "ResetPassword", nameof(User), id);
            return NoContent();
        }

        /// <summary>DELETE /api/users/{id} - remove only when no historical or business record references the account.</summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(id);
            if (user is null) return NotFound();
            if (id == User.GetUserId())
            {
                return Conflict(new { message = "You cannot delete your own account." });
            }

            var role = user.RoleId.HasValue ? await _unitOfWork.Roles.GetByIdAsync(user.RoleId.Value) : null;
            if (user.IsActive && role?.RoleCode == "Junior Engineer (JE)")
            {
                var activejeCount = await _context.Users.CountAsync(
                    existing => existing.IsActive && existing.AccountType == "Internal" &&
                        existing.Role != null && existing.Role.RoleCode == "Junior Engineer (JE)");
                if (activejeCount <= 1)
                {
                    return Conflict(new { message = "The last active Junior Engineer (JE) Main Admin cannot be deleted." });
                }
            }

            _unitOfWork.Users.Remove(user);
            try
            {
                await _unitOfWork.CompleteAsync();
            }
            catch (DbUpdateException exception) when (exception.GetBaseException() is SqlException { Number: 547 })
            {
                return Conflict(new { message = "This account is referenced by business or audit history and cannot be deleted. Deactivate it instead." });
            }

            await _auditService.LogAsync(User.GetUserId(), "Delete", nameof(User), id);
            return NoContent();
        }
    }
}
