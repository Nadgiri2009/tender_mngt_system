namespace TenderManagement.Domain.Enums
{
    /// <summary>
    /// All statuses a Tender can move through. Stored as string on Tender.CurrentStatus
    /// (not as an int enum) so new statuses can be added via WorkflowStages without a
    /// schema migration, but this enum gives compile-time safety in C# code.
    /// </summary>
    public static class TenderStatus
    {
        public const string Draft = "Draft";
        public const string RequirementSubmitted = "Requirement Submitted";
        public const string EstimatePrepared = "Estimate Prepared";
        public const string TSApprovalPending = "TS Approval Pending";
        public const string TSApproved = "TS Approved";
        public const string GBApprovalPending = "GB Approval Pending";
        public const string GBApproved = "GB Approved";
        public const string AdministrativeApprovalPending = "Administrative Approval Pending";
        public const string AdministrativeApproved = "Administrative Approved";
        public const string BudgetApprovalPending = "Budget Approval Pending";
        public const string BudgetApproved = "Budget Approved";
        public const string TenderDraftCreated = "Tender Draft Created";
        public const string TenderCommitteeApproval = "Tender Committee Approval";
        public const string PublicationApprovalPending = "Publication Approval Pending";
        public const string TenderPublished = "Tender Published";
        public const string PrimaryVerificationPending = "Primary Verification Pending";
        public const string ShortfallPending = "Shortfall Pending";
        public const string ShortfallDocumentsSubmitted = "Shortfall Documents Submitted";
        public const string FinancialBidOpeningApprovalPending = "Financial Bid Opening Approval Pending";
        public const string FinancialBidOpeningApproved = "Financial Bid Opening Approved";
        public const string FinalFinancialApprovalPending = "Final Financial Approval Pending";
        public const string FinalFinancialApprovalCompleted = "Final Financial Approval Completed";
        public const string FinalFinancialApprovalRejected = "Final Financial Approval Rejected";
        public const string TenderAgreementGenerated = "Tender Agreement Generated";
        public const string TechnicalBidOpening = "Technical Bid Opening";
        public const string TechnicalEvaluation = "Technical Evaluation";
        public const string TechnicalEvaluationApproved = "Technical Evaluation Approved";
        public const string FinancialBidOpening = "Financial Bid Opening";
        public const string FinancialEvaluation = "Financial Evaluation";
        public const string CAFORateOpinion = "CAFO Rate Opinion";
        public const string MCAOpinion = "MCA Opinion";
        public const string TenderCommitteeRecommendation = "Tender Committee Recommendation";
        public const string FinalApproval = "Final Approval";
        public const string AgreementCompleted = "Agreement Completed";
        public const string WorkOrderGenerated = "Work Order Generated";
        public const string WorkStarted = "Work Started";
        public const string MeasurementBookUpdated = "Measurement Book Updated";
        public const string BillSubmitted = "Bill Submitted";
        public const string BillApproved = "Bill Approved";
        public const string PaymentCompleted = "Payment Completed";
        public const string Rejected = "Rejected";
    }

    public static class TenderType
    {
        public const string Open = "Open";
        public const string Limited = "Limited";
        public const string Single = "Single";
        public const string EOI = "EOI";
    }

    public static class TenderStatusBuckets
    {
        public static bool IsApproved(string status) =>
            status.Contains("Approved", StringComparison.OrdinalIgnoreCase) ||
            status is TenderStatus.TenderPublished or TenderStatus.FinalApproval or TenderStatus.AgreementCompleted
                or TenderStatus.WorkOrderGenerated or TenderStatus.TenderAgreementGenerated
                or TenderStatus.FinalFinancialApprovalCompleted or TenderStatus.WorkStarted or TenderStatus.MeasurementBookUpdated
                or TenderStatus.PaymentCompleted;

        public static bool IsPending(string status) =>
            status.Contains("Pending", StringComparison.OrdinalIgnoreCase) ||
            status is TenderStatus.RequirementSubmitted or TenderStatus.EstimatePrepared or TenderStatus.TenderDraftCreated
                or TenderStatus.BillSubmitted;

        public static bool IsRejected(string status) =>
            status.Equals(TenderStatus.Rejected, StringComparison.OrdinalIgnoreCase);

        public static bool IsUnderReview(string status) =>
            status is TenderStatus.TenderCommitteeApproval or TenderStatus.TechnicalBidOpening
                or TenderStatus.TechnicalEvaluation or TenderStatus.FinancialBidOpening or TenderStatus.FinancialEvaluation
                or TenderStatus.CAFORateOpinion or TenderStatus.MCAOpinion or TenderStatus.TenderCommitteeRecommendation;
    }

    public static class RoleNames
    {
        public const string JuniorEngineer = "Junior Engineer (JE)";
        public const string TenderCell = "Tender Cell";
        public const string TenderCommitteeChairman = "Tender Committee Chairman (Nivida Samiti Adhyaksha)";
        public const string DirectorAuditor = "Director Auditor";
        public const string ExternalBidder = "ExternalBidder";
        public const string Disabled = "LegacyRoleDisabled";

        public const string Admin = Disabled;
        public const string DepartmentOfficer = JuniorEngineer;
        public const string TenderSection = TenderCell;
        public const string TenderCommittee = TenderCommitteeChairman;
        public const string CompetentAuthority = DirectorAuditor;
        public const string Contractor = ExternalBidder;
        public const string TechnicalSanctionOfficer = Disabled;
        public const string GBOfficer = Disabled;
        public const string DPDCOfficer = Disabled;
        public const string CAFOOfficer = Disabled;
        public const string MCAOfficer = Disabled;
    }
}
