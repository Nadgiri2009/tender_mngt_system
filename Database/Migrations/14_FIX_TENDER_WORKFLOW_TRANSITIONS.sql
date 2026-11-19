SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
SET NOCOUNT ON;

IF DB_NAME() <> N'tender_tracking'
    THROW 51400, 'This migration is scoped to the tender_tracking database only.', 1;

IF OBJECT_ID(N'dbo.Tender', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Users', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Roles', N'U') IS NULL
   OR OBJECT_ID(N'dbo.WorkflowTransitions', N'U') IS NULL
   OR OBJECT_ID(N'dbo.TenderWorkflowAssignments', N'U') IS NULL
   OR OBJECT_ID(N'dbo.TenderWorkflowHistory', N'U') IS NULL
   OR OBJECT_ID(N'dbo.ApprovalHistory', N'U') IS NULL
    THROW 51401, 'Run the four-role workflow migration before this migration.', 1;
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
    DECLARE @RoleCode NVARCHAR(40);
    DECLARE @ConfiguredFromStatus NVARCHAR(100);
    DECLARE @ConfiguredActionStatus NVARCHAR(20);
    DECLARE @ActionName NVARCHAR(50);
    DECLARE @TransitionStageId INT;
    DECLARE @TenderDepartmentId INT;
    DECLARE @ActorDepartmentId INT;
    DECLARE @AssignmentId BIGINT;
    DECLARE @NextStageId INT;
    DECLARE @NextRoleId INT;

    SELECT
        @NewStatus=transition.ToStatus,
        @StageName=stage.StageName,
        @RoleName=role.RoleName,
        @RoleCode=role.RoleCode,
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
        THROW 51402, 'The requested workflow transition is not configured for this role.', 1;

    SELECT @TenderDepartmentId=DepartmentId
    FROM dbo.Tender WITH (UPDLOCK,HOLDLOCK)
    WHERE TenderId=@TenderId
      AND IsDeleted=0
      AND WorkflowVersion=2
      AND CurrentStatus=@ExpectedStatus;

    IF @TenderDepartmentId IS NULL
        THROW 51403, 'Tender not found or its status has changed.', 1;

    SELECT @ActorDepartmentId=DepartmentId
    FROM dbo.Users
    WHERE UserId=@ApprovedBy;

    IF @RoleName=N'Junior Engineer (JE)'
       AND @RoleCode IN (N'JE',N'Junior Engineer (JE)')
       AND (@ActorDepartmentId IS NULL OR @ActorDepartmentId<>@TenderDepartmentId)
        THROW 51404, 'The tender is outside the assigned department.', 1;

    SELECT @AssignmentId=AssignmentId
    FROM dbo.TenderWorkflowAssignments WITH (UPDLOCK,HOLDLOCK)
    WHERE TenderId=@TenderId
      AND ShortfallId IS NULL
      AND IsCurrent=1
      AND AssignedRoleId=@RoleId
      AND DepartmentId=@TenderDepartmentId
      AND (AssignedUserId IS NULL OR AssignedUserId=@ApprovedBy);

    IF @AssignmentId IS NULL
        THROW 51405, 'The tender is not assigned to this user or role.', 1;

    UPDATE dbo.Tender
    SET CurrentStatus=@NewStatus,
        ModifiedBy=@ApprovedBy,
        ModifiedDate=SYSUTCDATETIME()
    WHERE TenderId=@TenderId
      AND CurrentStatus=@ExpectedStatus
      AND IsDeleted=0
      AND WorkflowVersion=2;

    IF @@ROWCOUNT<>1
        THROW 51406, 'Tender status changed; refresh and try again.', 1;

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

BEGIN TRANSACTION;
INSERT INTO dbo.TenderSchemaMigrations(MigrationId,Description)
SELECT N'14-WORKFLOW-TRANSITIONS',
       N'Enforce department-scoped JE transitions and maintain workflow assignments and history'
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.TenderSchemaMigrations
    WHERE MigrationId=N'14-WORKFLOW-TRANSITIONS'
);
COMMIT TRANSACTION;
GO
