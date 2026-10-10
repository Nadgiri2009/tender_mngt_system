using System;
using System.Collections.Generic;
using System.Linq;

namespace DepartmentModule.Helpers
{
    public sealed class WorkflowTransition
    {
        public string Stage { get; set; }
        public string NextStatus { get; set; }
        public string RejectStatus { get; set; }
        public string[] SubmitRoles { get; set; }
        public string[] ApproverRoles { get; set; }
    }

    public static class WorkflowRules
    {
        private static readonly Dictionary<string, WorkflowTransition> Transitions =
            new Dictionary<string, WorkflowTransition>(StringComparer.OrdinalIgnoreCase)
            {
                { "Draft", Submit("RequirementSubmission", "Requirement Submitted", "DepartmentOfficer") },
                { "Requirement Submitted", Submit("EstimatePreparation", "Estimate Prepared", "DepartmentOfficer") },
                { "Estimate Prepared", Submit("TechnicalSanction", "TS Approval Pending", "DepartmentOfficer") },
                { "TS Approval Pending", Approve("TechnicalSanction", "TS Approved", "TechnicalSanctionOfficer") },
                { "TS Approved", Submit("GBApproval", "GB Approval Pending", "DepartmentOfficer") },
                { "GB Approval Pending", Approve("GBApproval", "GB Approved", "GBOfficer") },
                { "GB Approved", Submit("AdministrativeApproval", "Administrative Approval Pending", "DepartmentOfficer") },
                { "Administrative Approval Pending", Approve("AdministrativeApproval", "Administrative Approved", "DPDCOfficer") },
                { "Administrative Approved", Submit("BudgetApproval", "Budget Approval Pending", "DepartmentOfficer") },
                { "Budget Approval Pending", Approve("BudgetApproval", "Budget Approved", "CAFOOfficer") },
                { "Budget Approved", Submit("TenderDraftPreparation", "Tender Draft Created", "DepartmentOfficer") },
                { "Tender Draft Created", Submit("TenderCommitteeApproval", "Tender Committee Approval", "DepartmentOfficer") },
                { "Tender Committee Approval", Approve("TenderCommitteeApproval", "Publication Approval Pending", "TenderCommittee") },
                { "Publication Approval Pending", Approve("PublicationApproval", "Tender Published", "CompetentAuthority") },
                { "Tender Published", Submit("TechnicalBidOpening", "Technical Bid Opening", "TenderCommittee", "DepartmentOfficer") },
                { "Technical Bid Opening", Submit("TechnicalEvaluation", "Technical Evaluation", "TenderCommittee") },
                { "Technical Evaluation", Approve("TechnicalEvaluation", "Technical Evaluation Approved", "TenderSection") },
                { "Technical Evaluation Approved", Approve("TechnicalEvaluationCommitteeApproval", "Financial Bid Opening", "TenderCommittee") },
                { "Financial Bid Opening", Submit("CAFORateOpinion", "CAFO Rate Opinion", "TenderCommittee") },
                { "CAFO Rate Opinion", Approve("CAFORateOpinion", "Financial Evaluation", "CAFOOfficer") },
                { "Financial Evaluation", Submit("MCAOpinion", "MCA Opinion", "TenderCommittee") },
                { "MCA Opinion", Approve("MCAOpinion", "Tender Committee Recommendation", "MCAOfficer", "Admin") },
                { "Tender Committee Recommendation", Approve("TenderCommitteeRecommendation", "Final Approval", "TenderCommittee") },
                { "Final Approval", Approve("FinalAuthorityApproval", "Agreement Completed", "CompetentAuthority") },
                { "Agreement Completed", Submit("WorkOrder", "Work Order Generated", "DepartmentOfficer", "Contractor") },
                { "Work Order Generated", Submit("WorkExecution", "Work Started", "Contractor", "DepartmentOfficer") },
                { "Work Started", Submit("MeasurementBook", "Measurement Book Updated", "Contractor", "DepartmentOfficer") },
                { "Measurement Book Updated", Submit("BillSubmission", "Bill Submitted", "Contractor") },
                { "Bill Submitted", Approve("BillApproval", "Bill Approved", "CAFOOfficer") },
                { "Bill Approved", Submit("PaymentRelease", "Payment Completed", "CAFOOfficer", "Admin") }
            };

        private static WorkflowTransition Submit(string stage, string next, params string[] roles)
        {
            return new WorkflowTransition { Stage = stage, NextStatus = next, SubmitRoles = roles, ApproverRoles = new string[0] };
        }

        private static WorkflowTransition Approve(string stage, string next, params string[] roles)
        {
            return new WorkflowTransition { Stage = stage, NextStatus = next, RejectStatus = "Rejected", SubmitRoles = new string[0], ApproverRoles = roles };
        }

        public static WorkflowTransition ForStatus(string status)
        {
            WorkflowTransition transition;
            return status != null && Transitions.TryGetValue(status, out transition) ? transition : null;
        }

        public static bool RoleAllowed(string[] roles, string role)
        {
            return String.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase) ||
                   roles.Any(permitted => String.Equals(permitted, role, StringComparison.OrdinalIgnoreCase));
        }

        public static string[] PendingStatusesForRole(string role)
        {
            return Transitions.Where(pair => pair.Value.ApproverRoles.Length > 0 &&
                    RoleAllowed(pair.Value.ApproverRoles, role))
                .Select(pair => pair.Key).ToArray();
        }

        public static string[] Statuses
        {
            get
            {
                string[] result = Transitions.Keys.Concat(new[] { "Rejected" }).ToArray();
                Array.Sort(result, StringComparer.OrdinalIgnoreCase);
                return result;
            }
        }
    }
}
