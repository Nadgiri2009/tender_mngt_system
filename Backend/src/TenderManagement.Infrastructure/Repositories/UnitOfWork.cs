using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TenderManagement.Application.Interfaces;
using TenderManagement.Domain.Entities;
using TenderManagement.Infrastructure.Data;

namespace TenderManagement.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
            Tenders = new TenderRepository(context);
            Users = new GenericRepository<User>(context);
            Roles = new GenericRepository<Role>(context);
            Departments = new GenericRepository<Department>(context);
            Estimates = new GenericRepository<Estimate>(context);
            ApprovalHistories = new GenericRepository<ApprovalHistory>(context);
            TenderDocuments = new GenericRepository<TenderDocument>(context);
            BOQItems = new GenericRepository<BOQItem>(context);
            Contractors = new GenericRepository<Contractor>(context);
            TechnicalBids = new GenericRepository<TechnicalBid>(context);
            TechnicalBidDocuments = new GenericRepository<TechnicalBidDocument>(context);
            FinancialBids = new GenericRepository<FinancialBid>(context);
            Agreements = new GenericRepository<Agreement>(context);
            WorkOrders = new GenericRepository<WorkOrder>(context);
            WorkExecutions = new GenericRepository<WorkExecution>(context);
            MeasurementBooks = new GenericRepository<MeasurementBook>(context);
            Bills = new GenericRepository<Bill>(context);
            Payments = new GenericRepository<Payment>(context);
            AuditLogs = new GenericRepository<AuditLog>(context);
            WorkflowStages = new GenericRepository<WorkflowStage>(context);
            Announcements = new GenericRepository<Announcement>(context);
        }

        public ITenderRepository Tenders { get; }
        public IGenericRepository<User> Users { get; }
        public IGenericRepository<Role> Roles { get; }
        public IGenericRepository<Department> Departments { get; }
        public IGenericRepository<Estimate> Estimates { get; }
        public IGenericRepository<ApprovalHistory> ApprovalHistories { get; }
        public IGenericRepository<TenderDocument> TenderDocuments { get; }
        public IGenericRepository<BOQItem> BOQItems { get; }
        public IGenericRepository<Contractor> Contractors { get; }
        public IGenericRepository<TechnicalBid> TechnicalBids { get; }
        public IGenericRepository<TechnicalBidDocument> TechnicalBidDocuments { get; }
        public IGenericRepository<FinancialBid> FinancialBids { get; }
        public IGenericRepository<Agreement> Agreements { get; }
        public IGenericRepository<WorkOrder> WorkOrders { get; }
        public IGenericRepository<WorkExecution> WorkExecutions { get; }
        public IGenericRepository<MeasurementBook> MeasurementBooks { get; }
        public IGenericRepository<Bill> Bills { get; }
        public IGenericRepository<Payment> Payments { get; }
        public IGenericRepository<AuditLog> AuditLogs { get; }
        public IGenericRepository<WorkflowStage> WorkflowStages { get; }
        public IGenericRepository<Announcement> Announcements { get; }

        public async Task<int> CompleteAsync() => await _context.SaveChangesAsync();

        /// <summary>
        /// Executes the workflow transition through the SQL stored procedure sp_UpdateTenderStatus,
        /// which updates Tender.CurrentStatus and inserts an ApprovalHistory row in a single
        /// database transaction. Using a raw SQL call here (rather than two separate EF SaveChanges)
        /// guarantees atomicity for the most safety-critical operation in the system.
        /// </summary>
        public async Task ExecuteWorkflowTransitionAsync(int tenderId, string approvalStage, string newStatus,
            string actionStatus, int approvedBy, string? roleAtAction, string? remarks)
        {
            var tenderIdParam = new SqlParameter("@TenderId", tenderId);
            var stageParam = new SqlParameter("@ApprovalStage", approvalStage);
            var newStatusParam = new SqlParameter("@NewStatus", newStatus);
            var actionStatusParam = new SqlParameter("@ActionStatus", actionStatus);
            var approvedByParam = new SqlParameter("@ApprovedBy", approvedBy);
            var roleParam = new SqlParameter("@RoleAtAction", roleAtAction ?? (object)DBNull.Value);
            var remarksParam = new SqlParameter("@Remarks", remarks ?? (object)DBNull.Value);

            await _context.Database.ExecuteSqlRawAsync(
                "EXEC sp_UpdateTenderStatus @TenderId, @ApprovalStage, @NewStatus, @ActionStatus, @ApprovedBy, @RoleAtAction, @Remarks",
                tenderIdParam, stageParam, newStatusParam, actionStatusParam, approvedByParam, roleParam, remarksParam);

            // Keep the EF change-tracker in sync in case the same Tender instance is reused later
            // in this request (e.g. when returning the updated entity to the controller).
            var tracked = _context.ChangeTracker.Entries<Tender>().FirstOrDefault(e => e.Entity.TenderId == tenderId);
            if (tracked is not null)
            {
                await _context.Entry(tracked.Entity).ReloadAsync();
            }
        }

        public void Dispose() => _context.Dispose();
    }
}
