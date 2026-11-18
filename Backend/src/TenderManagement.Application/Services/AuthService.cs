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

            var mobile = NormalizeLoginMobile(request.Mobile)
                ?? throw new BusinessRuleException("Enter a valid Indian mobile number.");
            var activeUsers = await _unitOfWork.Users.FindAsync(
                user => user.IsActive && user.Mobile != null, cancellationToken);
            var user = activeUsers.FirstOrDefault(candidate =>
                NormalizeLoginMobile(candidate.Mobile) == mobile);

            if (user is not null)
            {
                if (user.AccountType == "Internal")
                {
                    var role = user.RoleId.HasValue
                        ? await _unitOfWork.Roles.GetByIdAsync(user.RoleId.Value, cancellationToken)
                        : null;
                    if (role is null || !role.IsActive)
                        user = null;
                }
                else if (user.AccountType != "ExternalBidder" || user.RoleId.HasValue)
                    user = null;
            }

            var now = DateTimeOffset.UtcNow;
            var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
            var canSend = _otpChallengeStore.TryIssue(
                mobile,
                user?.UserId,
                code,
                now,
                now.AddMinutes(5));

            string? developmentOtp = null;
            if (user is not null && canSend)
            {
                await _otpDeliveryService.SendAsync(mobile, code, cancellationToken);
                if (_otpDeliveryService.CanExposeOtp)
                    developmentOtp = code;
            }

            return new RequestLoginOtpResponseDto(OtpRequestMessage, developmentOtp);
        }

        public async Task<AuthResponseDto> LoginAsync(
            LoginRequestDto request, CancellationToken cancellationToken)
        {
            var mobile = NormalizeLoginMobile(request.Mobile);
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

            var role = user.RoleId.HasValue
                ? await _unitOfWork.Roles.GetByIdAsync(user.RoleId.Value, cancellationToken)
                : null;
            if (user.AccountType == "Internal" && (role is null || !role.IsActive) ||
                user.AccountType == "ExternalBidder" && user.RoleId.HasValue ||
                user.AccountType is not ("Internal" or "ExternalBidder"))
            {
                throw new UnauthorizedAccessException(InvalidOtpMessage);
            }

            var roleName = role?.RoleName ?? RoleNames.ExternalBidder;
            var (token, expiresAt) = _jwtTokenService.GenerateToken(user, roleName);
            await _auditService.LogAsync(user.UserId, "Login", nameof(User), user.UserId,
                cancellationToken: cancellationToken);

            return new AuthResponseDto
            {
                Token = token,
                ExpiresAt = expiresAt,
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Role = roleName,
                AccountType = user.AccountType,
                DepartmentId = user.DepartmentId
            };
        }

        public async Task<RegisterResponseDto> RegisterAsync(
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

            var mobile = NormalizeMobile(request.Mobile)
                ?? throw new BusinessRuleException("Enter a valid Indian mobile number.");
            var duplicateMobile = await _unitOfWork.Users.FindAsync(
                user => user.Mobile == mobile && user.IsActive, cancellationToken);
            if (duplicateMobile.Any())
                throw new BusinessRuleException("An account already uses this mobile number.");

            var user = new User
            {
                Email = request.Email,
                Mobile = mobile,
                FullName = request.FullName,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(
                    Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))),
                AccountType = "ExternalBidder",
                RoleId = null,
                DepartmentId = null,
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

            return new RegisterResponseDto(
                user.UserId,
                "Registration complete. Sign in with your 10-digit mobile number and one-time code.");
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

        private static string? NormalizeLoginMobile(string? mobile) =>
            mobile is { Length: 10 } &&
            mobile.All(character => character is >= '0' and <= '9')
                ? mobile
                : null;
    }
}
