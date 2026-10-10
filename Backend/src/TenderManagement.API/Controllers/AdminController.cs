using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenderManagement.API.Extensions;
using TenderManagement.Application.Common;
using TenderManagement.Application.DTOs.User;
using TenderManagement.Application.Interfaces;
using TenderManagement.Domain.Entities;
using TenderManagement.Domain.Enums;

namespace TenderManagement.API.Controllers
{
    [ApiController]
    [Route("api/departments")]
    [Authorize]
    public class DepartmentsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        public DepartmentsController(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        /// <summary>GET /api/departments</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok((await _unitOfWork.Departments.GetAllAsync()).OrderBy(d => d.DepartmentName));

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
        [Authorize(Roles = RoleNames.Admin)]
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
            return Ok(department);
        }

        /// <summary>PUT /api/departments/{id} - Admin only</summary>
        [HttpPut("{id:int}")]
        [Authorize(Roles = RoleNames.Admin)]
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
            return Ok(department);
        }

        /// <summary>DELETE /api/departments/{id} - Admin only, soft disable for existing references</summary>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = RoleNames.Admin)]
        public async Task<IActionResult> Delete(int id)
        {
            var department = await _unitOfWork.Departments.GetByIdAsync(id);
            if (department is null) return NotFound();

            var hasTenderReferences = (await _unitOfWork.Tenders.FindAsync(t => t.DepartmentId == id)).Any();
            var hasUserReferences = (await _unitOfWork.Users.FindAsync(u => u.DepartmentId == id)).Any();

            if (hasTenderReferences || hasUserReferences)
            {
                department.IsActive = false;
                _unitOfWork.Departments.Update(department);
                await _unitOfWork.CompleteAsync();
                return Conflict(new { message = "This department is currently referenced by tenders or staff records. It has been disabled instead of being deleted." });
            }

            department.IsActive = false;
            _unitOfWork.Departments.Update(department);
            await _unitOfWork.CompleteAsync();
            return Ok(new { message = "Department disabled successfully." });
        }
    }

    [ApiController]
    [Route("api/roles")]
    [Authorize]
    public class RolesController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        public RolesController(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        /// <summary>GET /api/roles</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _unitOfWork.Roles.GetAllAsync());
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
        [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.DepartmentOfficer}")]
        public async Task<IActionResult> Create([FromBody] Contractor contractor)
        {
            await _unitOfWork.Contractors.AddAsync(contractor);
            await _unitOfWork.CompleteAsync();
            return Ok(contractor);
        }
    }

    [ApiController]
    [Route("api/users")]
    [Authorize(Roles = RoleNames.Admin)]
    public class UsersController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        public UsersController(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

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
                RoleName = roles.GetValueOrDefault(u.RoleId, "Unknown"),
                DepartmentName = u.DepartmentId.HasValue ? departments.GetValueOrDefault(u.DepartmentId.Value) : null,
                u.IsActive,
                u.CreatedDate,
            }).OrderByDescending(u => u.CreatedDate);

            return Ok(result);
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

            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                Mobile = dto.Mobile,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                RoleId = role.RoleId,
                DepartmentId = dto.DepartmentId,
                IsActive = true,
            };

            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.CompleteAsync();

            return Ok(new
            {
                user.UserId,
                user.FullName,
                user.Email,
                user.Mobile,
                RoleName = role.RoleName,
                DepartmentName = (string?)null,
                user.IsActive,
                user.CreatedDate,
            });
        }

        /// <summary>PUT /api/users/{id}/toggle-active</summary>
        [HttpPut("{id:int}/toggle-active")]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(id);
            if (user is null) return NotFound();

            user.IsActive = !user.IsActive;
            _unitOfWork.Users.Update(user);
            await _unitOfWork.CompleteAsync();
            return Ok(new { user.UserId, user.IsActive });
        }
    }
}
