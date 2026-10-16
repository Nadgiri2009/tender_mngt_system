namespace TenderManagement.Application.Interfaces
{
    /// <summary>Aggregates repositories so a service can commit several changes in one transaction.</summary>
    public interface IUnitOfWork : IDisposable
    {
        ITenderRepository Tenders { get; }
        IGenericRepository<Domain.Entities.User> Users { get; }
        IGenericRepository<Domain.Entities.Role> Roles { get; }
        IGenericRepository<Domain.Entities.Department> Departments { get; }
        IGenericRepository<Domain.Entities.Estimate> Estimates { get; }
        IGenericRepository<Domain.Entities.ApprovalHistory> ApprovalHistories { get; }
        IGenericRepository<Domain.Entities.TenderDocument> TenderDocuments { get; }
        IGenericRepository<Domain.Entities.BOQItem> BOQItems { get; }
        IGenericRepository<Domain.Entities.Contractor> Contractors { get; }
        IGenericRepository<Domain.Entities.TechnicalBid> TechnicalBids { get; }
        IGenericRepository<Domain.Entities.TechnicalBidDocument> TechnicalBidDocuments { get; }
        IGenericRepository<Domain.Entities.FinancialBid> FinancialBids { get; }
        IGenericRepository<Domain.Entities.Agreement> Agreements { get; }
        IGenericRepository<Domain.Entities.WorkOrder> WorkOrders { get; }
        IGenericRepository<Domain.Entities.WorkExecution> WorkExecutions { get; }
        IGenericRepository<Domain.Entities.MeasurementBook> MeasurementBooks { get; }
        IGenericRepository<Domain.Entities.Bill> Bills { get; }
        IGenericRepository<Domain.Entities.Payment> Payments { get; }
        IGenericRepository<Domain.Entities.AuditLog> AuditLogs { get; }
        IGenericRepository<Domain.Entities.WorkflowStage> WorkflowStages { get; }
        IGenericRepository<Domain.Entities.Announcement> Announcements { get; }

        Task<int> CompleteAsync();
        Task ExecuteWorkflowTransitionAsync(int tenderId, string approvalStage, string newStatus,
            string actionStatus, int approvedBy, string? roleAtAction, string? remarks);
    }
}
