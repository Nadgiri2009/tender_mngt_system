/*
  Add publication scheduling, extension review, bid opening and evaluation
  records to the existing Web Forms schema. Existing tender, user, approval,
  document and audit tables are reused. All lifecycle timestamps are UTC;
  Web Forms converts publication inputs and outputs to India Standard Time.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'dbo.TenderPublicationSchedule', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TenderPublicationSchedule
    (
        PublicationScheduleId INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_TenderPublicationSchedule PRIMARY KEY,
        TenderId INT NOT NULL
            CONSTRAINT UQ_TenderPublicationSchedule_Tender UNIQUE
            CONSTRAINT FK_TenderPublicationSchedule_Tender
                REFERENCES dbo.Tender(TenderId),
        PublicationStartUtc DATETIME2(0) NOT NULL,
        OriginalClosingUtc DATETIME2(0) NOT NULL,
        CurrentClosingUtc DATETIME2(0) NOT NULL,
        PublicationPeriodDays DECIMAL(10,2) NOT NULL,
        Remarks NVARCHAR(1000) NULL,
        ApprovedBy INT NOT NULL
            CONSTRAINT FK_TenderPublicationSchedule_ApprovedBy REFERENCES dbo.Users(UserId),
        ApprovedAtUtc DATETIME2(0) NOT NULL
            CONSTRAINT DF_TenderPublicationSchedule_ApprovedAt DEFAULT SYSUTCDATETIME(),
        ModifiedAtUtc DATETIME2(0) NOT NULL
            CONSTRAINT DF_TenderPublicationSchedule_ModifiedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT CK_TenderPublicationSchedule_Range
            CHECK (PublicationStartUtc < OriginalClosingUtc AND CurrentClosingUtc >= OriginalClosingUtc),
        CONSTRAINT CK_TenderPublicationSchedule_Days CHECK (PublicationPeriodDays > 0)
    );
    CREATE INDEX IX_TenderPublicationSchedule_CurrentClosing
        ON dbo.TenderPublicationSchedule(CurrentClosingUtc, TenderId);
END;
GO

IF OBJECT_ID(N'dbo.TenderExtensionRequests', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TenderExtensionRequests
    (
        ExtensionRequestId INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_TenderExtensionRequests PRIMARY KEY,
        TenderId INT NOT NULL
            CONSTRAINT FK_TenderExtensionRequests_Tender REFERENCES dbo.Tender(TenderId),
        ExtensionNumber TINYINT NOT NULL,
        OriginalClosingUtc DATETIME2(0) NOT NULL,
        ProposedClosingUtc DATETIME2(0) NOT NULL,
        ApplicationFileName NVARCHAR(260) NOT NULL,
        ApplicationFilePath NVARCHAR(400) NOT NULL,
        SupportingFileName NVARCHAR(260) NOT NULL,
        SupportingFilePath NVARCHAR(400) NOT NULL,
        Reason NVARCHAR(1000) NOT NULL,
        SubmittedBy INT NOT NULL
            CONSTRAINT FK_TenderExtensionRequests_SubmittedBy REFERENCES dbo.Users(UserId),
        SubmittedAtUtc DATETIME2(0) NOT NULL
            CONSTRAINT DF_TenderExtensionRequests_SubmittedAt DEFAULT SYSUTCDATETIME(),
        ReviewStatus NVARCHAR(20) NOT NULL
            CONSTRAINT DF_TenderExtensionRequests_Status DEFAULT N'Pending',
        ReviewedBy INT NULL
            CONSTRAINT FK_TenderExtensionRequests_ReviewedBy REFERENCES dbo.Users(UserId),
        ReviewedAtUtc DATETIME2(0) NULL,
        ReviewRemarks NVARCHAR(1000) NULL,
        CONSTRAINT CK_TenderExtensionRequests_Number CHECK (ExtensionNumber BETWEEN 1 AND 3),
        CONSTRAINT CK_TenderExtensionRequests_Range CHECK (ProposedClosingUtc > OriginalClosingUtc),
        CONSTRAINT CK_TenderExtensionRequests_Status CHECK (ReviewStatus IN (N'Pending',N'Approved',N'Rejected'))
    );
    CREATE INDEX IX_TenderExtensionRequests_TenderStatus
        ON dbo.TenderExtensionRequests(TenderId, ReviewStatus, ExtensionNumber);
    CREATE UNIQUE INDEX UX_TenderExtensionRequests_OnePending
        ON dbo.TenderExtensionRequests(TenderId) WHERE ReviewStatus=N'Pending';
END;
GO

IF OBJECT_ID(N'dbo.TenderBidOpening', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TenderBidOpening
    (
        BidOpeningId INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_TenderBidOpening PRIMARY KEY,
        TenderId INT NOT NULL
            CONSTRAINT UQ_TenderBidOpening_Tender UNIQUE
            CONSTRAINT FK_TenderBidOpening_Tender REFERENCES dbo.Tender(TenderId),
        OriginalClosingUtc DATETIME2(0) NOT NULL,
        CurrentClosingUtc DATETIME2(0) NOT NULL,
        ActualOpeningUtc DATETIME2(0) NOT NULL,
        OpenedBy INT NOT NULL
            CONSTRAINT FK_TenderBidOpening_OpenedBy REFERENCES dbo.Users(UserId),
        OperatorName NVARCHAR(150) NOT NULL,
        Designation NVARCHAR(150) NOT NULL,
        OpeningRemarks NVARCHAR(1000) NULL,
        OpeningStatus NVARCHAR(30) NOT NULL
            CONSTRAINT DF_TenderBidOpening_Status DEFAULT N'Opened',
        CONSTRAINT CK_TenderBidOpening_Status CHECK (OpeningStatus=N'Opened')
    );
END;
GO

IF OBJECT_ID(N'dbo.TenderEvaluationSettings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TenderEvaluationSettings
    (
        SettingsId TINYINT NOT NULL
            CONSTRAINT PK_TenderEvaluationSettings PRIMARY KEY
            CONSTRAINT CK_TenderEvaluationSettings_Singleton CHECK (SettingsId=1),
        StartDelayDays INT NOT NULL
            CONSTRAINT DF_TenderEvaluationSettings_Delay DEFAULT 1
            CONSTRAINT CK_TenderEvaluationSettings_Delay CHECK (StartDelayDays BETWEEN 0 AND 365),
        ModifiedBy INT NULL
            CONSTRAINT FK_TenderEvaluationSettings_ModifiedBy REFERENCES dbo.Users(UserId),
        ModifiedAtUtc DATETIME2(0) NOT NULL
            CONSTRAINT DF_TenderEvaluationSettings_ModifiedAt DEFAULT SYSUTCDATETIME()
    );
    INSERT INTO dbo.TenderEvaluationSettings(SettingsId, StartDelayDays)
        VALUES(1,1);
END;
GO

IF OBJECT_ID(N'dbo.TenderEvaluation', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TenderEvaluation
    (
        EvaluationId INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_TenderEvaluation PRIMARY KEY,
        TenderId INT NOT NULL
            CONSTRAINT UQ_TenderEvaluation_Tender UNIQUE
            CONSTRAINT FK_TenderEvaluation_Tender REFERENCES dbo.Tender(TenderId),
        EvaluationEligibleAtUtc DATETIME2(0) NOT NULL,
        EvaluationStatus NVARCHAR(30) NOT NULL
            CONSTRAINT DF_TenderEvaluation_Status DEFAULT N'Awaiting Evaluation',
        AssignedTeam NVARCHAR(50) NOT NULL
            CONSTRAINT DF_TenderEvaluation_Team DEFAULT N'TenderSection',
        StartedAtUtc DATETIME2(0) NULL,
        CompletedAtUtc DATETIME2(0) NULL,
        TechnicalOutcome NVARCHAR(1000) NULL,
        FinancialOutcome NVARCHAR(1000) NULL,
        Observations NVARCHAR(4000) NULL,
        Recommendation NVARCHAR(2000) NULL,
        ReportFileName NVARCHAR(260) NULL,
        ReportFilePath NVARCHAR(400) NULL,
        UpdatedBy INT NULL
            CONSTRAINT FK_TenderEvaluation_UpdatedBy REFERENCES dbo.Users(UserId),
        UpdatedAtUtc DATETIME2(0) NOT NULL
            CONSTRAINT DF_TenderEvaluation_UpdatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT CK_TenderEvaluation_Status CHECK
            (EvaluationStatus IN (N'Awaiting Evaluation',N'Under Evaluation',N'Evaluation Completed'))
    );
    CREATE INDEX IX_TenderEvaluation_Eligibility
        ON dbo.TenderEvaluation(EvaluationStatus, EvaluationEligibleAtUtc);
END;
GO

IF OBJECT_ID(N'dbo.TenderEvaluationDocuments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TenderEvaluationDocuments
    (
        EvaluationDocumentId INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_TenderEvaluationDocuments PRIMARY KEY,
        TenderId INT NOT NULL
            CONSTRAINT FK_TenderEvaluationDocuments_Tender REFERENCES dbo.Tender(TenderId),
        FileName NVARCHAR(260) NOT NULL,
        FilePath NVARCHAR(400) NOT NULL,
        UploadedBy INT NOT NULL
            CONSTRAINT FK_TenderEvaluationDocuments_User REFERENCES dbo.Users(UserId),
        UploadedAtUtc DATETIME2(0) NOT NULL
            CONSTRAINT DF_TenderEvaluationDocuments_UploadedAt DEFAULT SYSUTCDATETIME()
    );
    CREATE INDEX IX_TenderEvaluationDocuments_Tender
        ON dbo.TenderEvaluationDocuments(TenderId, UploadedAtUtc);
END;
GO

CREATE OR ALTER TRIGGER dbo.TR_TenderEvaluationDocuments_Immutable
ON dbo.TenderEvaluationDocuments
INSTEAD OF UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    THROW 51065, 'Tender evaluation document history is immutable.', 1;
END;
GO

IF OBJECT_ID(N'dbo.TenderLifecycleHistory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TenderLifecycleHistory
    (
        LifecycleHistoryId BIGINT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_TenderLifecycleHistory PRIMARY KEY,
        TenderId INT NOT NULL
            CONSTRAINT FK_TenderLifecycleHistory_Tender REFERENCES dbo.Tender(TenderId),
        PreviousStatus NVARCHAR(50) NULL,
        NewStatus NVARCHAR(50) NULL,
        ActionName NVARCHAR(80) NOT NULL,
        ActionBy INT NOT NULL
            CONSTRAINT FK_TenderLifecycleHistory_User REFERENCES dbo.Users(UserId),
        RoleAtAction NVARCHAR(50) NOT NULL,
        DepartmentId INT NULL
            CONSTRAINT FK_TenderLifecycleHistory_Department REFERENCES dbo.Departments(DepartmentId),
        ActionAtUtc DATETIME2(0) NOT NULL
            CONSTRAINT DF_TenderLifecycleHistory_ActionAt DEFAULT SYSUTCDATETIME(),
        Remarks NVARCHAR(2000) NULL,
        RelatedDocumentPath NVARCHAR(400) NULL
    );
    CREATE INDEX IX_TenderLifecycleHistory_TenderDate
        ON dbo.TenderLifecycleHistory(TenderId, ActionAtUtc);
END;
GO

IF OBJECT_ID(N'dbo.TenderBudgetOpinions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TenderBudgetOpinions
    (
        BudgetOpinionId INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_TenderBudgetOpinions PRIMARY KEY,
        TenderId INT NOT NULL
            CONSTRAINT FK_TenderBudgetOpinions_Tender REFERENCES dbo.Tender(TenderId),
        Opinion NVARCHAR(10) NOT NULL,
        ApprovedAmount DECIMAL(18,2) NULL,
        Remarks NVARCHAR(1000) NULL,
        ApprovedBy INT NOT NULL
            CONSTRAINT FK_TenderBudgetOpinions_User REFERENCES dbo.Users(UserId),
        OpinionAtUtc DATETIME2(0) NOT NULL
            CONSTRAINT DF_TenderBudgetOpinions_OpinionAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT CK_TenderBudgetOpinions_Opinion CHECK (Opinion IN (N'Positive',N'Negative')),
        CONSTRAINT CK_TenderBudgetOpinions_Amount CHECK
            ((Opinion=N'Positive' AND ApprovedAmount>0) OR (Opinion=N'Negative' AND ApprovedAmount IS NULL))
    );
    CREATE INDEX IX_TenderBudgetOpinions_TenderDate
        ON dbo.TenderBudgetOpinions(TenderId, OpinionAtUtc);
END;
GO

CREATE OR ALTER TRIGGER dbo.TR_TenderBidOpening_Immutable
ON dbo.TenderBidOpening
INSTEAD OF UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    THROW 51020, 'Bid-opening audit records are immutable.', 1;
END;
GO

CREATE OR ALTER TRIGGER dbo.TR_TenderLifecycleHistory_Immutable
ON dbo.TenderLifecycleHistory
INSTEAD OF UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    THROW 51021, 'Tender lifecycle history records are immutable.', 1;
END;
GO

CREATE OR ALTER TRIGGER dbo.TR_TenderBudgetOpinions_Immutable
ON dbo.TenderBudgetOpinions
INSTEAD OF UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    THROW 51073, 'Tender budget-opinion history records are immutable.', 1;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_SaveTenderPublicationSchedule
    @TenderId INT,
    @PublicationStartUtc DATETIME2(0),
    @ClosingUtc DATETIME2(0),
    @Remarks NVARCHAR(1000),
    @UserId INT,
    @RoleAtAction NVARCHAR(50),
    @DepartmentId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;

    DECLARE @Status NVARCHAR(50), @TenderDepartmentId INT, @CurrentEnd DATETIME2(0);
    SELECT @Status=t.CurrentStatus,@TenderDepartmentId=t.DepartmentId
    FROM dbo.Tender t WITH (UPDLOCK,HOLDLOCK)
    WHERE t.TenderId=@TenderId AND t.IsDeleted=0;

    IF @Status IS NULL THROW 51022, 'Tender not found.', 1;
    IF @RoleAtAction NOT IN (N'CAFOOfficer',N'Admin')
       OR NOT EXISTS
          (SELECT 1 FROM dbo.Users u JOIN dbo.Roles r ON r.RoleId=u.RoleId
           WHERE u.UserId=@UserId AND u.IsActive=1 AND r.RoleName=@RoleAtAction)
        THROW 51023, 'Only the authorized CAFO or administrator can approve a publication schedule.', 1;
    IF @DepartmentId IS NOT NULL AND @DepartmentId<>@TenderDepartmentId
        THROW 51024, 'The tender is outside your department.', 1;
    IF @RoleAtAction<>N'Admin' AND EXISTS
       (SELECT 1 FROM dbo.Users WHERE UserId=@UserId AND DepartmentId IS NOT NULL
        AND DepartmentId<>@TenderDepartmentId)
        THROW 51068, 'The tender is outside your assigned department.', 1;
    IF @Status<>N'Tender Published'
        THROW 51025, 'A publication schedule can only be configured for a tender approved for publication.', 1;
    IF @PublicationStartUtc<=SYSUTCDATETIME() OR @ClosingUtc<=@PublicationStartUtc
        THROW 51026, 'Publication dates must be valid and start in the future.', 1;
    IF EXISTS (SELECT 1 FROM dbo.TenderBidOpening WHERE TenderId=@TenderId)
        THROW 51027, 'A schedule cannot be changed after bid opening.', 1;
    IF EXISTS (SELECT 1 FROM dbo.TechnicalBid WHERE TenderId=@TenderId)
        THROW 51028, 'A schedule cannot be changed after bids have been received.', 1;
    IF EXISTS (SELECT 1 FROM dbo.FinancialBid WHERE TenderId=@TenderId)
        THROW 51067, 'A schedule cannot be changed after bids have been received.', 1;

    SELECT @CurrentEnd=CurrentClosingUtc
    FROM dbo.TenderPublicationSchedule WITH (UPDLOCK,HOLDLOCK)
    WHERE TenderId=@TenderId;

    IF @CurrentEnd IS NOT NULL AND @CurrentEnd<=SYSUTCDATETIME()
        THROW 51029, 'An active publication schedule cannot be changed after it starts.', 1;

    DECLARE @Days DECIMAL(10,2)=CAST(DATEDIFF_BIG(SECOND,@PublicationStartUtc,@ClosingUtc)/86400.0 AS DECIMAL(10,2));
    IF @Days<=0 THROW 51030, 'The publication period must be at least one hundredth of a day.', 1;

    IF @CurrentEnd IS NULL
    BEGIN
        INSERT INTO dbo.TenderPublicationSchedule
            (TenderId,PublicationStartUtc,OriginalClosingUtc,CurrentClosingUtc,PublicationPeriodDays,Remarks,ApprovedBy)
        VALUES
            (@TenderId,@PublicationStartUtc,@ClosingUtc,@ClosingUtc,@Days,@Remarks,@UserId);
    END
    ELSE
    BEGIN
        UPDATE dbo.TenderPublicationSchedule
        SET PublicationStartUtc=@PublicationStartUtc,
            OriginalClosingUtc=@ClosingUtc,
            CurrentClosingUtc=@ClosingUtc,
            PublicationPeriodDays=@Days,
            Remarks=@Remarks,
            ApprovedBy=@UserId,
            ApprovedAtUtc=SYSUTCDATETIME(),
            ModifiedAtUtc=SYSUTCDATETIME()
        WHERE TenderId=@TenderId;
    END;

    INSERT INTO dbo.TenderLifecycleHistory
        (TenderId,PreviousStatus,NewStatus,ActionName,ActionBy,RoleAtAction,DepartmentId,Remarks)
    VALUES(@TenderId,@Status,@Status,N'Publication schedule approved',@UserId,@RoleAtAction,@DepartmentId,@Remarks);
    INSERT INTO dbo.AuditLog(UserId,Action,EntityName,EntityId,Details)
    VALUES(@UserId,N'ApprovePublicationSchedule',N'TenderPublicationSchedule',@TenderId,
           CONCAT(N'Start UTC: ',CONVERT(NVARCHAR(30),@PublicationStartUtc,126),
                  N'; closing UTC: ',CONVERT(NVARCHAR(30),@ClosingUtc,126),N'; days: ',@Days));

    COMMIT TRANSACTION;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_CreateTenderExtensionRequest
    @TenderId INT,
    @ProposedClosingUtc DATETIME2(0),
    @ApplicationFileName NVARCHAR(260),
    @ApplicationFilePath NVARCHAR(400),
    @SupportingFileName NVARCHAR(260),
    @SupportingFilePath NVARCHAR(400),
    @Reason NVARCHAR(1000),
    @UserId INT,
    @RoleAtAction NVARCHAR(50),
    @DepartmentId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;

    DECLARE @Status NVARCHAR(50), @TenderDepartmentId INT, @ClosingUtc DATETIME2(0), @ExtensionNo INT;
    SELECT @Status=t.CurrentStatus,@TenderDepartmentId=t.DepartmentId
    FROM dbo.Tender t WITH (UPDLOCK,HOLDLOCK)
    WHERE t.TenderId=@TenderId AND t.IsDeleted=0;
    IF @Status IS NULL THROW 51031, 'Tender not found.', 1;
    IF @RoleAtAction NOT IN (N'DepartmentOfficer',N'Admin')
       OR NOT EXISTS (SELECT 1 FROM dbo.Users u JOIN dbo.Roles r ON r.RoleId=u.RoleId
                      WHERE u.UserId=@UserId AND u.IsActive=1 AND r.RoleName=@RoleAtAction)
        THROW 51032, 'Only the authorized department officer can request an extension.', 1;
    IF @DepartmentId IS NOT NULL AND @DepartmentId<>@TenderDepartmentId
        THROW 51033, 'The tender is outside your department.', 1;
    IF @RoleAtAction<>N'Admin' AND EXISTS
       (SELECT 1 FROM dbo.Users WHERE UserId=@UserId AND DepartmentId IS NOT NULL
        AND DepartmentId<>@TenderDepartmentId)
        THROW 51069, 'The tender is outside your assigned department.', 1;
    IF @RoleAtAction<>N'Admin' AND
       NOT EXISTS (SELECT 1 FROM dbo.Tender t WHERE t.TenderId=@TenderId AND t.CreatedBy=@UserId)
        THROW 51034, 'You may only request extensions for tenders you own.', 1;
    IF @Status<>N'Tender Published' OR EXISTS (SELECT 1 FROM dbo.TenderBidOpening WHERE TenderId=@TenderId)
        THROW 51035, 'Extensions are unavailable after bid opening or evaluation begins.', 1;
    SELECT @ClosingUtc=CurrentClosingUtc
    FROM dbo.TenderPublicationSchedule WITH (UPDLOCK,HOLDLOCK)
    WHERE TenderId=@TenderId;
    IF @ClosingUtc IS NULL THROW 51036, 'The tender has no approved publication schedule.', 1;
    IF @ClosingUtc<=SYSUTCDATETIME() THROW 51037, 'An extension must be requested before publication closes.', 1;
    IF @ProposedClosingUtc<=@ClosingUtc THROW 51038, 'The proposed closing time must be after the current closing time.', 1;
    IF EXISTS (SELECT 1 FROM dbo.TenderExtensionRequests WITH (UPDLOCK,HOLDLOCK)
               WHERE TenderId=@TenderId AND ReviewStatus=N'Pending')
        THROW 51039, 'An extension request is already awaiting review.', 1;

    SELECT @ExtensionNo=COUNT(*)+1
    FROM dbo.TenderExtensionRequests WITH (UPDLOCK,HOLDLOCK)
    WHERE TenderId=@TenderId AND ReviewStatus=N'Approved';
    IF @ExtensionNo>3 THROW 51040, 'The maximum of three approved extensions has been reached.', 1;

    INSERT INTO dbo.TenderExtensionRequests
        (TenderId,ExtensionNumber,OriginalClosingUtc,ProposedClosingUtc,
         ApplicationFileName,ApplicationFilePath,SupportingFileName,SupportingFilePath,
         Reason,SubmittedBy)
    VALUES
        (@TenderId,@ExtensionNo,@ClosingUtc,@ProposedClosingUtc,
         @ApplicationFileName,@ApplicationFilePath,@SupportingFileName,@SupportingFilePath,
         @Reason,@UserId);

    DECLARE @RequestId INT=CONVERT(INT,SCOPE_IDENTITY());
    INSERT INTO dbo.TenderLifecycleHistory
        (TenderId,PreviousStatus,NewStatus,ActionName,ActionBy,RoleAtAction,DepartmentId,Remarks,RelatedDocumentPath)
    VALUES(@TenderId,@Status,@Status,N'Extension requested',@UserId,@RoleAtAction,@DepartmentId,@Reason,@ApplicationFilePath);
    INSERT INTO dbo.AuditLog(UserId,Action,EntityName,EntityId,Details)
    VALUES(@UserId,N'Submit',N'TenderExtensionRequest',@RequestId,@Reason);
    COMMIT TRANSACTION;
    SELECT @RequestId AS ExtensionRequestId;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_ReviewTenderExtensionRequest
    @ExtensionRequestId INT,
    @Decision NVARCHAR(10),
    @UserId INT,
    @RoleAtAction NVARCHAR(50),
    @DepartmentId INT = NULL,
    @Remarks NVARCHAR(1000) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;

    IF @Decision NOT IN (N'Approve',N'Reject')
        THROW 51041, 'Choose approve or reject.', 1;
    IF @RoleAtAction NOT IN (N'CAFOOfficer',N'Admin')
       OR NOT EXISTS (SELECT 1 FROM dbo.Users u JOIN dbo.Roles r ON r.RoleId=u.RoleId
                      WHERE u.UserId=@UserId AND u.IsActive=1 AND r.RoleName=@RoleAtAction)
        THROW 51042, 'Only the authorized CAFO or administrator can review extensions.', 1;

    DECLARE @TenderId INT,@TenderStatus NVARCHAR(50),@TenderDepartmentId INT,
            @OriginalClosing DATETIME2(0),@ProposedClosing DATETIME2(0),@ScheduleClosing DATETIME2(0),
            @SubmittedBy INT,@ExtensionNo INT;
    SELECT @TenderId=e.TenderId,@OriginalClosing=e.OriginalClosingUtc,@ProposedClosing=e.ProposedClosingUtc,
           @SubmittedBy=e.SubmittedBy,@ExtensionNo=e.ExtensionNumber
    FROM dbo.TenderExtensionRequests e WITH (UPDLOCK,HOLDLOCK)
    WHERE e.ExtensionRequestId=@ExtensionRequestId AND e.ReviewStatus=N'Pending';
    IF @TenderId IS NULL THROW 51043, 'The extension request is no longer pending.', 1;
    SELECT @TenderStatus=t.CurrentStatus,@TenderDepartmentId=t.DepartmentId
    FROM dbo.Tender t WITH (UPDLOCK,HOLDLOCK)
    WHERE t.TenderId=@TenderId AND t.IsDeleted=0;
    IF @DepartmentId IS NOT NULL AND @DepartmentId<>@TenderDepartmentId
        THROW 51044, 'The tender is outside your department.', 1;
    IF @RoleAtAction<>N'Admin' AND EXISTS
       (SELECT 1 FROM dbo.Users WHERE UserId=@UserId AND DepartmentId IS NOT NULL
        AND DepartmentId<>@TenderDepartmentId)
        THROW 51070, 'The tender is outside your assigned department.', 1;
    IF @TenderStatus<>N'Tender Published' OR EXISTS (SELECT 1 FROM dbo.TenderBidOpening WHERE TenderId=@TenderId)
        THROW 51045, 'The tender has entered bid opening or evaluation.', 1;

    SELECT @ScheduleClosing=CurrentClosingUtc
    FROM dbo.TenderPublicationSchedule WITH (UPDLOCK,HOLDLOCK)
    WHERE TenderId=@TenderId;
    IF @ScheduleClosing IS NULL OR @ScheduleClosing<>@OriginalClosing
        THROW 51046, 'The publication schedule changed; the request must be resubmitted.', 1;

    IF @Decision=N'Approve'
    BEGIN
        IF (SELECT COUNT(*) FROM dbo.TenderExtensionRequests WITH (UPDLOCK,HOLDLOCK)
            WHERE TenderId=@TenderId AND ReviewStatus=N'Approved')>=3
            THROW 51047, 'The maximum of three approved extensions has been reached.', 1;
        UPDATE dbo.TenderPublicationSchedule
        SET CurrentClosingUtc=@ProposedClosing,ModifiedAtUtc=SYSUTCDATETIME()
        WHERE TenderId=@TenderId;
    END;

    UPDATE dbo.TenderExtensionRequests
    SET ReviewStatus=CASE WHEN @Decision=N'Approve' THEN N'Approved' ELSE N'Rejected' END,
        ReviewedBy=@UserId,ReviewedAtUtc=SYSUTCDATETIME(),ReviewRemarks=@Remarks
    WHERE ExtensionRequestId=@ExtensionRequestId AND ReviewStatus=N'Pending';

    INSERT INTO dbo.ApprovalHistory
        (TenderId,ApprovalStage,PreviousStatus,NewStatus,Status,ApprovedBy,RoleAtAction,Remarks)
    VALUES(@TenderId,N'PublicationExtension',@TenderStatus,@TenderStatus,
           CASE WHEN @Decision=N'Approve' THEN N'Approved' ELSE N'Rejected' END,@UserId,@RoleAtAction,@Remarks);
    INSERT INTO dbo.TenderLifecycleHistory
        (TenderId,PreviousStatus,NewStatus,ActionName,ActionBy,RoleAtAction,DepartmentId,Remarks)
    VALUES(@TenderId,@TenderStatus,@TenderStatus,
           CASE WHEN @Decision=N'Approve' THEN N'Extension approved' ELSE N'Extension rejected' END,
           @UserId,@RoleAtAction,@DepartmentId,@Remarks);
    INSERT INTO dbo.AuditLog(UserId,Action,EntityName,EntityId,Details)
    VALUES(@UserId,@Decision,N'TenderExtensionRequest',@ExtensionRequestId,
           CONCAT(N'Extension ',@ExtensionNo,N'; submitted by user ',@SubmittedBy,N'; ',COALESCE(@Remarks,N'')));
    COMMIT TRANSACTION;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_OpenTenderBids
    @TenderId INT,
    @UserId INT,
    @RoleAtAction NVARCHAR(50),
    @DepartmentId INT = NULL,
    @Designation NVARCHAR(150),
    @Remarks NVARCHAR(1000) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;

    DECLARE @Status NVARCHAR(50),@TenderDepartmentId INT,@Number NVARCHAR(50),
            @Title NVARCHAR(250),@OperatorName NVARCHAR(150),@OriginalClosing DATETIME2(0),
            @CurrentClosing DATETIME2(0),@DelayDays INT,@Now DATETIME2(0)=SYSUTCDATETIME();
    SELECT @Status=t.CurrentStatus,@TenderDepartmentId=t.DepartmentId,
           @Number=t.TenderNumber,@Title=t.Title
    FROM dbo.Tender t WITH (UPDLOCK,HOLDLOCK)
    WHERE t.TenderId=@TenderId AND t.IsDeleted=0;
    IF @Status IS NULL THROW 51048, 'Tender not found.', 1;
    IF @RoleAtAction NOT IN (N'CAFOOfficer',N'TenderCommittee',N'TenderSection',N'Admin')
       OR NOT EXISTS (SELECT 1 FROM dbo.Users u JOIN dbo.Roles r ON r.RoleId=u.RoleId
                      WHERE u.UserId=@UserId AND u.IsActive=1 AND r.RoleName=@RoleAtAction)
        THROW 51049, 'Your role is not authorized to open tender bids.', 1;
    IF @DepartmentId IS NOT NULL AND @DepartmentId<>@TenderDepartmentId
        THROW 51050, 'The tender is outside your department.', 1;
    IF @RoleAtAction<>N'Admin' AND EXISTS
       (SELECT 1 FROM dbo.Users WHERE UserId=@UserId AND DepartmentId IS NOT NULL
        AND DepartmentId<>@TenderDepartmentId)
        THROW 51071, 'The tender is outside your assigned department.', 1;
    IF @Status<>N'Tender Published'
        THROW 51051, 'Only a published tender can be opened.', 1;
    SELECT @OriginalClosing=OriginalClosingUtc,@CurrentClosing=CurrentClosingUtc
    FROM dbo.TenderPublicationSchedule WITH (UPDLOCK,HOLDLOCK)
    WHERE TenderId=@TenderId AND PublicationStartUtc<=@Now;
    IF @CurrentClosing IS NULL THROW 51052, 'The tender has no active approved publication schedule.', 1;
    IF @CurrentClosing>@Now THROW 51053, 'Bids cannot be opened before the approved closing time.', 1;
    IF EXISTS (SELECT 1 FROM dbo.TenderBidOpening WHERE TenderId=@TenderId)
        THROW 51054, 'Bids have already been opened.', 1;
    SELECT @OperatorName=FullName FROM dbo.Users WHERE UserId=@UserId AND IsActive=1;
    SELECT @DelayDays=StartDelayDays FROM dbo.TenderEvaluationSettings WHERE SettingsId=1;
    IF @OperatorName IS NULL OR @DelayDays IS NULL THROW 51055, 'Opening user or evaluation settings are unavailable.', 1;

    INSERT INTO dbo.TenderBidOpening
        (TenderId,OriginalClosingUtc,CurrentClosingUtc,ActualOpeningUtc,OpenedBy,OperatorName,Designation,OpeningRemarks)
    VALUES(@TenderId,@OriginalClosing,@CurrentClosing,@Now,@UserId,@OperatorName,@Designation,@Remarks);
    INSERT INTO dbo.TenderEvaluation(TenderId,EvaluationEligibleAtUtc)
        VALUES(@TenderId,DATEADD(DAY,@DelayDays,@Now));

    UPDATE dbo.Tender SET CurrentStatus=N'Technical Bid Opening',ModifiedBy=@UserId,ModifiedDate=@Now
    WHERE TenderId=@TenderId AND CurrentStatus=N'Tender Published';
    IF @@ROWCOUNT<>1 THROW 51056, 'Tender status changed; reload before opening bids.', 1;
    INSERT INTO dbo.ApprovalHistory
        (TenderId,ApprovalStage,PreviousStatus,NewStatus,Status,ApprovedBy,RoleAtAction,Remarks,ApprovalDate)
    VALUES(@TenderId,N'TechnicalBidOpening',N'Tender Published',N'Technical Bid Opening',N'Opened',
           @UserId,@RoleAtAction,@Remarks,@Now);
    INSERT INTO dbo.TenderLifecycleHistory
        (TenderId,PreviousStatus,NewStatus,ActionName,ActionBy,RoleAtAction,DepartmentId,Remarks)
    VALUES(@TenderId,N'Tender Published',N'Technical Bid Opening',N'Bids opened',@UserId,@RoleAtAction,@DepartmentId,@Remarks);
    INSERT INTO dbo.AuditLog(UserId,Action,EntityName,EntityId,Details)
    VALUES(@UserId,N'Open',N'TenderBidOpening',@TenderId,
           CONCAT(N'Tender ',@Number,N' - ',@Title,N'; ',@OperatorName,N'; ',@Designation));
    COMMIT TRANSACTION;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_StartTenderEvaluation
    @TenderId INT,
    @UserId INT,
    @RoleAtAction NVARCHAR(50),
    @DepartmentId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;

    DECLARE @Status NVARCHAR(50),@TenderDepartmentId INT,@EligibleAt DATETIME2(0),@Now DATETIME2(0)=SYSUTCDATETIME();
    SELECT @Status=t.CurrentStatus,@TenderDepartmentId=t.DepartmentId
    FROM dbo.Tender t WITH (UPDLOCK,HOLDLOCK)
    WHERE t.TenderId=@TenderId AND t.IsDeleted=0;
    IF @Status IS NULL THROW 51057, 'Tender not found.', 1;
    IF @RoleAtAction NOT IN (N'TenderSection',N'TenderCommittee',N'Admin')
       OR NOT EXISTS (SELECT 1 FROM dbo.Users u JOIN dbo.Roles r ON r.RoleId=u.RoleId
                      WHERE u.UserId=@UserId AND u.IsActive=1 AND r.RoleName=@RoleAtAction)
        THROW 51058, 'Your role is not authorized to start evaluation.', 1;
    IF @DepartmentId IS NOT NULL AND @DepartmentId<>@TenderDepartmentId
        THROW 51059, 'The tender is outside your department.', 1;
    IF @RoleAtAction<>N'Admin' AND EXISTS
       (SELECT 1 FROM dbo.Users WHERE UserId=@UserId AND DepartmentId IS NOT NULL
        AND DepartmentId<>@TenderDepartmentId)
        THROW 51072, 'The tender is outside your assigned department.', 1;
    IF @Status<>N'Technical Bid Opening'
        THROW 51060, 'Tender is not awaiting evaluation.', 1;
    SELECT @EligibleAt=EvaluationEligibleAtUtc FROM dbo.TenderEvaluation WITH (UPDLOCK,HOLDLOCK)
    WHERE TenderId=@TenderId AND EvaluationStatus=N'Awaiting Evaluation';
    IF @EligibleAt IS NULL THROW 51061, 'Evaluation record not found.', 1;
    IF @Now<@EligibleAt THROW 51062, 'Evaluation is not yet eligible to start.', 1;

    UPDATE dbo.TenderEvaluation
    SET EvaluationStatus=N'Under Evaluation',StartedAtUtc=@Now,UpdatedBy=@UserId,UpdatedAtUtc=@Now
    WHERE TenderId=@TenderId AND EvaluationStatus=N'Awaiting Evaluation' AND EvaluationEligibleAtUtc<=@Now;
    IF @@ROWCOUNT<>1 THROW 51063, 'Evaluation was already started or is not eligible.', 1;
    UPDATE dbo.Tender SET CurrentStatus=N'Technical Evaluation',ModifiedBy=@UserId,ModifiedDate=@Now
    WHERE TenderId=@TenderId AND CurrentStatus=N'Technical Bid Opening';
    IF @@ROWCOUNT<>1 THROW 51064, 'Tender status changed; reload before starting evaluation.', 1;

    INSERT INTO dbo.ApprovalHistory(TenderId,ApprovalStage,PreviousStatus,NewStatus,Status,ApprovedBy,RoleAtAction,Remarks,ApprovalDate)
    VALUES(@TenderId,N'TenderEvaluation',N'Technical Bid Opening',N'Technical Evaluation',N'Started',@UserId,@RoleAtAction,NULL,@Now);
    INSERT INTO dbo.TenderLifecycleHistory(TenderId,PreviousStatus,NewStatus,ActionName,ActionBy,RoleAtAction,DepartmentId)
    VALUES(@TenderId,N'Technical Bid Opening',N'Technical Evaluation',N'Evaluation started',@UserId,@RoleAtAction,@DepartmentId);
    COMMIT TRANSACTION;
END;
GO

PRINT 'Tender lifecycle tables and guarded workflow procedures are ready.';
GO
