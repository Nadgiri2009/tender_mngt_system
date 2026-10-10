using TenderManagement.Application.Interfaces;
using TenderManagement.Domain.Entities;
using TenderManagement.Infrastructure.Data;

namespace TenderManagement.Infrastructure.Services
{
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;

        public AuditService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(int? userId, string action, string entityName, int? entityId, string? details = null)
        {
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                Details = details,
                CreatedDate = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }
    }
}
