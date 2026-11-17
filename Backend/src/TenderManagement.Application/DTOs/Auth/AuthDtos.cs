using System.ComponentModel.DataAnnotations;

namespace TenderManagement.Application.DTOs.Auth
{
    /// <summary>Payload for requesting a one-time sign-in code.</summary>
    public sealed record RequestLoginOtpRequestDto
    {
        [Required, Phone]
        public required string Mobile { get; init; }
    }

    /// <summary>Generic response returned after a one-time code request.</summary>
    public sealed record RequestLoginOtpResponseDto(string Message);

    /// <summary>Payload for verifying a one-time sign-in code.</summary>
    public sealed record LoginRequestDto
    {
        [Required, Phone]
        public required string Mobile { get; init; }

        [Required, RegularExpression(@"^\d{6}$")]
        public required string Otp { get; init; }
    }

    public class RegisterRequestDto
    {
        [Required, StringLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Required, StringLength(150)]
        public string CompanyName { get; set; } = string.Empty;

        [StringLength(300)]
        public string? Address { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, Phone]
        public string Mobile { get; set; } = string.Empty;

        [Required, MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public bool TermsAccepted { get; set; }

        [Required]
        public int RoleId { get; set; }

        public int? DepartmentId { get; set; }
    }

    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int? DepartmentId { get; set; }
    }
}
