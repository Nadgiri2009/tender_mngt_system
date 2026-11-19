using TenderManagement.Application.DTOs.Bid;

namespace TenderManagement.Application.Interfaces
{
    public interface IContractorApplicationService
    {
        Task<IReadOnlyList<TenderApplicationRequirementDto>> GetRequirementsAsync(
            int tenderId, bool publicOnly, CancellationToken cancellationToken);

        Task SetRequirementsAsync(
            int tenderId, int userId, IReadOnlyList<TenderApplicationRequirementDto> requirements,
            CancellationToken cancellationToken);

        Task<ContractorApplicationDetailDto> SaveDraftAsync(
            int tenderId, int userId, IReadOnlyList<ContractorApplicationFile> files,
            CancellationToken cancellationToken);

        Task<ContractorApplicationDetailDto> SubmitAsync(
            int applicationId, int userId, bool termsAccepted, string termsVersion,
            CancellationToken cancellationToken);

        Task<IReadOnlyList<ContractorApplicationListItemDto>> GetMyApplicationsAsync(
            int userId, CancellationToken cancellationToken);

        Task<IReadOnlyList<ContractorApplicationListItemDto>> GetApplicationsAsync(
            int userId, bool tenderCell, CancellationToken cancellationToken);

        Task<ContractorApplicationDashboardDto> GetDashboardAsync(
            int userId, CancellationToken cancellationToken);

        Task MarkNotificationReadAsync(long notificationId, int userId, CancellationToken cancellationToken);

        Task<ContractorApplicationDetailDto> GetDetailsAsync(
            int applicationId, int userId, bool contractor, bool tenderCell,
            CancellationToken cancellationToken);

        Task AddDocumentsAsync(
            int applicationId, int userId, bool contractor, bool tenderCell,
            IReadOnlyList<ContractorApplicationFile> files, CancellationToken cancellationToken);

        Task<ContractorApplicationDetailDto> AddRemarkAsync(
            int applicationId, int userId, bool contractor, bool tenderCell,
            AddContractorApplicationRemarkRequest request, CancellationToken cancellationToken);

        Task<ContractorApplicationDetailDto> ReviewDocumentAsync(
            int applicationId, int documentId, int userId, ReviewApplicationDocumentRequest request,
            CancellationToken cancellationToken);

        Task<ContractorApplicationDetailDto> ReviewApplicationAsync(
            int applicationId, int userId, bool tenderCell,
            ReviewContractorApplicationRequest request, CancellationToken cancellationToken);

        Task<(Stream Content, string FileName)> OpenDocumentAsync(
            int applicationId, int documentId, int userId, bool contractor, bool tenderCell,
            CancellationToken cancellationToken);
    }
}
