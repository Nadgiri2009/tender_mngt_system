using Microsoft.EntityFrameworkCore;
using TenderManagement.Domain.Entities;

namespace TenderManagement.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Role> Roles => Set<Role>();
        public DbSet<Permission> Permissions => Set<Permission>();
        public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
        public DbSet<Department> Departments => Set<Department>();
        public DbSet<DesignationMaster> Designations => Set<DesignationMaster>();
        public DbSet<User> Users => Set<User>();
        public DbSet<Contractor> Contractors => Set<Contractor>();
        public DbSet<Tender> Tenders => Set<Tender>();
        public DbSet<TenderMethod> TenderMethods => Set<TenderMethod>();
        public DbSet<TenderCategoryMaster> TenderCategories => Set<TenderCategoryMaster>();
        public DbSet<DocumentTypeMaster> DocumentTypes => Set<DocumentTypeMaster>();
        public DbSet<UnitMaster> Units => Set<UnitMaster>();
        public DbSet<Estimate> Estimates => Set<Estimate>();
        public DbSet<ApprovalHistory> ApprovalHistories => Set<ApprovalHistory>();
        public DbSet<TenderDocument> TenderDocuments => Set<TenderDocument>();
        public DbSet<BOQItem> BOQItems => Set<BOQItem>();
        public DbSet<TechnicalBid> TechnicalBids => Set<TechnicalBid>();
        public DbSet<TechnicalBidDocument> TechnicalBidDocuments => Set<TechnicalBidDocument>();
        public DbSet<TenderRequiredBidDocument> TenderRequiredBidDocuments => Set<TenderRequiredBidDocument>();
        public DbSet<ContractorApplicationHistory> ContractorApplicationHistory => Set<ContractorApplicationHistory>();
        public DbSet<ContractorApplicationRemark> ContractorApplicationRemarks => Set<ContractorApplicationRemark>();
        public DbSet<TenderNotification> TenderNotifications => Set<TenderNotification>();
        public DbSet<TenderDocumentVerificationHistory> TenderDocumentVerificationHistory => Set<TenderDocumentVerificationHistory>();
        public DbSet<TenderVerificationReview> TenderVerificationReviews => Set<TenderVerificationReview>();
        public DbSet<FinancialBid> FinancialBids => Set<FinancialBid>();
        public DbSet<Agreement> Agreements => Set<Agreement>();
        public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
        public DbSet<WorkExecution> WorkExecutions => Set<WorkExecution>();
        public DbSet<MeasurementBook> MeasurementBooks => Set<MeasurementBook>();
        public DbSet<Bill> Bills => Set<Bill>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<WorkflowStage> WorkflowStages => Set<WorkflowStage>();
        public DbSet<WorkflowTransition> WorkflowTransitions => Set<WorkflowTransition>();
        public DbSet<TenderWorkflowAssignment> TenderWorkflowAssignments => Set<TenderWorkflowAssignment>();
        public DbSet<Announcement> Announcements => Set<Announcement>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Role>(e =>
            {
                e.ToTable("Roles");
                e.HasKey(x => x.RoleId);
                e.Property(x => x.RoleCode).IsRequired().HasMaxLength(40);
                e.HasIndex(x => x.RoleCode).IsUnique();
                e.Property(x => x.RoleName).IsRequired().HasMaxLength(100);
                e.HasIndex(x => x.RoleName).IsUnique();
            });

            modelBuilder.Entity<Permission>(e =>
            {
                e.ToTable("Permissions");
                e.HasKey(x => x.PermissionId);
                e.Property(x => x.PermissionKey).IsRequired().HasMaxLength(120);
                e.HasIndex(x => x.PermissionKey).IsUnique();
                e.Property(x => x.Description).IsRequired().HasMaxLength(400);
                e.Property(x => x.CreatedAtUtc).HasColumnType("datetime2(0)");
            });

            modelBuilder.Entity<RolePermission>(e =>
            {
                e.ToTable("RolePermissions");
                e.HasKey(x => new { x.RoleId, x.PermissionId });
                e.Property(x => x.AssignedAtUtc).HasColumnType("datetime2(0)");
                e.HasOne(x => x.Role).WithMany(r => r.RolePermissions).HasForeignKey(x => x.RoleId);
                e.HasOne(x => x.Permission).WithMany(p => p.RolePermissions).HasForeignKey(x => x.PermissionId);
                e.HasOne(x => x.AssignedByUser).WithMany().HasForeignKey(x => x.AssignedBy).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Department>(e =>
            {
                e.ToTable("Departments");
                e.HasKey(x => x.DepartmentId);
                e.Property(x => x.DepartmentName).IsRequired().HasMaxLength(150);
                e.Property(x => x.HeadOfficerName).HasMaxLength(150);
                e.HasIndex(x => x.DepartmentName).IsUnique();
            });

            modelBuilder.Entity<DesignationMaster>(e =>
            {
                e.ToTable("Designations");
                e.HasKey(x => x.DesignationName);
                e.Property(x => x.DesignationName).IsRequired().HasMaxLength(150);
                e.Property(x => x.SortOrder).IsRequired();
                e.Property(x => x.IsActive).IsRequired();
                e.Property(x => x.CreatedAtUtc).HasColumnType("datetime2");
            });

            modelBuilder.Entity<User>(e =>
            {
                e.ToTable("Users");
                e.HasKey(x => x.UserId);
                e.Property(x => x.Email).IsRequired().HasMaxLength(150);
                e.HasIndex(x => x.Email).IsUnique();
                e.Property(x => x.PasswordHash).IsRequired();
                e.Property(x => x.AccountType).IsRequired().HasMaxLength(24);
                e.HasOne(x => x.Role).WithMany(r => r.Users).HasForeignKey(x => x.RoleId).IsRequired(false);
                e.HasOne(x => x.Department).WithMany(d => d.Users).HasForeignKey(x => x.DepartmentId);
            });

            modelBuilder.Entity<Contractor>(e =>
            {
                e.ToTable("Contractors");
                e.HasKey(x => x.ContractorId);
                e.Property(x => x.RegistrationNo).IsRequired().HasMaxLength(100);
                e.HasIndex(x => x.RegistrationNo).IsUnique();
                e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
            });

            modelBuilder.Entity<Tender>(e =>
            {
                e.ToTable("Tender");
                e.HasKey(x => x.TenderId);
                e.Property(x => x.TenderNumber).IsRequired().HasMaxLength(50);
                e.HasIndex(x => x.TenderNumber).IsUnique();
                e.Property(x => x.EstimatedAmount).HasColumnType("decimal(18,2)");
                e.Property(x => x.EligibilityCriteria).HasMaxLength(4000);
                e.Property(x => x.TenderCategoryName).HasColumnName("TenderCategory").HasMaxLength(50);
                e.Property(x => x.BudgetInformation).HasMaxLength(4000);
                e.Property(x => x.TechnicalSanctionNumber).HasMaxLength(200);
                e.Property(x => x.TechnicalSanctionAuthority).HasMaxLength(400);
                e.Property(x => x.PreBidLocation).HasMaxLength(600);
                e.Property(x => x.PreBidDetails).HasMaxLength(4000);
                e.HasOne(x => x.Department).WithMany(d => d.Tenders).HasForeignKey(x => x.DepartmentId);
                e.HasOne(x => x.TenderCategory).WithMany().HasForeignKey(x => x.TenderCategoryName)
                    .HasPrincipalKey(x => x.TypeName).OnDelete(DeleteBehavior.Restrict);
                e.HasOne<TenderMethod>().WithMany().HasForeignKey(x => x.TenderType)
                    .HasPrincipalKey(x => x.MethodName).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
                e.HasQueryFilter(x => !x.IsDeleted);
                   e.Property(x => x.TrackingId).HasMaxLength(100).IsRequired();
                   e.Property(x => x.WorkflowVersion).HasDefaultValue((short)1);
            });

            modelBuilder.Entity<TenderMethod>(e =>
            {
                e.ToTable("TenderMethods");
                e.HasKey(x => x.MethodName);
                e.Property(x => x.MethodName).HasMaxLength(50).IsRequired();
            });

            modelBuilder.Entity<TenderCategoryMaster>(e =>
            {
                e.ToTable("TenderTypes");
                e.HasKey(x => x.TypeName);
                e.Property(x => x.TypeName).HasMaxLength(50);
            });

            modelBuilder.Entity<DocumentTypeMaster>(e =>
            {
                e.ToTable("DocumentTypes");
                e.HasKey(x => x.TypeName);
                e.Property(x => x.TypeName).HasMaxLength(80);
            });

            modelBuilder.Entity<UnitMaster>(e =>
            {
                e.ToTable("Units");
                e.HasKey(x => x.UnitName);
                e.Property(x => x.UnitName).HasMaxLength(20);
            });

            modelBuilder.Entity<Estimate>(e =>
            {
                e.ToTable("Estimate");
                e.HasKey(x => x.EstimateId);
                e.Property(x => x.Amount).HasColumnType("decimal(18,2)");
                e.HasOne(x => x.Tender).WithMany(t => t.Estimates).HasForeignKey(x => x.TenderId);
            });

            modelBuilder.Entity<Announcement>(e =>
            {
                e.ToTable("Announcements");
                e.HasKey(x => x.AnnouncementId);
                e.Property(x => x.Title).IsRequired().HasMaxLength(250);
                e.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
                e.HasQueryFilter(x => !x.IsDeleted);
            });

            modelBuilder.Entity<ApprovalHistory>(e =>
            {
                e.ToTable("ApprovalHistory");
                e.HasKey(x => x.ApprovalId);
                e.HasOne(x => x.Tender).WithMany(t => t.ApprovalHistories).HasForeignKey(x => x.TenderId);
                e.HasOne(x => x.ApprovedByUser).WithMany().HasForeignKey(x => x.ApprovedBy);
            });

            modelBuilder.Entity<TenderDocument>(e =>
            {
                e.ToTable("TenderDocuments");
                e.HasKey(x => x.DocumentId);
                e.HasOne(x => x.Tender).WithMany(t => t.Documents).HasForeignKey(x => x.TenderId);
            });

            modelBuilder.Entity<BOQItem>(e =>
            {
                e.ToTable("BOQ");
                e.HasKey(x => x.BOQId);
                e.Property(x => x.Quantity).HasColumnType("decimal(18,3)");
                e.Property(x => x.Rate).HasColumnType("decimal(18,2)");
                e.Property(x => x.Amount).HasColumnType("decimal(18,2)")
                    .HasComputedColumnSql("[Quantity] * [Rate]", stored: true);
                e.HasOne(x => x.Tender).WithMany(t => t.BOQItems).HasForeignKey(x => x.TenderId);
            });

            modelBuilder.Entity<TechnicalBid>(e =>
            {
                e.ToTable("TechnicalBid");
                e.HasKey(x => x.TechnicalBidId);
                e.HasIndex(x => new { x.TenderId, x.ContractorId }).IsUnique();
                e.HasOne(x => x.Tender).WithMany(t => t.TechnicalBids).HasForeignKey(x => x.TenderId);
                e.HasOne(x => x.Contractor).WithMany(c => c.TechnicalBids).HasForeignKey(x => x.ContractorId);
                e.HasMany(x => x.Documents).WithOne(x => x.TechnicalBid).HasForeignKey(x => x.TechnicalBidId);
            });

            modelBuilder.Entity<TechnicalBidDocument>(e =>
            {
                e.ToTable("TechnicalBidDocuments");
                e.HasKey(x => x.TechnicalBidDocumentId);
                e.Property(x => x.DocumentType).IsRequired().HasMaxLength(160).HasDefaultValue("Bid document");
                e.Property(x => x.FileName).IsRequired().HasMaxLength(260);
                e.Property(x => x.FilePath).IsRequired().HasMaxLength(400);
                e.HasOne(x => x.TechnicalBid).WithMany(x => x.Documents).HasForeignKey(x => x.TechnicalBidId);
                e.HasOne<User>().WithMany().HasForeignKey(x => x.UploadedBy).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<TenderRequiredBidDocument>(e =>
            {
                e.ToTable("TenderRequiredBidDocuments");
                e.HasKey(x => new { x.TenderId, x.DocumentType });
                e.Property(x => x.DocumentType).HasMaxLength(160).IsRequired();
                e.Property(x => x.Description).HasMaxLength(1000);
                e.Property(x => x.AllowedExtensions).HasMaxLength(500);
                e.HasOne<Tender>().WithMany().HasForeignKey(x => x.TenderId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne<User>().WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ContractorApplicationHistory>(e =>
            {
                e.ToTable("ContractorApplicationHistory");
                e.HasKey(x => x.HistoryId);
                e.Property(x => x.EventType).HasMaxLength(40).IsRequired();
                e.Property(x => x.PreviousStatus).HasMaxLength(30);
                e.Property(x => x.NewStatus).HasMaxLength(30);
                e.Property(x => x.Remarks).HasMaxLength(2000);
                e.HasIndex(x => new { x.TechnicalBidId, x.CreatedAtUtc });
                e.HasOne(x => x.TechnicalBid).WithMany().HasForeignKey(x => x.TechnicalBidId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne<User>().WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
                e.HasOne<Role>().WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ContractorApplicationRemark>(e =>
            {
                e.ToTable("ContractorApplicationRemarks");
                e.HasKey(x => x.RemarkId);
                e.Property(x => x.RemarkType).HasMaxLength(30).IsRequired();
                e.Property(x => x.Message).HasMaxLength(2000).IsRequired();
                e.HasIndex(x => new { x.TechnicalBidId, x.CreatedAtUtc });
                e.HasOne<TechnicalBid>().WithMany().HasForeignKey(x => x.TechnicalBidId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne<User>().WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
                e.HasOne<Role>().WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<TenderNotification>(e =>
            {
                e.ToTable("TenderNotifications");
                e.HasKey(x => x.NotificationId);
                e.Property(x => x.NotificationType).HasMaxLength(60).IsRequired();
                e.Property(x => x.Message).HasMaxLength(1000).IsRequired();
                e.HasIndex(x => new { x.UserId, x.ReadAtUtc, x.CreatedAtUtc });
                e.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne<Tender>().WithMany().HasForeignKey(x => x.TenderId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<TenderDocumentVerificationHistory>(e =>
            {
                e.ToTable("TenderDocumentVerificationHistory");
                e.HasKey(x => x.DocumentVerificationHistoryId);
                e.Property(x => x.FileName).HasMaxLength(520).IsRequired();
                e.Property(x => x.FilePath).HasMaxLength(800).IsRequired();
                e.Property(x => x.DocumentType).HasMaxLength(160).IsRequired();
                e.Property(x => x.ReviewStage).HasMaxLength(20).IsRequired();
                e.Property(x => x.VerificationStatus).HasMaxLength(20).IsRequired();
                e.Property(x => x.Remarks).HasMaxLength(2000);
                e.HasOne<Tender>().WithMany().HasForeignKey(x => x.TenderId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne<Contractor>().WithMany().HasForeignKey(x => x.ContractorId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne<TechnicalBidDocument>().WithMany().HasForeignKey(x => x.TechnicalBidDocumentId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne<User>().WithMany().HasForeignKey(x => x.ReviewerId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne<Role>().WithMany().HasForeignKey(x => x.ReviewerRoleId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<TenderVerificationReview>(e =>
            {
                e.ToTable("TenderVerificationReviews");
                e.HasKey(x => x.VerificationReviewId);
                e.Property(x => x.ReviewStage).HasMaxLength(20).IsRequired();
                e.Property(x => x.Decision).HasMaxLength(30).IsRequired();
                e.Property(x => x.Remarks).HasMaxLength(2000);
                e.HasOne<Tender>().WithMany().HasForeignKey(x => x.TenderId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne<Contractor>().WithMany().HasForeignKey(x => x.ContractorId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne<User>().WithMany().HasForeignKey(x => x.ReviewerId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne<Role>().WithMany().HasForeignKey(x => x.ReviewerRoleId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<FinancialBid>(e =>
            {
                e.ToTable("FinancialBid");
                e.HasKey(x => x.FinancialBidId);
                e.Property(x => x.BidAmount).HasColumnType("decimal(18,2)");
                e.HasIndex(x => new { x.TenderId, x.ContractorId }).IsUnique();
                e.HasOne(x => x.Tender).WithMany(t => t.FinancialBids).HasForeignKey(x => x.TenderId);
                e.HasOne(x => x.Contractor).WithMany(c => c.FinancialBids).HasForeignKey(x => x.ContractorId);
            });

            modelBuilder.Entity<Agreement>(e =>
            {
                e.ToTable("Agreement");
                e.HasKey(x => x.AgreementId);
                e.HasIndex(x => x.AgreementNo).IsUnique();
                e.HasOne(x => x.Tender).WithMany().HasForeignKey(x => x.TenderId);
                e.HasOne(x => x.Contractor).WithMany().HasForeignKey(x => x.ContractorId);
            });

            modelBuilder.Entity<WorkOrder>(e =>
            {
                e.ToTable("WorkOrder");
                e.HasKey(x => x.WorkOrderId);
                e.Property(x => x.Amount).HasColumnType("decimal(18,2)");
                e.HasIndex(x => x.OrderNo).IsUnique();
                e.HasOne(x => x.Tender).WithMany().HasForeignKey(x => x.TenderId);
                e.HasOne(x => x.Contractor).WithMany().HasForeignKey(x => x.ContractorId);
            });

            modelBuilder.Entity<WorkExecution>(e =>
            {
                e.ToTable("WorkExecution");
                e.HasKey(x => x.ExecutionId);
                e.Property(x => x.ProgressPercentage).HasColumnType("decimal(5,2)");
                e.HasOne(x => x.Tender).WithMany().HasForeignKey(x => x.TenderId);
            });

            modelBuilder.Entity<MeasurementBook>(e =>
            {
                e.ToTable("MeasurementBook");
                e.HasKey(x => x.MBId);
                e.Property(x => x.Quantity).HasColumnType("decimal(18,3)");
                e.HasOne(x => x.Tender).WithMany().HasForeignKey(x => x.TenderId);
            });

            modelBuilder.Entity<Bill>(e =>
            {
                e.ToTable("Bills");
                e.HasKey(x => x.BillId);
                e.Property(x => x.Amount).HasColumnType("decimal(18,2)");
                e.HasIndex(x => x.BillNo).IsUnique();
                e.HasOne(x => x.Tender).WithMany().HasForeignKey(x => x.TenderId);
                e.HasOne(x => x.Contractor).WithMany().HasForeignKey(x => x.ContractorId);
            });

            modelBuilder.Entity<Payment>(e =>
            {
                e.ToTable("Payments");
                e.HasKey(x => x.PaymentId);
                e.Property(x => x.Amount).HasColumnType("decimal(18,2)");
                e.HasIndex(x => x.TransactionNo).IsUnique();
                e.HasOne(x => x.Bill).WithMany(b => b.Payments).HasForeignKey(x => x.BillId);
            });

            modelBuilder.Entity<AuditLog>(e =>
            {
                e.ToTable("AuditLog");
                e.HasKey(x => x.LogId);
            });

            modelBuilder.Entity<WorkflowStage>(e =>
            {
                e.ToTable("WorkflowStages");
                e.HasKey(x => x.StageId);
                e.HasIndex(x => x.StageName).IsUnique();
                e.Property(x => x.WorkflowVersion).HasDefaultValue((short)1);
                e.HasOne(x => x.ApproverRole).WithMany().HasForeignKey(x => x.ApproverRoleId);
            });

            modelBuilder.Entity<WorkflowTransition>(e =>
            {
                e.ToTable("WorkflowTransitions");
                e.HasKey(x => x.TransitionId);
                e.Property(x => x.TransitionCode).IsRequired().HasMaxLength(80);
                e.HasIndex(x => x.TransitionCode).IsUnique();
                e.Property(x => x.FromStatus).IsRequired().HasMaxLength(100);
                e.Property(x => x.ActionName).IsRequired().HasMaxLength(50);
                e.Property(x => x.ToStatus).IsRequired().HasMaxLength(100);
                e.Property(x => x.ActionStatus).IsRequired().HasMaxLength(20);
                e.Property(x => x.PermissionKey).IsRequired().HasMaxLength(120);
                e.HasOne(x => x.Stage).WithMany().HasForeignKey(x => x.StageId);
                e.HasOne(x => x.Role).WithMany().HasForeignKey(x => x.RoleId);
            });

            modelBuilder.Entity<TenderWorkflowAssignment>(e =>
            {
                e.ToTable("TenderWorkflowAssignments");
                e.HasKey(x => x.AssignmentId);
                e.Property(x => x.AssignmentId).ValueGeneratedOnAdd();
                e.Property(x => x.AssignedAtUtc).HasColumnType("datetime2(0)");
                e.Property(x => x.CompletedAtUtc).HasColumnType("datetime2(0)");
                e.HasOne(x => x.Tender).WithMany().HasForeignKey(x => x.TenderId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.Stage).WithMany().HasForeignKey(x => x.StageId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.AssignedRole).WithMany().HasForeignKey(x => x.AssignedRoleId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.AssignedUser).WithMany().HasForeignKey(x => x.AssignedUserId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
