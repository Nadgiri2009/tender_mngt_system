using Microsoft.EntityFrameworkCore;
using TenderManagement.Domain.Entities;

namespace TenderManagement.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Role> Roles => Set<Role>();
        public DbSet<Department> Departments => Set<Department>();
        public DbSet<User> Users => Set<User>();
        public DbSet<Contractor> Contractors => Set<Contractor>();
        public DbSet<Tender> Tenders => Set<Tender>();
        public DbSet<Estimate> Estimates => Set<Estimate>();
        public DbSet<ApprovalHistory> ApprovalHistories => Set<ApprovalHistory>();
        public DbSet<TenderDocument> TenderDocuments => Set<TenderDocument>();
        public DbSet<BOQItem> BOQItems => Set<BOQItem>();
        public DbSet<TechnicalBid> TechnicalBids => Set<TechnicalBid>();
        public DbSet<TechnicalBidDocument> TechnicalBidDocuments => Set<TechnicalBidDocument>();
        public DbSet<FinancialBid> FinancialBids => Set<FinancialBid>();
        public DbSet<Agreement> Agreements => Set<Agreement>();
        public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
        public DbSet<WorkExecution> WorkExecutions => Set<WorkExecution>();
        public DbSet<MeasurementBook> MeasurementBooks => Set<MeasurementBook>();
        public DbSet<Bill> Bills => Set<Bill>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<WorkflowStage> WorkflowStages => Set<WorkflowStage>();
        public DbSet<Announcement> Announcements => Set<Announcement>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Role>(e =>
            {
                e.ToTable("Roles");
                e.HasKey(x => x.RoleId);
                e.Property(x => x.RoleName).IsRequired().HasMaxLength(50);
                e.HasIndex(x => x.RoleName).IsUnique();
            });

            modelBuilder.Entity<Department>(e =>
            {
                e.ToTable("Departments");
                e.HasKey(x => x.DepartmentId);
                e.Property(x => x.DepartmentName).IsRequired().HasMaxLength(150);
                e.Property(x => x.HeadOfficerName).HasMaxLength(150);
                e.HasIndex(x => x.DepartmentName).IsUnique();
            });

            modelBuilder.Entity<User>(e =>
            {
                e.ToTable("Users");
                e.HasKey(x => x.UserId);
                e.Property(x => x.Email).IsRequired().HasMaxLength(150);
                e.HasIndex(x => x.Email).IsUnique();
                e.Property(x => x.PasswordHash).IsRequired();
                e.HasOne(x => x.Role).WithMany(r => r.Users).HasForeignKey(x => x.RoleId);
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
                e.HasOne(x => x.Department).WithMany(d => d.Tenders).HasForeignKey(x => x.DepartmentId);
                e.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
                e.HasQueryFilter(x => !x.IsDeleted);
                   e.Property(x => x.TrackingId).HasMaxLength(100).IsRequired();
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
                e.Property(x => x.FileName).IsRequired().HasMaxLength(260);
                e.Property(x => x.FilePath).IsRequired().HasMaxLength(400);
                e.HasOne(x => x.TechnicalBid).WithMany(x => x.Documents).HasForeignKey(x => x.TechnicalBidId);
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
                e.HasOne(x => x.ApproverRole).WithMany().HasForeignKey(x => x.ApproverRoleId);
            });
        }
    }
}
