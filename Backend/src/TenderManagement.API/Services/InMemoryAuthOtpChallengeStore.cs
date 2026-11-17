using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using TenderManagement.Application.Interfaces;

namespace TenderManagement.API.Services
{
    public sealed class InMemoryAuthOtpChallengeStore : IAuthOtpChallengeStore
    {
        private static readonly TimeSpan RequestWindow = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan RequestCooldown = TimeSpan.FromSeconds(60);
        private const int MaxRequestsPerWindow = 3;
        private const int MaxVerificationAttempts = 5;

        private readonly ConcurrentDictionary<string, ChallengeEntry> _entries =
            new(StringComparer.Ordinal);

        public bool TryIssue(
            string mobile, int? userId, string code, DateTimeOffset now, DateTimeOffset expiresAt)
        {
            var entry = _entries.GetOrAdd(mobile, static _ => new ChallengeEntry());
            lock (entry.SyncRoot)
            {
                while (entry.Requests.TryPeek(out var requestedAt) && requestedAt <= now - RequestWindow)
                {
                    entry.Requests.Dequeue();
                }

                if (entry.Requests.Count >= MaxRequestsPerWindow ||
                    (entry.LastRequestedAt is { } lastRequestedAt &&
                     now - lastRequestedAt < RequestCooldown))
                {
                    return false;
                }

                entry.Requests.Enqueue(now);
                entry.LastRequestedAt = now;
                entry.UserId = userId;
                entry.Salt = RandomNumberGenerator.GetBytes(16);
                entry.CodeHash = HashCode(entry.Salt, code);
                entry.ExpiresAt = expiresAt;
                entry.Attempts = 0;
                return true;
            }
        }

        public bool TryVerify(string mobile, string code, DateTimeOffset now, out int userId)
        {
            userId = 0;
            if (!_entries.TryGetValue(mobile, out var entry))
            {
                return false;
            }

            lock (entry.SyncRoot)
            {
                if (entry.UserId is null ||
                    entry.Salt is null ||
                    entry.CodeHash is null ||
                    entry.ExpiresAt <= now ||
                    entry.Attempts >= MaxVerificationAttempts)
                {
                    ClearChallenge(entry);
                    return false;
                }

                entry.Attempts++;
                var candidateHash = HashCode(entry.Salt, code);
                if (CryptographicOperations.FixedTimeEquals(candidateHash, entry.CodeHash))
                {
                    userId = entry.UserId.Value;
                    ClearChallenge(entry);
                    return true;
                }

                if (entry.Attempts >= MaxVerificationAttempts)
                {
                    ClearChallenge(entry);
                }

                return false;
            }
        }

        private static byte[] HashCode(byte[] salt, string code)
        {
            var codeBytes = Encoding.UTF8.GetBytes(code);
            var input = new byte[salt.Length + codeBytes.Length];
            Buffer.BlockCopy(salt, 0, input, 0, salt.Length);
            Buffer.BlockCopy(codeBytes, 0, input, salt.Length, codeBytes.Length);
            return SHA256.HashData(input);
        }

        private static void ClearChallenge(ChallengeEntry entry)
        {
            entry.UserId = null;
            entry.Salt = null;
            entry.CodeHash = null;
            entry.ExpiresAt = null;
            entry.Attempts = 0;
        }

        private sealed class ChallengeEntry
        {
            public object SyncRoot { get; } = new();
            public Queue<DateTimeOffset> Requests { get; } = new();
            public DateTimeOffset? LastRequestedAt { get; set; }
            public int? UserId { get; set; }
            public byte[]? Salt { get; set; }
            public byte[]? CodeHash { get; set; }
            public DateTimeOffset? ExpiresAt { get; set; }
            public int Attempts { get; set; }
        }
    }
}
