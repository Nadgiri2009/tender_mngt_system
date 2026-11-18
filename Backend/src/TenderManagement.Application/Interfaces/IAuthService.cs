using TenderManagement.Application.DTOs.Auth;

namespace TenderManagement.Application.Interfaces
{
    public interface IAuthService
    {
        Task<RequestLoginOtpResponseDto> RequestLoginOtpAsync(
            RequestLoginOtpRequestDto request, CancellationToken cancellationToken);
        Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken);
        Task<RegisterResponseDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken);
    }
}
