using TenderManagement.Domain.Entities;

namespace TenderManagement.Application.Services
{
    public static class TenderAssignmentMapper
    {
        public static string GetAssignedRespOfficer(Tender tender)
        {
            var fullName = tender.CreatedByUser?.FullName;
            return string.IsNullOrWhiteSpace(fullName) ? "Unassigned" : fullName;
        }

        public static string GetAssignedRole(string currentStatus)
        {
            if (!WorkflowEngine.Transitions.TryGetValue(currentStatus, out var transition))
            {
                return "No pending role";
            }

            var roles = transition.ApproverRoles.Length > 0
                ? transition.ApproverRoles
                : transition.SubmitRoles;

            return roles.Length > 0 ? string.Join(", ", roles) : "No pending role";
        }
    }
}
