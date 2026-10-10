using TenderManagement.Domain.Enums;

namespace TenderManagement.Application.Services
{
    /// <summary>
    /// Encodes the fixed tender workflow (Department Requirement -> ... -> Payment Release) as a
    /// deterministic state machine. Each entry describes, for a given CurrentStatus:
    ///   - the "Submit" forward move (plain hand-off, no approve/reject decision), and/or
    ///   - the "Approve" / "Reject" decision (made by a specific role) and where each leads.
    ///
    /// This keeps the full status sequence from the spec (Department Requirement through Payment,
    /// including the publication-approval-by-power-delegation gate and the independent CAFO/MCA
    /// financial sign-offs) auditable and centrally defined in one place,
    /// while WorkflowStages (DB table) lets Admin re-point which Role owns each approval stage
    /// without a code change (read by WorkflowService when checking permissions).
    /// </summary>
    public static class WorkflowEngine
    {
        public class Transition
        {
            public string ApprovalStage { get; init; } = string.Empty;

            // Plain forward submit (no approval decision) - null if this status has no submit step.
            public string? SubmitToStatus { get; init; }
            public string[] SubmitRoles { get; init; } = Array.Empty<string>();

            // Approval decision - null if this status is not awaiting an approval.
            public string? ApproveToStatus { get; init; }
            public string? RejectToStatus { get; init; } = TenderStatus.Rejected;
            public string[] ApproverRoles { get; init; } = Array.Empty<string>();
        }

        public static readonly Dictionary<string, Transition> Transitions = new()
        {
            [TenderStatus.Draft] = new Transition
            {
                ApprovalStage = "RequirementSubmission",
                SubmitToStatus = TenderStatus.RequirementSubmitted,
                SubmitRoles = new[] { RoleNames.DepartmentOfficer }
            },
            [TenderStatus.RequirementSubmitted] = new Transition
            {
                ApprovalStage = "EstimatePreparation",
                SubmitToStatus = TenderStatus.EstimatePrepared,
                SubmitRoles = new[] { RoleNames.DepartmentOfficer }
            },
            [TenderStatus.EstimatePrepared] = new Transition
            {
                ApprovalStage = "TechnicalSanction",
                SubmitToStatus = TenderStatus.TSApprovalPending,
                SubmitRoles = new[] { RoleNames.DepartmentOfficer }
            },
            [TenderStatus.TSApprovalPending] = new Transition
            {
                ApprovalStage = "TechnicalSanction",
                ApproveToStatus = TenderStatus.TSApproved,
                ApproverRoles = new[] { RoleNames.TechnicalSanctionOfficer }
            },
            [TenderStatus.TSApproved] = new Transition
            {
                ApprovalStage = "GBApproval",
                SubmitToStatus = TenderStatus.GBApprovalPending,
                SubmitRoles = new[] { RoleNames.DepartmentOfficer }
            },
            [TenderStatus.GBApprovalPending] = new Transition
            {
                ApprovalStage = "GBApproval",
                ApproveToStatus = TenderStatus.GBApproved,
                ApproverRoles = new[] { RoleNames.GBOfficer }
            },
            [TenderStatus.GBApproved] = new Transition
            {
                ApprovalStage = "AdministrativeApproval",
                SubmitToStatus = TenderStatus.AdministrativeApprovalPending,
                SubmitRoles = new[] { RoleNames.DepartmentOfficer }
            },
            [TenderStatus.AdministrativeApprovalPending] = new Transition
            {
                ApprovalStage = "AdministrativeApproval",
                ApproveToStatus = TenderStatus.AdministrativeApproved,
                ApproverRoles = new[] { RoleNames.DPDCOfficer }
            },
            [TenderStatus.AdministrativeApproved] = new Transition
            {
                ApprovalStage = "BudgetApproval",
                SubmitToStatus = TenderStatus.BudgetApprovalPending,
                SubmitRoles = new[] { RoleNames.DepartmentOfficer }
            },
            [TenderStatus.BudgetApprovalPending] = new Transition
            {
                ApprovalStage = "BudgetApproval",
                ApproveToStatus = TenderStatus.BudgetApproved,
                ApproverRoles = new[] { RoleNames.CAFOOfficer }
            },
            [TenderStatus.BudgetApproved] = new Transition
            {
                ApprovalStage = "TenderDraftPreparation",
                SubmitToStatus = TenderStatus.TenderDraftCreated,
                SubmitRoles = new[] { RoleNames.DepartmentOfficer }
            },
            [TenderStatus.TenderDraftCreated] = new Transition
            {
                ApprovalStage = "TenderCommitteeApproval",
                SubmitToStatus = TenderStatus.TenderCommitteeApproval,
                SubmitRoles = new[] { RoleNames.DepartmentOfficer }
            },
            [TenderStatus.TenderCommitteeApproval] = new Transition
            {
                ApprovalStage = "TenderCommitteeApproval",
                ApproveToStatus = TenderStatus.PublicationApprovalPending,
                ApproverRoles = new[] { RoleNames.TenderCommittee }
            },
            // Separate gate from the committee's documentation approval: publication itself is
            // released only by whichever authority the tender's value delegates it to (Competent
            // Authority by default here; WorkflowStages can repoint this per delegation-of-powers
            // rules without a code change).
            [TenderStatus.PublicationApprovalPending] = new Transition
            {
                ApprovalStage = "PublicationApproval",
                ApproveToStatus = TenderStatus.TenderPublished,
                ApproverRoles = new[] { RoleNames.CompetentAuthority }
            },
            [TenderStatus.TenderPublished] = new Transition
            {
                ApprovalStage = "TechnicalBidOpening",
                SubmitToStatus = TenderStatus.TechnicalBidOpening,
                SubmitRoles = new[] { RoleNames.TenderCommittee, RoleNames.DepartmentOfficer }
            },
            [TenderStatus.TechnicalBidOpening] = new Transition
            {
                ApprovalStage = "TechnicalEvaluation",
                SubmitToStatus = TenderStatus.TechnicalEvaluation,
                SubmitRoles = new[] { RoleNames.TenderCommittee }
            },
            [TenderStatus.TechnicalEvaluation] = new Transition
            {
                ApprovalStage = "TechnicalEvaluation",
                ApproveToStatus = TenderStatus.TechnicalEvaluationApproved,
                ApproverRoles = new[] { RoleNames.TenderSection }
            },
            [TenderStatus.TechnicalEvaluationApproved] = new Transition
            {
                ApprovalStage = "TechnicalEvaluationCommitteeApproval",
                ApproveToStatus = TenderStatus.FinancialBidOpening,
                ApproverRoles = new[] { RoleNames.TenderCommittee }
            },
            // Per the standard process, CAFO gives its rate opinion first; that opinion is an
            // input to the financial evaluation, not a check performed after it.
            [TenderStatus.FinancialBidOpening] = new Transition
            {
                ApprovalStage = "CAFORateOpinion",
                SubmitToStatus = TenderStatus.CAFORateOpinion,
                SubmitRoles = new[] { RoleNames.TenderCommittee }
            },
            [TenderStatus.CAFORateOpinion] = new Transition
            {
                ApprovalStage = "CAFORateOpinion",
                ApproveToStatus = TenderStatus.FinancialEvaluation,
                ApproverRoles = new[] { RoleNames.CAFOOfficer }
            },
            [TenderStatus.FinancialEvaluation] = new Transition
            {
                ApprovalStage = "MCAOpinion",
                SubmitToStatus = TenderStatus.MCAOpinion,
                SubmitRoles = new[] { RoleNames.TenderCommittee }
            },
            // MCA (Municipal Commissioner's Approval) is a distinct, independent sign-off from
            // CAFO's rate opinion above -- it must not be closed out by the same role that gave
            // the rate opinion, or the check is meaningless.
            [TenderStatus.MCAOpinion] = new Transition
            {
                ApprovalStage = "MCAOpinion",
                ApproveToStatus = TenderStatus.TenderCommitteeRecommendation,
                ApproverRoles = new[] { RoleNames.MCAOfficer, RoleNames.Admin }
            },
            [TenderStatus.TenderCommitteeRecommendation] = new Transition
            {
                ApprovalStage = "TenderCommitteeRecommendation",
                ApproveToStatus = TenderStatus.FinalApproval,
                ApproverRoles = new[] { RoleNames.TenderCommittee }
            },
            [TenderStatus.FinalApproval] = new Transition
            {
                ApprovalStage = "FinalAuthorityApproval",
                ApproveToStatus = TenderStatus.AgreementCompleted,
                ApproverRoles = new[] { RoleNames.CompetentAuthority }
            },
            [TenderStatus.AgreementCompleted] = new Transition
            {
                ApprovalStage = "WorkOrder",
                SubmitToStatus = TenderStatus.WorkOrderGenerated,
                SubmitRoles = new[] { RoleNames.DepartmentOfficer, RoleNames.Contractor }
            },
            [TenderStatus.WorkOrderGenerated] = new Transition
            {
                ApprovalStage = "WorkExecution",
                SubmitToStatus = TenderStatus.WorkStarted,
                SubmitRoles = new[] { RoleNames.Contractor, RoleNames.DepartmentOfficer }
            },
            [TenderStatus.WorkStarted] = new Transition
            {
                ApprovalStage = "MeasurementBook",
                SubmitToStatus = TenderStatus.MeasurementBookUpdated,
                SubmitRoles = new[] { RoleNames.Contractor, RoleNames.DepartmentOfficer }
            },
            [TenderStatus.MeasurementBookUpdated] = new Transition
            {
                ApprovalStage = "BillSubmission",
                SubmitToStatus = TenderStatus.BillSubmitted,
                SubmitRoles = new[] { RoleNames.Contractor }
            },
            [TenderStatus.BillSubmitted] = new Transition
            {
                ApprovalStage = "BillApproval",
                ApproveToStatus = TenderStatus.BillApproved,
                ApproverRoles = new[] { RoleNames.CAFOOfficer }
            },
            [TenderStatus.BillApproved] = new Transition
            {
                ApprovalStage = "PaymentRelease",
                SubmitToStatus = TenderStatus.PaymentCompleted,
                SubmitRoles = new[] { RoleNames.CAFOOfficer, RoleNames.Admin }
            },
        };

        public static bool IsRoleAllowed(string[] allowedRoles, string roleName) =>
            allowedRoles.Contains(roleName) || roleName == RoleNames.Admin;
    }
}
