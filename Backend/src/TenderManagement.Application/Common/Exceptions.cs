namespace TenderManagement.Application.Common
{
    /// <summary>Thrown for expected business-rule violations; caught by the global exception middleware -> HTTP 400.</summary>
    public class BusinessRuleException : Exception
    {
        public BusinessRuleException(string message) : base(message) { }
    }

    /// <summary>Thrown when a requested entity does not exist; caught by middleware -> HTTP 404.</summary>
    public class NotFoundException : Exception
    {
        public NotFoundException(string entity, object key) : base($"{entity} with id '{key}' was not found.") { }
    }

    /// <summary>Thrown when the current user's role does not permit the requested workflow action; caught -> HTTP 403.</summary>
    public class ForbiddenActionException : Exception
    {
        public ForbiddenActionException(string message) : base(message) { }
    }

    /// <summary>Thrown when another sign-in code cannot be issued yet.</summary>
    public sealed class OtpRequestRateLimitException : Exception
    {
        public OtpRequestRateLimitException(int retryAfterSeconds)
            : base($"A sign-in code was requested recently. Please wait {retryAfterSeconds} seconds before trying again.")
        {
            RetryAfterSeconds = retryAfterSeconds;
        }

        public int RetryAfterSeconds { get; }
    }
}
