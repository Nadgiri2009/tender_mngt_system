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
}
