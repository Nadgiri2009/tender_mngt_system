using TenderManagement.Domain.Entities;
using TenderManagement.Domain.Enums;

namespace TenderManagement.Application.Services
{
    public static class TenderApplicationRules
    {
        public const string TermsVersion = "portal-terms-v1";

        public static bool CanApply(Tender tender, DateTime utcNow)
        {
            return tender.CurrentStatus == TenderStatus.TenderPublished
                && tender.SubmissionStartDate.HasValue
                && tender.SubmissionDeadline.HasValue
                && utcNow >= tender.SubmissionStartDate.Value
                && utcNow <= tender.SubmissionDeadline.Value;
        }

        public static string GetApplicationStatus(Tender tender, DateTime utcNow)
        {
            if (tender.CurrentStatus != TenderStatus.TenderPublished)
            {
                return "Not publicly available";
            }

            if (!tender.SubmissionStartDate.HasValue || !tender.SubmissionDeadline.HasValue)
            {
                return "Application schedule not configured";
            }

            if (utcNow < tender.SubmissionStartDate.Value)
            {
                return "Applications not yet open";
            }

            if (utcNow > tender.SubmissionDeadline.Value)
            {
                return "Applications closed";
            }

            return "Applications open";
        }
    }
}
