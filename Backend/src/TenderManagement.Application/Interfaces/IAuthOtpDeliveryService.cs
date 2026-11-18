namespace TenderManagement.Application.Interfaces
{
    public interface IAuthOtpDeliveryService
    {
        bool IsAvailable { get; }
        bool CanExposeOtp { get; }
        Task SendAsync(string mobile, string code, CancellationToken cancellationToken);
    }
}
