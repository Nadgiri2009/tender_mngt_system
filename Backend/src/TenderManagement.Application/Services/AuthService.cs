using TenderManagement.Application.Common;
using TenderManagement.Application.DTOs.Auth;
using TenderManagement.Application.Interfaces;
using TenderManagement.Domain.Entities;

namespace TenderManagement.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IAuditService _auditService;

        public AuthService(IUnitOfWork unitOfWork, IJwtTokenService jwtTokenService, IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _jwtTokenService = jwtTokenService;
            _auditService = auditService;
        }

        public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
        {
            var users = await _unitOfWork.Users.FindAsync(u => u.Email == request.Email && u.IsActive);
            var user = users.FirstOrDefault();

            if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                throw new BusinessRuleException("Invalid email or password.");
            }

            var role = await _unitOfWork.Roles.GetByIdAsync(user.RoleId);
            var roleName = role?.RoleName ?? "Unknown";

            var (token, expiresAt) = _jwtTokenService.GenerateToken(user, roleName);

            await _auditService.LogAsync(user.UserId, "Login", nameof(User), user.UserId);

            return new AuthResponseDto
            {
                Token = token,
                ExpiresAt = expiresAt,
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Role = roleName,
                DepartmentId = user.DepartmentId
            };
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
        {
            var existing = await _unitOfWork.Users.FindAsync(u => u.Email == request.Email);
            if (existing.Any())
            {
                throw new BusinessRuleException("A user with this email already exists.");
            }

            var role = await _unitOfWork.Roles.GetByIdAsync(request.RoleId)
                ?? throw new NotFoundException(nameof(Role), request.RoleId);

            // SECURITY: public self-registration must never be able to mint an Admin or
            // department-officer account -- those are government staff roles and must be
            // provisioned by an Admin through an authenticated admin-only flow. Without this
            // check, anyone could POST /api/auth/register with RoleId pointing at "Admin" and
            // grant themselves full system access.
            if (!string.Equals(role.RoleName, TenderManagement.Domain.Enums.RoleNames.Contractor, StringComparison.OrdinalIgnoreCase))
            {
                throw new BusinessRuleException(
                    "Public self-registration is only available for the Contractor/Vendor role. " +
                    "Staff accounts must be created by an administrator.");
            }

          var user = new User
{
    Email = request.Email,
    Mobile = request.Mobile,
    FullName = request.FullName,

    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),

    RoleId = role.RoleId,

    DepartmentId = request.DepartmentId ?? 1,

    IsActive = true
};

            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.CompleteAsync();

            await _auditService.LogAsync(user.UserId, "Register", nameof(User), user.UserId);

            var (token, expiresAt) = _jwtTokenService.GenerateToken(user, role.RoleName);

            return new AuthResponseDto
            {
                Token = token,
                ExpiresAt = expiresAt,
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Role = role.RoleName,
                DepartmentId = user.DepartmentId
            };
        }
    }
}
