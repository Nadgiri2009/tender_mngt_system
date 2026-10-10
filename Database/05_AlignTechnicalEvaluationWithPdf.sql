/*
  Incremental patch: align workflow with PDF
  - Add TenderSection role (if missing)
  - Move TechnicalEvaluation approver to TenderSection
  - Add TenderEvaluation committee approval stage (TechnicalEvaluationApproved -> FinancialBidOpening)
  - Ensure a Tender Section test user exists
*/

SET NOCOUNT ON;

DECLARE @TenderSectionRoleId INT;
DECLARE @DepartmentId INT;

SELECT @TenderSectionRoleId = RoleId
FROM dbo.Roles
WHERE RoleName = 'TenderSection';

IF @TenderSectionRoleId IS NULL
BEGIN
    INSERT INTO dbo.Roles (RoleName, Description)
    VALUES ('TenderSection', 'Evaluates technical bids before Tender Committee approval');

    SET @TenderSectionRoleId = SCOPE_IDENTITY();
END;

-- Re-point Technical Evaluation stage to TenderSection
UPDATE dbo.WorkflowStages
SET ApproverRoleId = @TenderSectionRoleId
WHERE StageName = 'TechnicalEvaluation';

-- Add committee approval stage for technical evaluation if not exists
IF NOT EXISTS (
    SELECT 1
    FROM dbo.WorkflowStages
    WHERE StageName = 'TechnicalEvaluationCommitteeApproval'
)
BEGIN
    DECLARE @NextStageOrder INT;
    SELECT @NextStageOrder = ISNULL(MAX(StageOrder), 0) + 1 FROM dbo.WorkflowStages;

    INSERT INTO dbo.WorkflowStages
        (StageOrder, StageName, StatusOnPending, StatusOnApprove, StatusOnReject, ApproverRoleId)
    VALUES
        (@NextStageOrder, 'TechnicalEvaluationCommitteeApproval', 'Technical Evaluation Approved', 'Financial Bid Opening', 'Rejected',
         (SELECT RoleId FROM dbo.Roles WHERE RoleName = 'TenderCommittee'));
END;

SELECT TOP (1) @DepartmentId = DepartmentId FROM dbo.Departments ORDER BY DepartmentId;

IF NOT EXISTS (
    SELECT 1 FROM dbo.Users WHERE Email = 'tender.section@smc.gov.in'
)
BEGIN
    INSERT INTO dbo.Users
        (FullName, Email, Mobile, PasswordHash, RoleId, DepartmentId, IsActive, CreatedDate)
    VALUES
        ('Tender Section Officer', 'tender.section@smc.gov.in', '9000000010',
         '$2b$11$z7uw.OQun03PjjX7cnUvTOd.We4BMvxmQkS6KjWIkaK7mxBmCvm1u',
         @TenderSectionRoleId, @DepartmentId, 1, SYSUTCDATETIME());
END;

PRINT 'Technical evaluation workflow aligned with PDF.';
