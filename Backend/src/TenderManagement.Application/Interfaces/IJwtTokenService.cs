using TenderManagement.Domain.Entities;

namespace TenderManagement.Application.Interfaces
{
    public interface IJwtTokenService
    {
        (string Token, DateTime ExpiresAt) GenerateToken(User user, string roleName);
    }
}
