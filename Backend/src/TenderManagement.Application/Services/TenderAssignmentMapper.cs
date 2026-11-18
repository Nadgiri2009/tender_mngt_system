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
            => "Configured by workflow";
    }
}
