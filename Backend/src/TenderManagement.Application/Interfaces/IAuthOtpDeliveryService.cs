namespace TenderManagement.Application.Interfaces
{
    public interface IAuthOtpDeliveryService
    {
        bool IsAvailable { get; }
        Task SendAsync(string mobile, string code, CancellationToken cancellationToken);
    }
}
