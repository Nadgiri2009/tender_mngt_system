namespace TenderManagement.Application.Interfaces
{
    public interface IAuditService
    {
        Task LogAsync(int? userId, string action, string entityName, int? entityId, string? details = null);
    }
}
