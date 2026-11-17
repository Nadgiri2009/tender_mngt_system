using System.Net;
using TenderManagement.Application.Common;
using TenderManagement.Application.Interfaces;

namespace TenderManagement.API.Services
{
    public sealed class DevelopmentConsoleOtpDeliveryService : IAuthOtpDeliveryService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<DevelopmentConsoleOtpDeliveryService> _logger;

        public DevelopmentConsoleOtpDeliveryService(
            IWebHostEnvironment environment,
            IHttpContextAccessor httpContextAccessor,
            ILogger<DevelopmentConsoleOtpDeliveryService> logger)
        {
            _environment = environment;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public bool IsAvailable =>
            _environment.IsDevelopment() &&
            _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress is { } remoteIp &&
            IPAddress.IsLoopback(remoteIp.IsIPv4MappedToIPv6 ? remoteIp.MapToIPv4() : remoteIp);

        public Task SendAsync(string mobile, string code, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsAvailable)
            {
                throw new OtpDeliveryUnavailableException();
            }

            _logger.LogInformation(
                "Development sign-in OTP for mobile ending {MobileSuffix}: {OtpCode}",
                mobile[^4..],
                code);
            return Task.CompletedTask;
        }
    }
}
