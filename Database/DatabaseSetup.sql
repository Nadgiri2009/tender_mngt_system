/* =====================================================================
   TENDER MANAGEMENT SYSTEM - FULL DATABASE SETUP
   This script creates the TenderManagementDB database, schema, seed data,
   default admin account, and stored procedures required by the application.
   ===================================================================== */

IF DB_ID('TenderManagementDB') IS NULL
BEGIN
    CREATE DATABASE TenderManagementDB;
END
GO

USE TenderManagementDB;
GO

/* =====================================================================
   1. ROLES
   ===================================================================== */
CREATE TABLE dbo.Roles (
    RoleId          INT IDENTITY(1,1) PRIMARY KEY,
    RoleName        NVARCHAR(50)  NOT NULL UNIQUE,
    Description     NVARCHAR(200) NULL,
    CreatedDate     DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
    IsActive        BIT           NOT NULL DEFAULT 1
);
GO

/* =====================================================================
   2. DEPARTMENTS
   ===================================================================== */
CREATE TABLE dbo.Departments (
    DepartmentId      INT IDENTITY(1,1) PRIMARY KEY,
    DepartmentName    NVARCHAR(150) NOT NULL UNIQUE,
    DepartmentCode    NVARCHAR(20)  NULL,
    HeadOfficerName   NVARCHAR(150) NULL,
    IsActive          BIT           NOT NULL DEFAULT 1,
    CreatedDate       DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME()
);
GO

/* =====================================================================
   3. USERS
   ===================================================================== */
CREATE TABLE dbo.Users (
    UserId          INT IDENTITY(1,1) PRIMARY KEY,
    FullName        NVARCHAR(150)  NOT NULL,
    Email           NVARCHAR(150)  NOT NULL UNIQUE,
    Mobile          NVARCHAR(20)   NULL,
    PasswordHash    NVARCHAR(500)  NOT NULL,
    RoleId          INT            NOT NULL,
    DepartmentId    INT            NULL,
    IsActive        BIT            NOT NULL DEFAULT 1,
    CreatedDate     DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedBy       INT            NULL,
    ModifiedDate    DATETIME2      NULL,
    ModifiedBy      INT            NULL,
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles(RoleId),
    CONSTRAINT FK_Users_Departments FOREIGN KEY (DepartmentId) REFERENCES dbo.Departments(DepartmentId)
);
GO
CREATE INDEX IX_Users_RoleId ON dbo.Users(RoleId);
CREATE INDEX IX_Users_DepartmentId ON dbo.Users(DepartmentId);
GO

/* =====================================================================
   4. CONTRACTORS
   ===================================================================== */
CREATE TABLE dbo.Contractors (
    ContractorId    INT IDENTITY(1,1) PRIMARY KEY,
    Name            NVARCHAR(150) NOT NULL,
    RegistrationNo  NVARCHAR(100) NOT NULL UNIQUE,
    Address         NVARCHAR(300) NULL,
    ContactNo       NVARCHAR(20)  NULL,
    Email           NVARCHAR(150) NULL,
    UserId          INT           NULL,
    IsActive        BIT           NOT NULL DEFAULT 1,
    CreatedDate     DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Contractors_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId)
);
GO

/* =====================================================================
   5. TENDER
   ===================================================================== */
CREATE TABLE dbo.Tender (
    TenderId           INT IDENTITY(1,1) PRIMARY KEY,
    TenderNumber       NVARCHAR(50)   NOT NULL UNIQUE,
    TrackingId         NVARCHAR(100)  NOT NULL UNIQUE DEFAULT (CONVERT(NVARCHAR(100), NEWID())),
    Title              NVARCHAR(250)  NOT NULL,
    Description        NVARCHAR(MAX)  NULL,
    DepartmentId       INT            NOT NULL,
    EstimatedAmount    DECIMAL(18,2)  NOT NULL DEFAULT 0,
    TenderType         NVARCHAR(50)   NOT NULL,
    CurrentStatus      NVARCHAR(50)   NOT NULL DEFAULT 'Draft',
    SubmissionDeadline DATETIME2      NULL,
    CreatedBy          INT            NOT NULL,
    CreatedDate        DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
    ModifiedBy         INT            NULL,
    ModifiedDate       DATETIME2      NULL,
    IsDeleted          BIT            NOT NULL DEFAULT 0,
    CONSTRAINT FK_Tender_Departments FOREIGN KEY (DepartmentId) REFERENCES dbo.Departments(DepartmentId),
    CONSTRAINT FK_Tender_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.Users(UserId)
);
GO
CREATE INDEX IX_Tender_DepartmentId ON dbo.Tender(DepartmentId);
CREATE INDEX IX_Tender_CurrentStatus ON dbo.Tender(CurrentStatus);
GO

/* =====================================================================
   6. ESTIMATE
   ===================================================================== */
CREATE TABLE dbo.Estimate (
    EstimateId      INT IDENTITY(1,1) PRIMARY KEY,
    TenderId        INT            NOT NULL,
    Amount          DECIMAL(18,2)  NOT NULL,
    PreparedBy      INT            NOT NULL,
    DocumentPath    NVARCHAR(400)  NULL,
    Status          NVARCHAR(50)   NOT NULL DEFAULT 'Prepared',
    CreatedDate     DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
    ModifiedDate    DATETIME2      NULL,
    CONSTRAINT FK_Estimate_Tender FOREIGN KEY (TenderId) REFERENCES dbo.Tender(TenderId) ON DELETE CASCADE,
    CONSTRAINT FK_Estimate_Users FOREIGN KEY (PreparedBy) REFERENCES dbo.Users(UserId)
);
GO

/* =====================================================================
   7. APPROVAL HISTORY
   ===================================================================== */
CREATE TABLE dbo.ApprovalHistory (
    ApprovalId       INT IDENTITY(1,1) PRIMARY KEY,
    TenderId         INT            NOT NULL,
    ApprovalStage    NVARCHAR(80)   NOT NULL,
    PreviousStatus   NVARCHAR(50)   NULL,
    NewStatus        NVARCHAR(50)   NOT NULL,
    Status           NVARCHAR(20)   NOT NULL,
    ApprovedBy       INT            NOT NULL,
    RoleAtAction     NVARCHAR(50)   NULL,
    Remarks          NVARCHAR(1000) NULL,
    ApprovalDate     DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_ApprovalHistory_Tender FOREIGN KEY (TenderId) REFERENCES dbo.Tender(TenderId) ON DELETE CASCADE,
    CONSTRAINT FK_ApprovalHistory_Users FOREIGN KEY (ApprovedBy) REFERENCES dbo.Users(UserId)
);
GO
CREATE INDEX IX_ApprovalHistory_TenderId ON dbo.ApprovalHistory(TenderId);
GO

/* =====================================================================
   8. TENDER DOCUMENTS
   ===================================================================== */
CREATE TABLE dbo.TenderDocuments (
    DocumentId      INT IDENTITY(1,1) PRIMARY KEY,
    TenderId        INT            NOT NULL,
    DocumentType    NVARCHAR(80)   NOT NULL,
    FileName        NVARCHAR(260)  NOT NULL,
    FilePath        NVARCHAR(400)  NOT NULL,
    UploadedBy      INT            NOT NULL,
    UploadedDate    DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_TenderDocuments_Tender FOREIGN KEY (TenderId) REFERENCES dbo.Tender(TenderId) ON DELETE CASCADE,
    CONSTRAINT FK_TenderDocuments_Users FOREIGN KEY (UploadedBy) REFERENCES dbo.Users(UserId)
);
GO

/* =====================================================================
   9. BOQ
   ===================================================================== */
CREATE TABLE dbo.BOQ (
    BOQId           INT IDENTITY(1,1) PRIMARY KEY,
    TenderId        INT            NOT NULL,
    ItemName        NVARCHAR(200)  NOT NULL,
    Description     NVARCHAR(500)  NULL,
    Quantity        DECIMAL(18,3)  NOT NULL,
    Unit            NVARCHAR(20)   NOT NULL,
    Rate            DECIMAL(18,2)  NOT NULL,
    Amount          AS (Quantity * Rate) PERSISTED,
    CreatedDate     DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_BOQ_Tender FOREIGN KEY (TenderId) REFERENCES dbo.Tender(TenderId) ON DELETE CASCADE
);
GO

/* =====================================================================
   10. TECHNICAL BID
   ===================================================================== */
CREATE TABLE dbo.TechnicalBid (
    TechnicalBidId  INT IDENTITY(1,1) PRIMARY KEY,
    TenderId        INT            NOT NULL,
    ContractorId    INT            NOT NULL,
    DocumentPath    NVARCHAR(400)  NULL,
    Status          NVARCHAR(30)   NOT NULL DEFAULT 'Submitted',
    Remarks         NVARCHAR(1000) NULL,
    EvaluatedBy     INT            NULL,
    SubmittedDate   DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
    EvaluatedDate   DATETIME2      NULL,
    CONSTRAINT FK_TechnicalBid_Tender FOREIGN KEY (TenderId) REFERENCES dbo.Tender(TenderId) ON DELETE CASCADE,
    CONSTRAINT FK_TechnicalBid_Contractor FOREIGN KEY (ContractorId) REFERENCES dbo.Contractors(ContractorId),
    CONSTRAINT UQ_TechnicalBid_Tender_Contractor UNIQUE (TenderId, ContractorId)
);
GO

/* =====================================================================
   11. FINANCIAL BID
   ===================================================================== */
CREATE TABLE dbo.FinancialBid (
    FinancialBidId  INT IDENTITY(1,1) PRIMARY KEY,
    TenderId        INT            NOT NULL,
    ContractorId    INT            NOT NULL,
    BidAmount       DECIMAL(18,2)  NOT NULL,
    [Rank]          INT            NULL,
    Status          NVARCHAR(30)   NOT NULL DEFAULT 'Submitted',
    SubmittedDate   DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_FinancialBid_Tender FOREIGN KEY (TenderId) REFERENCES dbo.Tender(TenderId) ON DELETE CASCADE,
    CONSTRAINT FK_FinancialBid_Contractor FOREIGN KEY (ContractorId) REFERENCES dbo.Contractors(ContractorId),
    CONSTRAINT UQ_FinancialBid_Tender_Contractor UNIQUE (TenderId, ContractorId)
);
GO

/* =====================================================================
   12. AGREEMENT
   ===================================================================== */
CREATE TABLE dbo.Agreement (
    AgreementId     INT IDENTITY(1,1) PRIMARY KEY,
    TenderId        INT            NOT NULL,
    ContractorId    INT            NOT NULL,
    AgreementNo     NVARCHAR(50)   NOT NULL UNIQUE,
    AgreementDate   DATETIME2      NOT NULL,
    DocumentPath    NVARCHAR(400)  NULL,
    CreatedDate     DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Agreement_Tender FOREIGN KEY (TenderId) REFERENCES dbo.Tender(TenderId),
    CONSTRAINT FK_Agreement_Contractor FOREIGN KEY (ContractorId) REFERENCES dbo.Contractors(ContractorId)
);
GO

/* =====================================================================
   13. WORK ORDER
   ===================================================================== */
CREATE TABLE dbo.WorkOrder (
    WorkOrderId     INT IDENTITY(1,1) PRIMARY KEY,
    TenderId        INT            NOT NULL,
    ContractorId    INT            NOT NULL,
    OrderNo         NVARCHAR(50)   NOT NULL UNIQUE,
    IssueDate       DATETIME2      NOT NULL,
    Amount          DECIMAL(18,2)  NOT NULL,
    CompletionDate  DATETIME2      NULL,
    CreatedDate     DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_WorkOrder_Tender FOREIGN KEY (TenderId) REFERENCES dbo.Tender(TenderId),
    CONSTRAINT FK_WorkOrder_Contractor FOREIGN KEY (ContractorId) REFERENCES dbo.Contractors(ContractorId)
);
GO

/* =====================================================================
   14. WORK EXECUTION
   ===================================================================== */
CREATE TABLE dbo.WorkExecution (
    ExecutionId        INT IDENTITY(1,1) PRIMARY KEY,
    TenderId           INT            NOT NULL,
    ProgressPercentage DECIMAL(5,2)   NOT NULL DEFAULT 0,
    Remarks            NVARCHAR(1000) NULL,
    UpdatedBy          INT            NULL,
    UpdatedDate        DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_WorkExecution_Tender FOREIGN KEY (TenderId) REFERENCES dbo.Tender(TenderId),
    CONSTRAINT FK_WorkExecution_Users FOREIGN KEY (UpdatedBy) REFERENCES dbo.Users(UserId)
);
GO

/* =====================================================================
   15. MEASUREMENT BOOK
   ===================================================================== */
CREATE TABLE dbo.MeasurementBook (
    MBId             INT IDENTITY(1,1) PRIMARY KEY,
    TenderId         INT            NOT NULL,
    Description      NVARCHAR(500)  NOT NULL,
    Quantity         DECIMAL(18,3)  NOT NULL,
    MeasurementDate  DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
    RecordedBy       INT            NULL,
    ApprovedBy       INT            NULL,
    ApprovalStatus   NVARCHAR(30)   NOT NULL DEFAULT 'Pending',
    CONSTRAINT FK_MeasurementBook_Tender FOREIGN KEY (TenderId) REFERENCES dbo.Tender(TenderId),
    CONSTRAINT FK_MeasurementBook_RecordedBy FOREIGN KEY (RecordedBy) REFERENCES dbo.Users(UserId),
    CONSTRAINT FK_MeasurementBook_ApprovedBy FOREIGN KEY (ApprovedBy) REFERENCES dbo.Users(UserId)
);
GO

/* =====================================================================
   16. BILLS
   ===================================================================== */
CREATE TABLE dbo.Bills (
    BillId          INT IDENTITY(1,1) PRIMARY KEY,
    TenderId        INT            NOT NULL,
    ContractorId    INT            NOT NULL,
    BillNo          NVARCHAR(50)   NOT NULL UNIQUE,
    Amount          DECIMAL(18,2)  NOT NULL,
    Status          NVARCHAR(30)   NOT NULL DEFAULT 'Submitted',
    SubmittedDate   DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
    ApprovedBy      INT            NULL,
    ApprovedDate    DATETIME2      NULL,
    CONSTRAINT FK_Bills_Tender FOREIGN KEY (TenderId) REFERENCES dbo.Tender(TenderId),
    CONSTRAINT FK_Bills_Contractor FOREIGN KEY (ContractorId) REFERENCES dbo.Contractors(ContractorId),
    CONSTRAINT FK_Bills_ApprovedBy FOREIGN KEY (ApprovedBy) REFERENCES dbo.Users(UserId)
);
GO

/* =====================================================================
   17. PAYMENTS
   ===================================================================== */
CREATE TABLE dbo.Payments (
    PaymentId       INT IDENTITY(1,1) PRIMARY KEY,
    BillId          INT            NOT NULL,
    Amount          DECIMAL(18,2)  NOT NULL,
    PaymentDate     DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
    TransactionNo   NVARCHAR(100)  NOT NULL UNIQUE,
    ProcessedBy     INT            NULL,
    CONSTRAINT FK_Payments_Bills FOREIGN KEY (BillId) REFERENCES dbo.Bills(BillId),
    CONSTRAINT FK_Payments_Users FOREIGN KEY (ProcessedBy) REFERENCES dbo.Users(UserId)
);
GO

/* =====================================================================
   18. AUDIT LOG
   ===================================================================== */
CREATE TABLE dbo.AuditLog (
    LogId           BIGINT IDENTITY(1,1) PRIMARY KEY,
    UserId          INT            NULL,
    Action          NVARCHAR(80)   NOT NULL,
    EntityName      NVARCHAR(100)  NOT NULL,
    EntityId        INT            NULL,
    Details         NVARCHAR(MAX)  NULL,
    IPAddress       NVARCHAR(50)   NULL,
    CreatedDate     DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_AuditLog_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId)
);
GO
CREATE INDEX IX_AuditLog_EntityName_EntityId ON dbo.AuditLog(EntityName, EntityId);
GO

/* =====================================================================
   19. WORKFLOW STAGES
   ===================================================================== */
CREATE TABLE dbo.WorkflowStages (
    StageId         INT IDENTITY(1,1) PRIMARY KEY,
    StageOrder      INT            NOT NULL,
    StageName       NVARCHAR(80)   NOT NULL UNIQUE,
    StatusOnPending NVARCHAR(50)   NOT NULL,
    StatusOnApprove NVARCHAR(50)   NOT NULL,
    StatusOnReject  NVARCHAR(50)   NOT NULL DEFAULT 'Rejected',
    ApproverRoleId  INT            NOT NULL,
    IsActive        BIT            NOT NULL DEFAULT 1,
    CONSTRAINT FK_WorkflowStages_Roles FOREIGN KEY (ApproverRoleId) REFERENCES dbo.Roles(RoleId)
);
GO

/* =====================================================================
   19a. ANNOUNCEMENTS -- public homepage notices, added for the portal
   homepage. Backs the "Important Announcements" section so it isn't
   hardcoded frontend content.
   ===================================================================== */
CREATE TABLE dbo.Announcements (
    AnnouncementId  INT IDENTITY(1,1) PRIMARY KEY,
    Title           NVARCHAR(250)  NOT NULL,
    Body            NVARCHAR(MAX)  NULL,
    IsPublished     BIT            NOT NULL DEFAULT 0,
    PublishDate     DATETIME2      NULL,
    ExpiryDate      DATETIME2      NULL,
    CreatedBy       INT            NOT NULL,
    CreatedDate     DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
    ModifiedBy      INT            NULL,
    ModifiedDate    DATETIME2      NULL,
    IsDeleted       BIT            NOT NULL DEFAULT 0,
    CONSTRAINT FK_Announcements_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.Users(UserId)
);
GO
CREATE INDEX IX_Announcements_IsPublished ON dbo.Announcements(IsPublished, PublishDate);
GO

/* =====================================================================
   20. STORED PROCEDURES
   ===================================================================== */
CREATE OR ALTER PROCEDURE dbo.sp_UpdateTenderStatus
    @TenderId INT,
    @ApprovalStage NVARCHAR(80),
    @NewStatus NVARCHAR(50),
    @ActionStatus NVARCHAR(20),
    @ApprovedBy INT,
    @RoleAtAction NVARCHAR(50),
    @Remarks NVARCHAR(1000) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @PreviousStatus NVARCHAR(50);
        SELECT @PreviousStatus = CurrentStatus FROM dbo.Tender WHERE TenderId = @TenderId;

        IF @PreviousStatus IS NULL
        BEGIN
            RAISERROR('Tender not found.', 16, 1);
        END

        UPDATE dbo.Tender
        SET CurrentStatus = @NewStatus,
            ModifiedBy = @ApprovedBy,
            ModifiedDate = SYSUTCDATETIME()
        WHERE TenderId = @TenderId;

        INSERT INTO dbo.ApprovalHistory
            (TenderId, ApprovalStage, PreviousStatus, NewStatus, Status, ApprovedBy, RoleAtAction, Remarks, ApprovalDate)
        VALUES
            (@TenderId, @ApprovalStage, @PreviousStatus, @NewStatus, @ActionStatus, @ApprovedBy, @RoleAtAction, @Remarks, SYSUTCDATETIME());

        COMMIT TRANSACTION;

        SELECT @PreviousStatus AS PreviousStatus, @NewStatus AS NewStatus;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetDashboardStats
    @UserId INT,
    @RoleId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        (SELECT COUNT(*) FROM dbo.Tender WHERE IsDeleted = 0) AS TotalTenders,
        (SELECT COUNT(*) FROM dbo.Tender WHERE IsDeleted = 0 AND CurrentStatus = 'Tender Published') AS PublishedTenders,
        (SELECT COUNT(*) FROM dbo.Tender WHERE IsDeleted = 0 AND CurrentStatus LIKE '%Pending%') AS PendingApprovals,
        (SELECT COUNT(*) FROM dbo.Tender WHERE IsDeleted = 0 AND CurrentStatus = 'Payment Completed') AS CompletedTenders,
        (SELECT COUNT(*) FROM dbo.Tender WHERE IsDeleted = 0 AND CreatedBy = @UserId) AS MyTenders;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetPendingApprovalsForRole
    @RoleId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT t.TenderId, t.TenderNumber, t.Title, t.CurrentStatus, t.EstimatedAmount,
           d.DepartmentName, t.CreatedDate
    FROM dbo.Tender t
    INNER JOIN dbo.Departments d ON d.DepartmentId = t.DepartmentId
    INNER JOIN dbo.WorkflowStages ws ON ws.StatusOnPending = t.CurrentStatus AND ws.ApproverRoleId = @RoleId
    WHERE t.IsDeleted = 0
    ORDER BY t.CreatedDate DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetFinancialBidRanking
    @TenderId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT fb.FinancialBidId, fb.TenderId, fb.ContractorId, c.Name AS ContractorName,
           fb.BidAmount, fb.Status,
           RANK() OVER (ORDER BY fb.BidAmount ASC) AS ComputedRank
    FROM dbo.FinancialBid fb
    INNER JOIN dbo.Contractors c ON c.ContractorId = fb.ContractorId
    WHERE fb.TenderId = @TenderId
    ORDER BY fb.BidAmount ASC;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetTenderFullHistory
    @TenderId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT ah.ApprovalId, ah.TenderId, ah.ApprovalStage, ah.PreviousStatus, ah.NewStatus,
           ah.Status, ah.RoleAtAction, ah.Remarks, ah.ApprovalDate,
           u.FullName AS ActionTakenBy
    FROM dbo.ApprovalHistory ah
    INNER JOIN dbo.Users u ON u.UserId = ah.ApprovedBy
    WHERE ah.TenderId = @TenderId
    ORDER BY ah.ApprovalDate ASC;
END
GO

/* =====================================================================
   21. SEED DATA
   ===================================================================== */
INSERT INTO dbo.Roles (RoleName, Description) VALUES
('Admin', 'System administrator'),
('DepartmentOfficer', 'Creates requirements and estimates'),
('TechnicalSanctionOfficer', 'Approves technical sanction'),
('GBOfficer', 'Approves estimate / work proposal'),
('DPDCOfficer', 'Administrative approval authority'),
('CAFOOfficer', 'Budget approval, rate opinion, financial verification'),
('MCAOfficer', 'Municipal Commissioner''s Approval - independent financial opinion, distinct from CAFO'),
('TenderCommittee', 'Approves tender documents and recommends contractor'),
('CompetentAuthority', 'Final approval authority; also clears publication as per power delegation'),
('Contractor', 'Bidder / awarded contractor'),
('TenderSection', 'Evaluates technical bids before Tender Committee approval');
GO

INSERT INTO dbo.Departments (DepartmentName, DepartmentCode) VALUES
('Administration', 'ADMIN');
GO

-- RoleId reference (per the dbo.Roles insert above, in order):
-- 1 Admin, 2 DepartmentOfficer, 3 TechnicalSanctionOfficer, 4 GBOfficer, 5 DPDCOfficer,
-- 6 CAFOOfficer, 7 MCAOfficer, 8 TenderCommittee, 9 CompetentAuthority, 10 Contractor,
-- 11 TenderSection
INSERT INTO dbo.WorkflowStages (StageOrder, StageName, StatusOnPending, StatusOnApprove, StatusOnReject, ApproverRoleId) VALUES
(1, 'RequirementSubmission', 'Requirement Submitted', 'Estimate Prepared', 'Draft', 2),
(2, 'TechnicalSanction', 'TS Approval Pending', 'TS Approved', 'Rejected', 3),
(3, 'GBApproval', 'GB Approval Pending', 'GB Approved', 'Rejected', 4),
(4, 'AdministrativeApproval', 'Administrative Approval Pending', 'Administrative Approved', 'Rejected', 5),
(5, 'BudgetApproval', 'Budget Approval Pending', 'Budget Approved', 'Rejected', 6),
(6, 'TenderCommitteeApproval', 'Tender Committee Approval', 'Publication Approval Pending', 'Rejected', 8),
(7, 'PublicationApproval', 'Publication Approval Pending', 'Tender Published', 'Rejected', 9),
(8, 'TechnicalEvaluation', 'Technical Evaluation', 'Technical Evaluation Approved', 'Rejected', 11),
(9, 'TechnicalEvaluationCommitteeApproval', 'Technical Evaluation Approved', 'Financial Bid Opening', 'Rejected', 8),
(10, 'CAFORateOpinion', 'CAFO Rate Opinion', 'Financial Evaluation', 'Rejected', 6),
(11, 'MCAOpinion', 'MCA Opinion', 'Tender Committee Recommendation', 'Rejected', 7),
(12, 'TenderCommitteeRecommendation', 'Tender Committee Recommendation', 'Final Approval', 'Rejected', 8),
(13, 'FinalAuthorityApproval', 'Final Approval', 'Agreement Completed', 'Rejected', 9),
(14, 'BillApproval', 'Bill Submitted', 'Bill Approved', 'Rejected', 6);
GO

/* =====================================================================
   22. LOGIN ACCOUNTS - one per role, so every stage of the tender
   lifecycle can actually be exercised without registering accounts by
   hand first. All non-admin accounts share the password Password@123
   (bcrypt hash below); the admin account keeps its original password
   Admin@1234. Change these before any real deployment.
   ===================================================================== */
INSERT INTO dbo.Users (FullName, Email, Mobile, PasswordHash, RoleId, DepartmentId, IsActive, CreatedDate) VALUES
('Administrator', 'admin@example.com', '9999999999', '$2a$11$Jr6Ol1A0TDnyFKfWRc0LhORa6ckHNKXhkSRlYQWIl7EJ8MIQeTv.W', 1, 1, 1, SYSUTCDATETIME()),
('Department Officer', 'dept.officer@smc.gov.in', '9000000001', '$2b$11$btyd1coXcY6wPi7YI/4jNu7I0O3zrS94R1COmZzK6sZZC/Jm240DO', 2, 1, 1, SYSUTCDATETIME()),
('Technical Sanction Officer', 'ts.officer@smc.gov.in', '9000000002', '$2b$11$4GXzSFql07uRAfHpDh57p.p/wdJ5aRRDcFDnwmG4Kk7NfUKnsvtH.', 3, 1, 1, SYSUTCDATETIME()),
('GB Officer', 'gb.officer@smc.gov.in', '9000000003', '$2b$11$NKzlr378WK/TMq3vrZ7C6.6a6hrdAbFkT3eeihgLoqDHjXJn8y0Pm', 4, 1, 1, SYSUTCDATETIME()),
('DPDC Officer', 'dpdc.officer@smc.gov.in', '9000000004', '$2b$11$iD2IOR/WT2JCJ13I8a4IKe6OCX9CyPuu.L4Jo3.z7n4JHWZtyVZzu', 5, 1, 1, SYSUTCDATETIME()),
('CAFO Officer', 'cafo.officer@smc.gov.in', '9000000005', '$2b$11$sIuopOzCElGAan/ceFB6hOF216kttJu4sgfq5SFiLDkVnTZu/YE3m', 6, 1, 1, SYSUTCDATETIME()),
('MCA Officer', 'mca.officer@smc.gov.in', '9000000006', '$2b$11$/nJNYIH33VVNJ5uFbYTHY.u/Z5ugx7VOx90hLz1cHAANMFKQYqzWi', 7, 1, 1, SYSUTCDATETIME()),
('Tender Committee', 'tender.committee@smc.gov.in', '9000000007', '$2b$11$fpHLm9ZZl0.FILCJOzA9j.dyTFXg2HxAqa8QtvZ8ruvKSsvY1ObhS', 8, 1, 1, SYSUTCDATETIME()),
('Competent Authority', 'competent.authority@smc.gov.in', '9000000008', '$2b$11$FFjQHsJjCHbMZoDbI2Y.9uFNFjyIi.N1ALd8OYmZs5Udy493A.gK.', 9, 1, 1, SYSUTCDATETIME()),
('Demo Contractor', 'contractor@smc.gov.in', '9000000009', '$2b$11$z7uw.OQun03PjjX7cnUvTOd.We4BMvxmQkS6KjWIkaK7mxBmCvm1u', 10, NULL, 1, SYSUTCDATETIME()),
('Tender Section Officer', 'tender.section@smc.gov.in', '9000000010', '$2b$11$z7uw.OQun03PjjX7cnUvTOd.We4BMvxmQkS6KjWIkaK7mxBmCvm1u', 11, 1, 1, SYSUTCDATETIME());
GO

PRINT 'Database schema, stored procedures, seed data, and one login per role created successfully.';
