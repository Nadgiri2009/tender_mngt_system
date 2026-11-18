using System.Net;
using TenderManagement.Application.Common;
using TenderManagement.Application.Interfaces;

namespace TenderManagement.API.Services
{
    public sealed class DevelopmentOtpDeliveryService : IAuthOtpDeliveryService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DevelopmentOtpDeliveryService(
            IWebHostEnvironment environment,
            IHttpContextAccessor httpContextAccessor)
        {
            _environment = environment;
            _httpContextAccessor = httpContextAccessor;
        }

        public bool IsAvailable =>
            _environment.IsDevelopment() &&
            _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress is { } remoteIp &&
            IPAddress.IsLoopback(remoteIp.IsIPv4MappedToIPv6 ? remoteIp.MapToIPv4() : remoteIp);

        public bool CanExposeOtp => IsAvailable;

        public Task SendAsync(string mobile, string code, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsAvailable)
            {
                throw new OtpDeliveryUnavailableException();
            }

            return Task.CompletedTask;
        }
    }
}
