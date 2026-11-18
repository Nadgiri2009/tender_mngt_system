SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_NAME() <> N'tender_tracking'
    THROW 51300, 'This migration is scoped to the tender_tracking database only.', 1;

IF OBJECT_ID(N'dbo.Roles', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Users', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Permissions', N'U') IS NULL
   OR OBJECT_ID(N'dbo.RolePermissions', N'U') IS NULL
   OR OBJECT_ID(N'dbo.WorkflowStages', N'U') IS NULL
   OR OBJECT_ID(N'dbo.AuditLog', N'U') IS NULL
    THROW 51301, 'Run the base SMC tender schema before this migration.', 1;

BEGIN TRANSACTION;

IF COL_LENGTH(N'dbo.Roles', N'RoleCode') IS NULL
    ALTER TABLE dbo.Roles ADD RoleCode NVARCHAR(40) NULL;
GO

UPDATE dbo.Roles
SET RoleCode=N'Junior Engineer (JE)'
WHERE RoleCode IS NULL
  AND RoleName IN (N'Junior Engineer (JE)',N'Junior Engineer (JE)');

UPDATE dbo.Roles
SET RoleCode=N'TENDER_CELL'
WHERE RoleCode IS NULL
  AND RoleName IN (N'Nivida Kakash / Tender Cell',N'Nivida Kaksh (Tender Cell)',N'Tender Cell');

UPDATE dbo.Roles
SET RoleCode=N'CHAIRMAN'
WHERE RoleCode IS NULL
  AND RoleName IN (N'Nivida Samiti Adhaksha',N'Nivida Samiti Adhyaksha',
                   N'Tender Committee Chairman (Nivida Samiti Adhyaksha)');

UPDATE dbo.Roles
SET RoleCode=N'DIRECTOR_AUDITOR'
WHERE RoleCode IS NULL
  AND RoleName IN (N'Chief Audit',N'Chief Auditor',N'Director Auditor');

IF EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleCode IS NULL)
    THROW 51302, 'An unrecognized role exists. Map it explicitly before applying this migration.', 1;

IF EXISTS
(
    SELECT RoleCode FROM dbo.Roles
    GROUP BY RoleCode HAVING COUNT(*) > 1
)
    THROW 51303, 'Duplicate role codes exist. Resolve them before applying this migration.', 1;

IF EXISTS
(
    SELECT 1 FROM dbo.Roles
    WHERE RoleCode NOT IN (N'Junior Engineer (JE)',N'TENDER_CELL',N'CHAIRMAN',N'DIRECTOR_AUDITOR')
)
    THROW 51304, 'Only the four approved internal roles are allowed in tender_tracking.', 1;

IF EXISTS
(
    SELECT 1 FROM sys.columns
    WHERE object_id=OBJECT_ID(N'dbo.Roles') AND name=N'RoleName' AND max_length<200
)
    ALTER TABLE dbo.Roles ALTER COLUMN RoleName NVARCHAR(100) NOT NULL;
GO

INSERT INTO dbo.Roles(RoleCode,RoleName,Description,IsActive,IsSystemRole)
SELECT v.RoleCode,v.RoleName,v.Description,1,1
FROM (VALUES
    (N'Junior Engineer (JE)',N'Junior Engineer (JE)',N'Tender creation, publication, pre-bid/pre-bill and measurement, opening/closing, financial data entry, work orders and agreements.'),
    (N'TENDER_CELL',N'Tender Cell',N'Document requirements, verification, shortfall creation and re-verification.'),
    (N'CHAIRMAN',N'Tender Committee Chairman (Nivida Samiti Adhyaksha)',N'Financial bid opening and approval.'),
    (N'DIRECTOR_AUDITOR',N'Director Auditor',N'Financial bid review and approval.')
) v(RoleCode,RoleName,Description)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Roles r WHERE r.RoleCode=v.RoleCode);

UPDATE r
SET RoleName=v.RoleName,Description=v.Description,IsActive=1,IsSystemRole=1
FROM dbo.Roles r
JOIN (VALUES
    (N'Junior Engineer (JE)',N'Junior Engineer (JE)',N'Tender creation, publication, pre-bid/pre-bill and measurement, opening/closing, financial data entry, work orders and agreements.'),
    (N'TENDER_CELL',N'Tender Cell',N'Document requirements, verification, shortfall creation and re-verification.'),
    (N'CHAIRMAN',N'Tender Committee Chairman (Nivida Samiti Adhyaksha)',N'Financial bid opening and approval.'),
    (N'DIRECTOR_AUDITOR',N'Director Auditor',N'Financial bid review and approval.')
) v(RoleCode,RoleName,Description) ON v.RoleCode=r.RoleCode;

IF (SELECT COUNT(*) FROM dbo.Roles) <> 4
    THROW 51305, 'The role catalog must contain exactly four internal roles.', 1;

IF EXISTS
(
    SELECT 1 FROM dbo.Roles
    WHERE RoleCode IS NULL OR RoleName IS NULL OR IsActive=0 OR IsSystemRole=0
)
    THROW 51306, 'All four configured roles must be active protected system roles.', 1;

IF EXISTS
(
    SELECT 1 FROM sys.columns
    WHERE object_id=OBJECT_ID(N'dbo.Roles') AND name=N'RoleCode'
      AND (max_length<>80 OR is_nullable=1)
)
    ALTER TABLE dbo.Roles ALTER COLUMN RoleCode NVARCHAR(40) NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Roles') AND name=N'UX_Roles_RoleCode')
    CREATE UNIQUE INDEX UX_Roles_RoleCode ON dbo.Roles(RoleCode);

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.Roles') AND name=N'CK_Roles_ApprovedRoleCodes')
    ALTER TABLE dbo.Roles WITH CHECK ADD CONSTRAINT CK_Roles_ApprovedRoleCodes
        CHECK (RoleCode IN (N'Junior Engineer (JE)',N'TENDER_CELL',N'CHAIRMAN',N'DIRECTOR_AUDITOR'));

IF COL_LENGTH(N'dbo.Users', N'AccountType') IS NULL
BEGIN
    ALTER TABLE dbo.Users ADD AccountType NVARCHAR(24) NOT NULL
        CONSTRAINT DF_Users_AccountType DEFAULT N'Internal' WITH VALUES;
END;
GO

IF EXISTS
(
    SELECT 1 FROM sys.columns
    WHERE object_id=OBJECT_ID(N'dbo.Users') AND name=N'RoleId' AND is_nullable=0
)
    ALTER TABLE dbo.Users ALTER COLUMN RoleId INT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.Users') AND name=N'CK_Users_AccountTypeRole')
    ALTER TABLE dbo.Users WITH CHECK ADD CONSTRAINT CK_Users_AccountTypeRole
        CHECK ((AccountType=N'Internal' AND RoleId IS NOT NULL)
            OR (AccountType=N'ExternalBidder' AND RoleId IS NULL));

IF OBJECT_ID(N'dbo.ExternalAccountTypePermissions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ExternalAccountTypePermissions
    (
        AccountType NVARCHAR(24) NOT NULL,
        PermissionId INT NOT NULL,
        AssignedAtUtc DATETIME2(0) NOT NULL
            CONSTRAINT DF_ExternalAccountTypePermissions_AssignedAtUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_ExternalAccountTypePermissions PRIMARY KEY(AccountType,PermissionId),
        CONSTRAINT CK_ExternalAccountTypePermissions_ExternalOnly CHECK(AccountType=N'ExternalBidder'),
        CONSTRAINT FK_ExternalAccountTypePermissions_Permissions
            FOREIGN KEY(PermissionId) REFERENCES dbo.Permissions(PermissionId)
    );
END;

DECLARE @PermissionCatalog TABLE
(
    PermissionKey NVARCHAR(120) NOT NULL PRIMARY KEY,
    Description NVARCHAR(400) NOT NULL
);

INSERT INTO @PermissionCatalog(PermissionKey,Description)
VALUES
    (N'agreement.manage',N'Create and upload tender agreements.'),
    (N'dashboard.view',N'View the role-specific dashboard and assigned work.'),
    (N'document.read.assigned',N'View documents belonging to assigned tenders.'),
    (N'document.requirements.manage',N'Configure tender-specific bidder document requirements.'),
    (N'financial.approve.final',N'Review and approve financial bid data as Director Auditor.'),
    (N'financial.bids.enter',N'Enter financial bid information after opening approval.'),
    (N'financial.open.approve',N'Approve financial bid opening as Chairman.'),
    (N'financial.open.execute',N'Open the financial bid data-entry stage after approval.'),
    (N'masterdata.manage',N'Maintain authorized department and tender reference data.'),
    (N'roles.view',N'View the fixed four-role catalog and permission matrix.'),
    (N'roles.manage',N'Manage permissions for the four fixed internal roles.'),
    (N'shortfall.approve',N'Approve document shortfalls as Tender Cell.'),
    (N'shortfall.clear',N'Re-verify and clear submitted shortfall documents.'),
    (N'shortfall.create',N'Create shortfalls with the configured three-calendar-day deadline.'),
    (N'shortfall.respond.own',N'Submit documents against the authenticated bidder account.'),
    (N'tender.cell.verify',N'Verify bidder documents and approve complete submissions.'),
    (N'tender.closing.manage',N'Close and record tender opening/closing actions.'),
    (N'tender.create.department',N'Create tenders for the assigned department.'),
    (N'tender.measurement.manage',N'Create pre-bill and measurement-book records.'),
    (N'tender.prebill.manage',N'Manage pre-bid meeting and pre-bill details.'),
    (N'tender.primary.open',N'Open technical bids after the tender closing time.'),
    (N'tender.publication.manage',N'Publish tenders and configure publication dates.'),
    (N'tender.read.department',N'Read tenders within the authorized department scope.'),
    (N'tender.read.published',N'Read published tender notices as an external bidder.'),
    (N'users.manage',N'Provision and manage accounts without creating additional roles.'),
    (N'workorder.manage',N'Create work orders and upload the approved document.'),
    (N'workflow.configure',N'Manage authorized workflow configuration within the fixed role boundary.');

INSERT INTO dbo.Permissions(PermissionKey,Description)
SELECT c.PermissionKey,c.Description
FROM @PermissionCatalog c
WHERE NOT EXISTS
(
    SELECT 1 FROM dbo.Permissions p WHERE p.PermissionKey=c.PermissionKey
);

CREATE TABLE #ExpectedRolePermissions
(
    RoleCode NVARCHAR(40) NOT NULL,
    PermissionKey NVARCHAR(120) NOT NULL,
    PRIMARY KEY(RoleCode,PermissionKey)
);

INSERT INTO #ExpectedRolePermissions(RoleCode,PermissionKey)
VALUES
    (N'Junior Engineer (JE)',N'agreement.manage'),(N'Junior Engineer (JE)',N'dashboard.view'),(N'Junior Engineer (JE)',N'document.read.assigned'),
    (N'Junior Engineer (JE)',N'financial.bids.enter'),(N'Junior Engineer (JE)',N'financial.open.execute'),
    (N'Junior Engineer (JE)',N'masterdata.manage'),(N'Junior Engineer (JE)',N'roles.manage'),(N'Junior Engineer (JE)',N'roles.view'),
    (N'Junior Engineer (JE)',N'tender.closing.manage'),(N'Junior Engineer (JE)',N'tender.create.department'),
    (N'Junior Engineer (JE)',N'tender.measurement.manage'),(N'Junior Engineer (JE)',N'tender.prebill.manage'),
    (N'Junior Engineer (JE)',N'tender.primary.open'),(N'Junior Engineer (JE)',N'tender.publication.manage'),
    (N'Junior Engineer (JE)',N'tender.read.department'),(N'Junior Engineer (JE)',N'users.manage'),(N'Junior Engineer (JE)',N'workorder.manage'),
    (N'Junior Engineer (JE)',N'workflow.configure'),
    (N'TENDER_CELL',N'dashboard.view'),(N'TENDER_CELL',N'document.read.assigned'),
    (N'TENDER_CELL',N'document.requirements.manage'),(N'TENDER_CELL',N'shortfall.approve'),
    (N'TENDER_CELL',N'shortfall.clear'),(N'TENDER_CELL',N'shortfall.create'),
    (N'TENDER_CELL',N'tender.cell.verify'),(N'TENDER_CELL',N'tender.read.department'),
    (N'CHAIRMAN',N'dashboard.view'),(N'CHAIRMAN',N'document.read.assigned'),
    (N'CHAIRMAN',N'financial.open.approve'),(N'CHAIRMAN',N'tender.read.department'),
    (N'DIRECTOR_AUDITOR',N'dashboard.view'),(N'DIRECTOR_AUDITOR',N'document.read.assigned'),
    (N'DIRECTOR_AUDITOR',N'financial.approve.final'),(N'DIRECTOR_AUDITOR',N'tender.read.department');

INSERT INTO dbo.AuditLog(UserId,Action,EntityName,EntityId,Details)
SELECT NULL,N'RolePermissionMigration',N'Role',r.RoleId,
       CONCAT(N'Revoked obsolete permission grant during four-role alignment: ',p.PermissionKey)
FROM dbo.RolePermissions rp
JOIN dbo.Roles r ON r.RoleId=rp.RoleId
JOIN dbo.Permissions p ON p.PermissionId=rp.PermissionId
WHERE NOT EXISTS
(
    SELECT 1 FROM #ExpectedRolePermissions expected
    WHERE expected.RoleCode=r.RoleCode AND expected.PermissionKey=p.PermissionKey
);

DELETE rp
FROM dbo.RolePermissions rp
JOIN dbo.Roles r ON r.RoleId=rp.RoleId
JOIN dbo.Permissions p ON p.PermissionId=rp.PermissionId
WHERE NOT EXISTS
(
    SELECT 1 FROM #ExpectedRolePermissions expected
    WHERE expected.RoleCode=r.RoleCode AND expected.PermissionKey=p.PermissionKey
);

INSERT INTO dbo.RolePermissions(RoleId,PermissionId,AssignedBy)
SELECT r.RoleId,p.PermissionId,NULL
FROM #ExpectedRolePermissions expected
JOIN dbo.Roles r ON r.RoleCode=expected.RoleCode
JOIN dbo.Permissions p ON p.PermissionKey=expected.PermissionKey
WHERE NOT EXISTS
(
    SELECT 1 FROM dbo.RolePermissions rp
    WHERE rp.RoleId=r.RoleId AND rp.PermissionId=p.PermissionId
);

INSERT INTO dbo.ExternalAccountTypePermissions(AccountType,PermissionId)
SELECT N'ExternalBidder',p.PermissionId
FROM dbo.Permissions p
WHERE p.PermissionKey IN (N'shortfall.respond.own',N'tender.read.published')
  AND NOT EXISTS
  (
      SELECT 1 FROM dbo.ExternalAccountTypePermissions x
      WHERE x.AccountType=N'ExternalBidder' AND x.PermissionId=p.PermissionId
  );

IF OBJECT_ID(N'dbo.WorkflowTransitions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.WorkflowTransitions
    (
        TransitionId INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_WorkflowTransitions PRIMARY KEY,
        TransitionCode NVARCHAR(80) NOT NULL,
        WorkflowVersion SMALLINT NOT NULL,
        StageId INT NOT NULL,
        FromStatus NVARCHAR(100) NOT NULL,
        ActionName NVARCHAR(50) NOT NULL,
        ToStatus NVARCHAR(100) NOT NULL,
        ActionStatus NVARCHAR(20) NOT NULL
            CONSTRAINT DF_WorkflowTransitions_ActionStatus DEFAULT N'Completed',
        RoleId INT NOT NULL,
        PermissionKey NVARCHAR(120) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_WorkflowTransitions_IsActive DEFAULT 1,
        CONSTRAINT UQ_WorkflowTransitions_Code UNIQUE(TransitionCode),
        CONSTRAINT FK_WorkflowTransitions_Stage FOREIGN KEY(StageId) REFERENCES dbo.WorkflowStages(StageId),
        CONSTRAINT FK_WorkflowTransitions_Role FOREIGN KEY(RoleId) REFERENCES dbo.Roles(RoleId),
        CONSTRAINT FK_WorkflowTransitions_Permission FOREIGN KEY(PermissionKey) REFERENCES dbo.Permissions(PermissionKey)
    );
    CREATE INDEX IX_WorkflowTransitions_Lookup
        ON dbo.WorkflowTransitions(WorkflowVersion,FromStatus,ActionName,RoleId,IsActive);
END;

IF COL_LENGTH(N'dbo.WorkflowTransitions',N'ActionStatus') IS NULL
BEGIN
    ALTER TABLE dbo.WorkflowTransitions
        ADD ActionStatus NVARCHAR(20) NOT NULL
            CONSTRAINT DF_WorkflowTransitions_ActionStatus DEFAULT N'Completed';

    EXEC sys.sp_executesql N'
        UPDATE dbo.WorkflowTransitions
        SET ActionStatus=CASE ActionName
            WHEN N''Submit'' THEN N''Submitted''
            WHEN N''Approve'' THEN N''Approved''
            WHEN N''Reject'' THEN N''Rejected''
            ELSE N''Completed''
        END;';
END;

GO

IF NOT EXISTS
(
    SELECT 1 FROM sys.indexes
    WHERE object_id=OBJECT_ID(N'dbo.WorkflowTransitions')
      AND name=N'UX_WorkflowTransitions_ActiveAction'
)
    CREATE UNIQUE INDEX UX_WorkflowTransitions_ActiveAction
        ON dbo.WorkflowTransitions(WorkflowVersion,FromStatus,ActionName,RoleId)
        WHERE IsActive=1;

DECLARE @WorkflowStages TABLE
(
    StageOrder INT NOT NULL,
    StageName NVARCHAR(80) NOT NULL PRIMARY KEY,
    StatusOnPending NVARCHAR(100) NOT NULL,
    StatusOnApprove NVARCHAR(100) NOT NULL,
    StatusOnReject NVARCHAR(100) NOT NULL,
    RoleCode NVARCHAR(40) NOT NULL
);

INSERT INTO @WorkflowStages(StageOrder,StageName,StatusOnPending,StatusOnApprove,StatusOnReject,RoleCode)
VALUES
    (101,N'Final_JE_SubmitTender',N'Draft',N'Submitted',N'Draft',N'Junior Engineer (JE)'),
    (102,N'Final_JE_PublishTender',N'Submitted',N'Tender Published',N'Submitted',N'Junior Engineer (JE)'),
    (103,N'Final_JE_OpenTechnicalBids',N'Tender Published',N'Primary Verification Pending',N'Tender Published',N'Junior Engineer (JE)'),
    (104,N'Final_Cell_CompleteSecondaryVerification',N'Primary Verification Pending',N'Financial Bid Opening Approval Pending',N'Primary Verification Pending',N'TENDER_CELL'),
    (105,N'Final_Cell_CreateShortfall',N'Primary Verification Pending',N'Shortfall Pending',N'Primary Verification Pending',N'TENDER_CELL'),
    (106,N'Final_Cell_ClearShortfall',N'Shortfall Documents Submitted',N'Financial Bid Opening Approval Pending',N'Shortfall Documents Submitted',N'TENDER_CELL'),
    (107,N'Final_Samiti_ApproveFinancialOpening',N'Financial Bid Opening Approval Pending',N'Financial Bid Opening Approved',N'Financial Bid Opening Rejected',N'CHAIRMAN'),
    (108,N'Final_JE_OpenFinancialBids',N'Financial Bid Opening Approved',N'Final Financial Approval Pending',N'Financial Bid Opening Approved',N'Junior Engineer (JE)'),
    (109,N'Final_Auditor_ApproveFinancialBids',N'Final Financial Approval Pending',N'Final Financial Approval Completed',N'Final Financial Approval Rejected',N'DIRECTOR_AUDITOR'),
    (110,N'Final_JE_GenerateWorkOrder',N'Final Financial Approval Completed',N'Work Order Generated',N'Final Financial Approval Completed',N'Junior Engineer (JE)'),
    (111,N'Final_JE_GenerateAgreement',N'Work Order Generated',N'Tender Agreement Generated',N'Work Order Generated',N'Junior Engineer (JE)');

INSERT INTO dbo.WorkflowStages(StageOrder,StageName,StatusOnPending,StatusOnApprove,StatusOnReject,ApproverRoleId,IsActive,WorkflowVersion)
SELECT s.StageOrder,s.StageName,s.StatusOnPending,s.StatusOnApprove,s.StatusOnReject,r.RoleId,1,2
FROM @WorkflowStages s
JOIN dbo.Roles r ON r.RoleCode=s.RoleCode
WHERE NOT EXISTS (SELECT 1 FROM dbo.WorkflowStages existing WHERE existing.StageName=s.StageName);

DECLARE @WorkflowTransitionCatalog TABLE
(
    TransitionCode NVARCHAR(80) NOT NULL PRIMARY KEY,
    StageName NVARCHAR(80) NOT NULL,
    FromStatus NVARCHAR(100) NOT NULL,
    ActionName NVARCHAR(50) NOT NULL,
    ToStatus NVARCHAR(100) NOT NULL,
    ActionStatus NVARCHAR(20) NOT NULL,
    RoleCode NVARCHAR(40) NOT NULL,
    PermissionKey NVARCHAR(120) NOT NULL
);

INSERT INTO @WorkflowTransitionCatalog
    (TransitionCode,StageName,FromStatus,ActionName,ToStatus,ActionStatus,RoleCode,PermissionKey)
VALUES
    (N'JE_SUBMIT_TENDER',N'Final_JE_SubmitTender',N'Draft',N'Submit',N'Submitted',N'Submitted',N'Junior Engineer (JE)',N'tender.create.department'),
    (N'JE_PUBLISH_TENDER',N'Final_JE_PublishTender',N'Submitted',N'Publish',N'Tender Published',N'Completed',N'Junior Engineer (JE)',N'tender.publication.manage'),
    (N'JE_OPEN_TECHNICAL_BIDS',N'Final_JE_OpenTechnicalBids',N'Tender Published',N'OpenTechnicalBids',N'Primary Verification Pending',N'Completed',N'Junior Engineer (JE)',N'tender.primary.open'),
    (N'CELL_APPROVE_DOCUMENTS',N'Final_Cell_CompleteSecondaryVerification',N'Primary Verification Pending',N'Approve',N'Financial Bid Opening Approval Pending',N'Approved',N'TENDER_CELL',N'tender.cell.verify'),
    (N'CELL_CREATE_SHORTFALL',N'Final_Cell_CreateShortfall',N'Primary Verification Pending',N'CreateShortfall',N'Shortfall Pending',N'Completed',N'TENDER_CELL',N'shortfall.create'),
    (N'CELL_CLEAR_SHORTFALL',N'Final_Cell_ClearShortfall',N'Shortfall Documents Submitted',N'ClearShortfall',N'Financial Bid Opening Approval Pending',N'Completed',N'TENDER_CELL',N'shortfall.clear'),
    (N'CHAIRMAN_APPROVE_FINANCIAL_OPENING',N'Final_Samiti_ApproveFinancialOpening',N'Financial Bid Opening Approval Pending',N'Approve',N'Financial Bid Opening Approved',N'Approved',N'CHAIRMAN',N'financial.open.approve'),
    (N'JE_ENTER_FINANCIAL_DATA',N'Final_JE_OpenFinancialBids',N'Financial Bid Opening Approved',N'EnterFinancialData',N'Final Financial Approval Pending',N'Completed',N'Junior Engineer (JE)',N'financial.bids.enter'),
    (N'AUDITOR_APPROVE_FINANCIAL_DATA',N'Final_Auditor_ApproveFinancialBids',N'Final Financial Approval Pending',N'Approve',N'Final Financial Approval Completed',N'Approved',N'DIRECTOR_AUDITOR',N'financial.approve.final'),
    (N'JE_GENERATE_WORK_ORDER',N'Final_JE_GenerateWorkOrder',N'Final Financial Approval Completed',N'GenerateWorkOrder',N'Work Order Generated',N'Completed',N'Junior Engineer (JE)',N'workorder.manage'),
    (N'JE_GENERATE_AGREEMENT',N'Final_JE_GenerateAgreement',N'Work Order Generated',N'GenerateAgreement',N'Tender Agreement Generated',N'Completed',N'Junior Engineer (JE)',N'agreement.manage');

INSERT INTO dbo.WorkflowTransitions
    (TransitionCode,WorkflowVersion,StageId,FromStatus,ActionName,ToStatus,ActionStatus,RoleId,PermissionKey,IsActive)
SELECT c.TransitionCode,2,s.StageId,c.FromStatus,c.ActionName,c.ToStatus,c.ActionStatus,r.RoleId,c.PermissionKey,1
FROM @WorkflowTransitionCatalog c
JOIN dbo.WorkflowStages s ON s.StageName=c.StageName AND s.WorkflowVersion=2
JOIN dbo.Roles r ON r.RoleCode=c.RoleCode
WHERE NOT EXISTS
(
    SELECT 1 FROM dbo.WorkflowTransitions existing
    WHERE existing.TransitionCode=c.TransitionCode
);

UPDATE dbo.WorkflowTransitions
SET ActionName=N'Approve',
    ActionStatus=N'Approved'
WHERE TransitionCode=N'CELL_APPROVE_DOCUMENTS'
  AND ActionName=N'ApproveDocuments';

INSERT INTO dbo.TenderSchemaMigrations(MigrationId,Description)
SELECT N'13-FOUR-ROLE',N'Four-role RBAC, external bidder account type, and configured tender workflow'
WHERE OBJECT_ID(N'dbo.TenderSchemaMigrations',N'U') IS NOT NULL
  AND NOT EXISTS(SELECT 1 FROM dbo.TenderSchemaMigrations WHERE MigrationId=N'13-FOUR-ROLE');

IF COL_LENGTH(N'dbo.ApprovalHistory', N'RoleAtAction') IS NOT NULL
    ALTER TABLE dbo.ApprovalHistory ALTER COLUMN RoleAtAction NVARCHAR(100) NULL;
IF OBJECT_ID(N'dbo.TenderLifecycleHistory', N'U') IS NOT NULL
    ALTER TABLE dbo.TenderLifecycleHistory ALTER COLUMN RoleAtAction NVARCHAR(100) NOT NULL;

IF (SELECT COUNT(*) FROM dbo.Roles) <> 4
   OR (SELECT COUNT(*) FROM dbo.Roles WHERE RoleCode IN (N'Junior Engineer (JE)',N'TENDER_CELL',N'CHAIRMAN',N'DIRECTOR_AUDITOR')) <> 4
    THROW 51307, 'Four-role post-migration validation failed.', 1;

IF EXISTS
(
    SELECT 1
    FROM dbo.RolePermissions rp
    JOIN dbo.Roles r ON r.RoleId=rp.RoleId
    WHERE r.RoleCode IN (N'Junior Engineer (JE)',N'TENDER_CELL',N'CHAIRMAN',N'DIRECTOR_AUDITOR')
      AND NOT EXISTS
      (
          SELECT 1 FROM #ExpectedRolePermissions expected
          JOIN dbo.Permissions p ON p.PermissionKey=expected.PermissionKey
          WHERE expected.RoleCode=r.RoleCode AND p.PermissionId=rp.PermissionId
      )
)
    THROW 51308, 'Unexpected role permissions remain after migration.', 1;

INSERT INTO dbo.TenderWorkflowAssignments
    (TenderId,StageId,AssignedRoleId,DepartmentId,AssignedAtUtc,IsCurrent)
SELECT tender.TenderId,nextAction.StageId,nextAction.RoleId,tender.DepartmentId,SYSUTCDATETIME(),1
FROM dbo.Tender tender
CROSS APPLY
(
    SELECT TOP (1) transition.StageId,transition.RoleId
    FROM dbo.WorkflowTransitions transition
    INNER JOIN dbo.WorkflowStages stage
        ON stage.StageId=transition.StageId
       AND stage.WorkflowVersion=transition.WorkflowVersion
       AND stage.IsActive=1
    WHERE transition.WorkflowVersion=tender.WorkflowVersion
      AND transition.WorkflowVersion=2
      AND transition.FromStatus=tender.CurrentStatus
      AND transition.IsActive=1
    ORDER BY stage.StageOrder,transition.TransitionId
) nextAction
WHERE tender.IsDeleted=0
  AND tender.WorkflowVersion=2
  AND NOT EXISTS
  (
      SELECT 1
      FROM dbo.TenderWorkflowAssignments assignment
      WHERE assignment.TenderId=tender.TenderId
        AND assignment.ShortfallId IS NULL
        AND assignment.IsCurrent=1
  );

COMMIT TRANSACTION;

GO

CREATE OR ALTER PROCEDURE dbo.sp_UpdateTenderStatus
    @TenderId INT,
    @TransitionCode NVARCHAR(80),
    @ExpectedStatus NVARCHAR(100),
    @ApprovedBy INT,
    @RoleId INT,
    @Remarks NVARCHAR(1000)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    DECLARE @NewStatus NVARCHAR(100);
    DECLARE @StageName NVARCHAR(80);
    DECLARE @RoleName NVARCHAR(100);
    DECLARE @ConfiguredFromStatus NVARCHAR(100);
    DECLARE @ConfiguredActionStatus NVARCHAR(20);
    DECLARE @ActionName NVARCHAR(50);
    DECLARE @TransitionStageId INT;
    DECLARE @TenderDepartmentId INT;
    DECLARE @AssignmentId BIGINT;
    DECLARE @NextStageId INT;
    DECLARE @NextRoleId INT;

    SELECT
        @NewStatus=transition.ToStatus,
        @StageName=stage.StageName,
        @RoleName=role.RoleName,
        @ConfiguredFromStatus=transition.FromStatus,
        @ConfiguredActionStatus=transition.ActionStatus,
        @ActionName=transition.ActionName,
        @TransitionStageId=transition.StageId
    FROM dbo.WorkflowTransitions transition WITH (UPDLOCK,HOLDLOCK)
    INNER JOIN dbo.WorkflowStages stage
        ON stage.StageId=transition.StageId
       AND stage.WorkflowVersion=transition.WorkflowVersion
       AND stage.IsActive=1
    INNER JOIN dbo.Roles role ON role.RoleId=transition.RoleId
    INNER JOIN dbo.Users actor ON actor.UserId=@ApprovedBy
    INNER JOIN dbo.RolePermissions rolePermission
        ON rolePermission.RoleId=role.RoleId
    INNER JOIN dbo.Permissions permission
        ON permission.PermissionId=rolePermission.PermissionId
       AND permission.PermissionKey=transition.PermissionKey
       AND permission.IsActive=1
    WHERE transition.TransitionCode=@TransitionCode
      AND transition.WorkflowVersion=2
      AND transition.IsActive=1
      AND transition.RoleId=@RoleId
      AND actor.RoleId=@RoleId
      AND actor.AccountType=N'Internal'
      AND actor.IsActive=1
      AND role.IsActive=1;

    IF @NewStatus IS NULL OR @ConfiguredFromStatus<>@ExpectedStatus
        THROW 51310, 'The requested workflow transition is not configured for this role.', 1;

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.Tender t WITH (UPDLOCK,HOLDLOCK)
        WHERE t.TenderId=@TenderId
          AND t.IsDeleted=0
          AND t.WorkflowVersion=2
          AND t.CurrentStatus=@ExpectedStatus
    )
        THROW 51311, 'Tender not found.', 1;

    SELECT @TenderDepartmentId=DepartmentId
    FROM dbo.Tender WITH (UPDLOCK,HOLDLOCK)
    WHERE TenderId=@TenderId;

    SELECT @AssignmentId=AssignmentId
    FROM dbo.TenderWorkflowAssignments WITH (UPDLOCK,HOLDLOCK)
    WHERE TenderId=@TenderId
      AND ShortfallId IS NULL
      AND IsCurrent=1
      AND AssignedRoleId=@RoleId
      AND DepartmentId=@TenderDepartmentId
      AND (AssignedUserId IS NULL OR AssignedUserId=@ApprovedBy);

    IF @AssignmentId IS NULL
        THROW 51314, 'The tender is not assigned to this user or role.', 1;

    UPDATE dbo.Tender
    SET CurrentStatus=@NewStatus,
        ModifiedBy=@ApprovedBy,
        ModifiedDate=SYSUTCDATETIME()
    WHERE TenderId=@TenderId
      AND CurrentStatus=@ExpectedStatus
      AND IsDeleted=0;

    IF @@ROWCOUNT<>1
        THROW 51313, 'Tender status changed; refresh and try again.', 1;

    UPDATE dbo.TenderWorkflowAssignments
    SET IsCurrent=0,
        CompletedAtUtc=SYSUTCDATETIME()
    WHERE AssignmentId=@AssignmentId
      AND IsCurrent=1;

    INSERT INTO dbo.ApprovalHistory
        (TenderId,ApprovalStage,PreviousStatus,NewStatus,Status,ApprovedBy,RoleAtAction,Remarks)
    VALUES
        (@TenderId,@StageName,@ExpectedStatus,@NewStatus,@ConfiguredActionStatus,@ApprovedBy,@RoleName,@Remarks);

    INSERT INTO dbo.TenderWorkflowHistory
        (TenderId,WorkflowVersion,StageId,PreviousStatus,NewStatus,ActionName,UserId,RoleId,DepartmentId,Remarks)
    VALUES
        (@TenderId,2,@TransitionStageId,@ExpectedStatus,@NewStatus,@ActionName,@ApprovedBy,@RoleId,@TenderDepartmentId,@Remarks);

    SELECT TOP (1)
        @NextStageId=transition.StageId,
        @NextRoleId=transition.RoleId
    FROM dbo.WorkflowTransitions transition
    INNER JOIN dbo.WorkflowStages stage
        ON stage.StageId=transition.StageId
       AND stage.WorkflowVersion=transition.WorkflowVersion
       AND stage.IsActive=1
    WHERE transition.WorkflowVersion=2
      AND transition.FromStatus=@NewStatus
      AND transition.IsActive=1
    ORDER BY stage.StageOrder,transition.TransitionId;

    IF @NextStageId IS NOT NULL
    BEGIN
        INSERT INTO dbo.TenderWorkflowAssignments
            (TenderId,StageId,AssignedRoleId,DepartmentId,AssignedAtUtc,IsCurrent)
        VALUES
            (@TenderId,@NextStageId,@NextRoleId,@TenderDepartmentId,SYSUTCDATETIME(),1);
    END;

    COMMIT TRANSACTION;
END;
GO

SELECT RoleCode,RoleId,RoleName,IsActive FROM dbo.Roles ORDER BY RoleId;
SELECT r.RoleCode,p.PermissionKey
FROM dbo.RolePermissions rp
JOIN dbo.Roles r ON r.RoleId=rp.RoleId
JOIN dbo.Permissions p ON p.PermissionId=rp.PermissionId
ORDER BY r.RoleCode,p.PermissionKey;
SELECT TransitionCode,FromStatus,ActionName,ToStatus,r.RoleCode,p.PermissionKey
FROM dbo.WorkflowTransitions t
JOIN dbo.Roles r ON r.RoleId=t.RoleId
JOIN dbo.Permissions p ON p.PermissionKey=t.PermissionKey
WHERE t.WorkflowVersion=2 AND t.IsActive=1
ORDER BY t.TransitionId;
