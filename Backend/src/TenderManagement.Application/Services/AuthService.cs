using System.Security.Cryptography;
using TenderManagement.Application.Common;
using TenderManagement.Application.DTOs.Auth;
using TenderManagement.Application.Interfaces;
using TenderManagement.Domain.Entities;
using TenderManagement.Domain.Enums;

namespace TenderManagement.Application.Services
{
    public sealed class AuthService : IAuthService
    {
        private const string InvalidOtpMessage = "The one-time code is invalid or expired.";
        private const string OtpRequestMessage =
            "If this mobile number is linked to an active account, sign-in instructions will be sent when available.";

        private readonly IUnitOfWork _unitOfWork;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IAuditService _auditService;
        private readonly IAuthOtpChallengeStore _otpChallengeStore;
        private readonly IAuthOtpDeliveryService _otpDeliveryService;

        public AuthService(
            IUnitOfWork unitOfWork,
            IJwtTokenService jwtTokenService,
            IAuditService auditService,
            IAuthOtpChallengeStore otpChallengeStore,
            IAuthOtpDeliveryService otpDeliveryService)
        {
            _unitOfWork = unitOfWork;
            _jwtTokenService = jwtTokenService;
            _auditService = auditService;
            _otpChallengeStore = otpChallengeStore;
            _otpDeliveryService = otpDeliveryService;
        }

        public async Task<RequestLoginOtpResponseDto> RequestLoginOtpAsync(
            RequestLoginOtpRequestDto request, CancellationToken cancellationToken)
        {
            if (!_otpDeliveryService.IsAvailable)
            {
                throw new OtpDeliveryUnavailableException();
            }

            var mobile = NormalizeMobile(request.Mobile)
                ?? throw new BusinessRuleException("Enter a valid Indian mobile number.");
            var activeUsers = await _unitOfWork.Users.FindAsync(
                user => user.IsActive && user.Mobile != null, cancellationToken);
            var user = activeUsers.FirstOrDefault(candidate =>
                NormalizeMobile(candidate.Mobile) == mobile);

            if (user is not null)
            {
                var role = await _unitOfWork.Roles.GetByIdAsync(user.RoleId, cancellationToken);
                if (role is null || !role.IsActive)
                {
                    user = null;
                }
            }

            var now = DateTimeOffset.UtcNow;
            var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
            var canSend = _otpChallengeStore.TryIssue(
                mobile,
                user?.UserId,
                code,
                now,
                now.AddMinutes(5));

            if (user is not null && canSend)
            {
                await _otpDeliveryService.SendAsync(mobile, code, cancellationToken);
            }

            return new RequestLoginOtpResponseDto(OtpRequestMessage);
        }

        public async Task<AuthResponseDto> LoginAsync(
            LoginRequestDto request, CancellationToken cancellationToken)
        {
            var mobile = NormalizeMobile(request.Mobile);
            if (mobile is null ||
                !_otpChallengeStore.TryVerify(mobile, request.Otp, DateTimeOffset.UtcNow, out var userId))
            {
                throw new UnauthorizedAccessException(InvalidOtpMessage);
            }

            var users = await _unitOfWork.Users.FindAsync(
                user => user.UserId == userId && user.IsActive, cancellationToken);
            var user = users.FirstOrDefault();
            if (user is null)
            {
                throw new UnauthorizedAccessException(InvalidOtpMessage);
            }

            var role = await _unitOfWork.Roles.GetByIdAsync(user.RoleId, cancellationToken);
            if (role is null || !role.IsActive)
            {
                throw new UnauthorizedAccessException(InvalidOtpMessage);
            }

            var (token, expiresAt) = _jwtTokenService.GenerateToken(user, role.RoleName);
            await _auditService.LogAsync(user.UserId, "Login", nameof(User), user.UserId,
                cancellationToken: cancellationToken);

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

        public async Task<AuthResponseDto> RegisterAsync(
            RegisterRequestDto request, CancellationToken cancellationToken)
        {
            if (!request.TermsAccepted)
            {
                throw new BusinessRuleException("You must accept the portal terms to register.");
            }

            var existing = await _unitOfWork.Users.FindAsync(
                user => user.Email == request.Email, cancellationToken);
            if (existing.Any())
            {
                throw new BusinessRuleException("A user with this email already exists.");
            }

            var role = await _unitOfWork.Roles.GetByIdAsync(request.RoleId, cancellationToken)
                ?? throw new NotFoundException(nameof(Role), request.RoleId);

            if (!string.Equals(role.RoleName, RoleNames.Contractor, StringComparison.OrdinalIgnoreCase))
            {
                throw new BusinessRuleException(
                    "Public self-registration is only available for the Contractor/Vendor role. " +
                    "Staff accounts must be provisioned by an administrator.");
            }

            var mobile = NormalizeMobile(request.Mobile)
                ?? throw new BusinessRuleException("Enter a valid Indian mobile number.");
            var user = new User
            {
                Email = request.Email,
                Mobile = mobile,
                FullName = request.FullName,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                RoleId = role.RoleId,
                DepartmentId = request.DepartmentId ?? 1,
                IsActive = true
            };

            await _unitOfWork.Users.AddAsync(user, cancellationToken);
            await _unitOfWork.CompleteAsync(cancellationToken);

            await _unitOfWork.Contractors.AddAsync(new Contractor
            {
                Name = request.CompanyName.Trim(),
                RegistrationNo = $"PORTAL-{user.UserId:D8}",
                Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim(),
                ContactNo = user.Mobile,
                Email = user.Email,
                UserId = user.UserId,
                IsActive = true
            }, cancellationToken);
            await _unitOfWork.CompleteAsync(cancellationToken);

            await _auditService.LogAsync(user.UserId, "Register", nameof(User), user.UserId,
                cancellationToken: cancellationToken);
            await _auditService.LogAsync(user.UserId, "AcceptRegistrationTerms", nameof(User), user.UserId,
                "Portal terms v1", cancellationToken);

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

        private static string? NormalizeMobile(string? mobile)
        {
            if (string.IsNullOrWhiteSpace(mobile))
            {
                return null;
            }

            var digits = new string(mobile.Where(char.IsDigit).ToArray());
            if (digits.Length == 12 && digits.StartsWith("91", StringComparison.Ordinal))
            {
                digits = digits[2..];
            }
            else if (digits.Length == 11 && digits[0] == '0')
            {
                digits = digits[1..];
            }

            return digits.Length == 10 && digits[0] is >= '6' and <= '9'
                ? digits
                : null;
        }
    }
}
