using System.ComponentModel.DataAnnotations;

namespace TenderManagement.Application.DTOs.Auth
{
    /// <summary>Payload for requesting a one-time sign-in code.</summary>
    public sealed record RequestLoginOtpRequestDto
    {
        [Required, RegularExpression(@"^\d{10}$",
            ErrorMessage = "Enter a 10-digit mobile number.")]
        public required string Mobile { get; init; }
    }

    /// <summary>Response returned after requesting a one-time sign-in code.</summary>
    public sealed record RequestLoginOtpResponseDto(string Message, string? DevelopmentOtp = null);

    /// <summary>Payload for updating the authenticated user's editable profile fields.</summary>
    public sealed record UpdateUserProfileRequestDto
    {
        [Required, StringLength(150, MinimumLength = 2)]
        public required string FullName { get; init; }

        [Required, EmailAddress, StringLength(150)]
        public required string Email { get; init; }
    }

    /// <summary>Profile fields returned for the authenticated user.</summary>
    public sealed record UserProfileResponseDto(
        int UserId,
        string FullName,
        string Email,
        string? Mobile);

    /// <summary>Payload for verifying a one-time sign-in code.</summary>
    public sealed record LoginRequestDto
    {
        [Required, RegularExpression(@"^\d{10}$",
            ErrorMessage = "Enter a 10-digit mobile number.")]
        public required string Mobile { get; init; }

        [Required, RegularExpression(@"^\d{6}$")]
        public required string Otp { get; init; }
    }

    public sealed record RegisterRequestDto
    {
        [Required, StringLength(150)]
        public required string FullName { get; init; }

        [Required, StringLength(150)]
        public required string CompanyName { get; init; }

        [StringLength(300)]
        public string? Address { get; init; }

        [Required, EmailAddress]
        public required string Email { get; init; }

        [Required, RegularExpression(@"^\d{10}$",
            ErrorMessage = "Enter a 10-digit mobile number.")]
        public required string Mobile { get; init; }

        [Required]
        public required bool TermsAccepted { get; init; }
    }

    /// <summary>Response returned after registering an external bidder account.</summary>
    public sealed record RegisterResponseDto(int UserId, string Message);

    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string AccountType { get; set; } = string.Empty;
        public int? DepartmentId { get; set; }
    }
}
