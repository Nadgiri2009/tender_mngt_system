namespace TenderManagement.Application.DTOs.Bid
{
    /// <summary>A tender-specific document a contractor may upload with an application.</summary>
    public sealed record TenderApplicationRequirementDto(
        string DocumentType,
        string? Description,
        bool IsMandatory,
        string? AllowedExtensions,
        int MaxFileSizeBytes);

    /// <summary>A document file sent with a contractor application request.</summary>
    public sealed record ContractorApplicationFile(string DocumentType, string FileName, Stream Content, long Length);

    /// <summary>A contractor application shown in dashboard and review lists.</summary>
    public sealed record ContractorApplicationListItemDto(
        int TechnicalBidId,
        int TenderId,
        string TenderNumber,
        string TenderTitle,
        int ContractorId,
        string ContractorName,
        string ApplicationReference,
        string Status,
        DateTime CreatedAtUtc,
        DateTime? SubmittedAtUtc,
        int DocumentCount,
        int PendingMandatoryDocumentCount);

    /// <summary>A private application document available only to its owner or authorized reviewers.</summary>
    public sealed record ContractorApplicationDocumentDto(
        int DocumentId,
        string DocumentType,
        string FileName,
        DateTime UploadedAtUtc,
        string UploadedByName,
        string? VerificationStatus,
        string? VerificationRemarks,
        DateTime? VerifiedAtUtc);

    /// <summary>An immutable remark or clarification associated with an application.</summary>
    public sealed record ContractorApplicationRemarkDto(
        long RemarkId,
        string RemarkType,
        string Message,
        string AuthorName,
        string? AuthorRole,
        bool VisibleToContractor,
        DateTime CreatedAtUtc);

    /// <summary>A recorded application status or review event.</summary>
    public sealed record ContractorApplicationHistoryDto(
        long HistoryId,
        string EventType,
        string? PreviousStatus,
        string? NewStatus,
        string? Remarks,
        string AuthorName,
        string? AuthorRole,
        bool VisibleToContractor,
        DateTime CreatedAtUtc);

    /// <summary>Full application information returned to its contractor or an authorized SMC reviewer.</summary>
    public sealed record ContractorApplicationDetailDto(
        int TechnicalBidId,
        int TenderId,
        string TenderNumber,
        string TenderTitle,
        string DepartmentName,
        decimal EstimatedAmount,
        string ContractorName,
        string? ContractorRepresentativeName,
        string? ContractorEmail,
        string? ContractorMobile,
        string? ContractorAddress,
        string ApplicationReference,
        string Status,
        DateTime CreatedAtUtc,
        DateTime? SubmittedAtUtc,
        IReadOnlyList<TenderApplicationRequirementDto> Requirements,
        IReadOnlyList<ContractorApplicationDocumentDto> Documents,
        IReadOnlyList<ContractorApplicationRemarkDto> Remarks,
        IReadOnlyList<ContractorApplicationHistoryDto> History);

    /// <summary>A request to add an immutable contractor-facing or internal remark.</summary>
    public sealed record AddContractorApplicationRemarkRequest(string Message, string RemarkType);

    /// <summary>Terms acceptance required to finalize a contractor application.</summary>
    public sealed record SubmitContractorApplicationRequest(bool TermsAccepted, string TermsVersion);

    /// <summary>A document verification decision submitted by an assigned reviewer.</summary>
    public sealed record ReviewApplicationDocumentRequest(string VerificationStatus, string? Remarks);

    /// <summary>A technical application decision submitted by an authorized reviewer.</summary>
    public sealed record ReviewContractorApplicationRequest(string Decision, string? Remarks);

    /// <summary>A portal notification delivered to a contractor account.</summary>
    public sealed record ContractorNotificationDto(
        long NotificationId,
        int TenderId,
        string TenderNumber,
        string Message,
        string NotificationType,
        DateTime CreatedAtUtc,
        DateTime? ReadAtUtc);

    /// <summary>Contractor dashboard counts and the contractor's own applications.</summary>
    public sealed record ContractorApplicationDashboardDto(
        string ContractorName,
        string CompanyName,
        int TotalApplications,
        int DraftApplications,
        int SubmittedApplications,
        int UnderVerificationApplications,
        int ShortfallApplications,
        int ApprovedApplications,
        int RejectedApplications,
        IReadOnlyList<ContractorNotificationDto> Notifications,
        IReadOnlyList<ContractorApplicationListItemDto> Applications);
}
