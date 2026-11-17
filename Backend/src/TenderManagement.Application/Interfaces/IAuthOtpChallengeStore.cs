namespace TenderManagement.Application.Interfaces
{
    public interface IAuthOtpChallengeStore
    {
        bool TryIssue(string mobile, int? userId, string code, DateTimeOffset now, DateTimeOffset expiresAt);
        bool TryVerify(string mobile, string code, DateTimeOffset now, out int userId);
    }
}
