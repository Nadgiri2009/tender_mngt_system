/*
  Development-only, rerunnable tender dashboard sample data.
  Change @EnableDashboardDemoData to 1 only after confirming this is a
  disposable development/test database. This script never updates or deletes
  existing tenders and uses deterministic tender numbers to prevent duplicates.
*/
USE TenderManagementDB;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @EnableDashboardDemoData BIT = 0;
IF @EnableDashboardDemoData <> 1
    THROW 51010, 'Demo data is disabled. Enable it only for a development/test database.', 1;

IF DB_NAME() <> N'TenderManagementDB'
    THROW 51011, 'Demo data may only be inserted into TenderManagementDB.', 1;

IF COL_LENGTH(N'dbo.Tender', N'SubmissionStartDate') IS NULL
    THROW 51012, 'Run 06_AddTenderSubmissionStartDate.sql before inserting dashboard sample data.', 1;

DECLARE @OfficerId INT;
DECLARE @OtherOfficerId INT;
DECLARE @DepartmentId INT;
DECLARE @AdminId INT;
DECLARE @TenderCommitteeId INT;
DECLARE @CompetentAuthorityId INT;
DECLARE @TenderSectionId INT;
DECLARE @TechnicalSanctionOfficerId INT;
DECLARE @GBOfficerId INT;
DECLARE @DPDCOfficerId INT;
DECLARE @CAFOOfficerId INT;
DECLARE @MCAOfficerId INT;
DECLARE @Now DATETIME2(0) = SYSUTCDATETIME();

SELECT TOP (1)
    @OfficerId = u.UserId,
    @DepartmentId = u.DepartmentId
FROM dbo.Users u
INNER JOIN dbo.Roles r ON r.RoleId = u.RoleId
WHERE u.IsActive = 1 AND r.RoleName = N'DepartmentOfficer'
ORDER BY u.UserId;

SELECT TOP (1) @OtherOfficerId = u.UserId
FROM dbo.Users u
INNER JOIN dbo.Roles r ON r.RoleId = u.RoleId
WHERE u.IsActive = 1 AND r.RoleName = N'DepartmentOfficer'
  AND u.UserId <> @OfficerId
ORDER BY u.UserId;

SELECT TOP (1) @DepartmentId = d.DepartmentId
FROM dbo.Departments d
WHERE d.IsActive = 1 AND @DepartmentId IS NULL
ORDER BY d.DepartmentId;

SELECT TOP (1) @AdminId = u.UserId
FROM dbo.Users u
INNER JOIN dbo.Roles r ON r.RoleId = u.RoleId
WHERE u.IsActive = 1 AND r.RoleName = N'Admin'
ORDER BY u.UserId;

SELECT TOP (1) @TenderCommitteeId = u.UserId
FROM dbo.Users u
INNER JOIN dbo.Roles r ON r.RoleId = u.RoleId
WHERE u.IsActive = 1 AND r.RoleName = N'TenderCommittee'
ORDER BY u.UserId;

SELECT TOP (1) @CompetentAuthorityId = u.UserId
FROM dbo.Users u
INNER JOIN dbo.Roles r ON r.RoleId = u.RoleId
WHERE u.IsActive = 1 AND r.RoleName = N'CompetentAuthority'
ORDER BY u.UserId;

SELECT TOP (1) @TenderSectionId = u.UserId
FROM dbo.Users u
INNER JOIN dbo.Roles r ON r.RoleId = u.RoleId
WHERE u.IsActive = 1 AND r.RoleName = N'TenderSection'
ORDER BY u.UserId;

SELECT TOP (1) @TechnicalSanctionOfficerId = u.UserId
FROM dbo.Users u
INNER JOIN dbo.Roles r ON r.RoleId = u.RoleId
WHERE u.IsActive = 1 AND r.RoleName = N'TechnicalSanctionOfficer'
ORDER BY u.UserId;

SELECT TOP (1) @GBOfficerId = u.UserId
FROM dbo.Users u
INNER JOIN dbo.Roles r ON r.RoleId = u.RoleId
WHERE u.IsActive = 1 AND r.RoleName = N'GBOfficer'
ORDER BY u.UserId;

SELECT TOP (1) @DPDCOfficerId = u.UserId
FROM dbo.Users u
INNER JOIN dbo.Roles r ON r.RoleId = u.RoleId
WHERE u.IsActive = 1 AND r.RoleName = N'DPDCOfficer'
ORDER BY u.UserId;

SELECT TOP (1) @CAFOOfficerId = u.UserId
FROM dbo.Users u
INNER JOIN dbo.Roles r ON r.RoleId = u.RoleId
WHERE u.IsActive = 1 AND r.RoleName = N'CAFOOfficer'
ORDER BY u.UserId;

SELECT TOP (1) @MCAOfficerId = u.UserId
FROM dbo.Users u
INNER JOIN dbo.Roles r ON r.RoleId = u.RoleId
WHERE u.IsActive = 1 AND r.RoleName = N'MCAOfficer'
ORDER BY u.UserId;

IF @OfficerId IS NULL OR @DepartmentId IS NULL OR @AdminId IS NULL
   OR @TenderCommitteeId IS NULL OR @CompetentAuthorityId IS NULL
   OR @TechnicalSanctionOfficerId IS NULL OR @GBOfficerId IS NULL OR @DPDCOfficerId IS NULL
   OR @CAFOOfficerId IS NULL OR @MCAOfficerId IS NULL
    THROW 51013, 'Active DepartmentOfficer, Admin, workflow approvers, and Department records are required.', 1;

DECLARE @DemoTenders TABLE
(
    TenderNumber NVARCHAR(50) NOT NULL PRIMARY KEY,
    OwnerId INT NULL,
    Title NVARCHAR(250) NOT NULL,
    Description NVARCHAR(MAX) NOT NULL,
    TenderType NVARCHAR(50) NOT NULL,
    EstimatedAmount DECIMAL(18,2) NOT NULL,
    CurrentStatus NVARCHAR(50) NOT NULL,
    CreatedOffset INT NOT NULL,
    StartOffset INT NULL,
    DeadlineOffset INT NOT NULL
);

INSERT INTO @DemoTenders
    (TenderNumber, OwnerId, Title, Description, TenderType, EstimatedAmount, CurrentStatus, CreatedOffset, StartOffset, DeadlineOffset)
VALUES
    (N'DEMO-DASH-2026-001', NULL, N'Road resurfacing and drainage improvements - Ward 4',
     N'DEMO DATA: Tender dashboard test record. Resurfacing of internal roads and renewal of storm-water drains in Ward 4.',
     N'Open', 4850000.00, N'Draft', -2, 14, 42),
    (N'DEMO-DASH-2026-002', NULL, N'Public garden maintenance and irrigation - Zone 2',
     N'DEMO DATA: Tender dashboard test record. Maintenance of public gardens, including seasonal planting and drip-irrigation repairs.',
     N'Limited', 1275000.00, N'Requirement Submitted', -4, 10, 36),
    (N'DEMO-DASH-2026-003', NULL, N'Electrical panel replacement at municipal facilities',
     N'DEMO DATA: Tender dashboard test record. Supply, installation, testing, and commissioning of electrical distribution panels.',
     N'Single', 2180000.00, N'TS Approval Pending', -7, 15, 45),
    (N'DEMO-DASH-2026-004', NULL, N'Solar high-mast lighting for public grounds',
     N'DEMO DATA: Tender dashboard test record. Installation and maintenance of solar-powered high-mast lights at municipal grounds.',
     N'Open', 3620000.00, N'Technical Evaluation', -30, -25, -16),
    (N'DEMO-DASH-2026-005', NULL, N'Solid-waste collection vehicles and route services',
     N'DEMO DATA: Tender dashboard test record. Provision of collection vehicles and scheduled route services for municipal zones.',
     N'Open', 7950000.00, N'Tender Published', -19, -7, 23),
    (N'DEMO-DASH-2026-006', NULL, N'Public building accessibility upgrades - Zone 1',
     N'DEMO DATA: Tender dashboard test record. Accessibility improvements including ramps, tactile paths, and accessible washrooms.',
     N'EOI', 1840000.00, N'Rejected', -50, -42, -35),
    (N'DEMO-DASH-2026-008', NULL, N'Ward 7 library roof repairs and reading-room refurbishment',
     N'DEMO DATA: Tender dashboard test record. Replace damaged roof sections, repair rainwater drainage, and refurbish the public reading room.',
     N'Open', 3260000.00, N'Tender Committee Approval', -3, -1, 12),
    (N'DEMO-DASH-2026-009', NULL, N'Community composting facility and organic-waste processing',
     N'DEMO DATA: Tender dashboard test record. Design, supply, and commission a community-scale organic-waste composting facility.',
     N'EOI', 6420000.00, N'Publication Approval Pending', -5, 5, 21),
    (N'DEMO-DASH-2026-010', NULL, N'Accessible bus shelters and passenger information boards',
     N'DEMO DATA: Tender dashboard test record. Construct accessible bus shelters and install weatherproof passenger information boards.',
     N'Limited', 4380000.00, N'GB Approval Pending', -12, -4, 7),
    (N'DEMO-DASH-2026-011', NULL, N'Emergency medical supplies for municipal health centres',
     N'DEMO DATA: Tender dashboard test record. Supply essential emergency medicines and consumables to municipal health centres.',
     N'Single', 1560000.00, N'Budget Approval Pending', -2, 12, 35),
    (N'DEMO-DASH-2026-012', NULL, N'Riverbank erosion protection and native planting works',
     N'DEMO DATA: Tender dashboard test record. Stabilize vulnerable riverbank sections and establish native vegetation for erosion control.',
     N'Open', 9180000.00, N'MCA Opinion', -15, 1, 16),
    (N'DEMO-PUBLIC-2026-001', NULL, N'Footpath accessibility improvements near municipal schools',
     N'DEMO DATA: Published public tender notice for accessible footpath improvements and tactile paving near municipal schools.',
     N'Open', 2740000.00, N'Tender Published', -12, -7, 18),
    (N'DEMO-PUBLIC-2026-002', NULL, N'Supply of vaccine refrigerators for urban health centres',
     N'DEMO DATA: Published public tender notice for supply, installation, and commissioning of vaccine refrigerators.',
     N'Limited', 1680000.00, N'Tender Published', -10, -5, 20),
    (N'DEMO-PUBLIC-2026-003', NULL, N'GIS mapping software licences and implementation support',
     N'DEMO DATA: Published public tender notice for GIS software licences, configuration, and implementation support.',
     N'Single', 3120000.00, N'Tender Published', -9, -4, 22),
    (N'DEMO-PUBLIC-2026-004', NULL, N'Expression of interest for operation of public parking facilities',
     N'DEMO DATA: Published expression of interest for operation and maintenance of designated public parking facilities.',
     N'EOI', 4250000.00, N'Tender Published', -8, -3, 24);

IF @OtherOfficerId IS NOT NULL
BEGIN
    INSERT INTO @DemoTenders
        (TenderNumber, OwnerId, Title, Description, TenderType, EstimatedAmount, CurrentStatus, CreatedOffset, StartOffset, DeadlineOffset)
    VALUES
        (N'DEMO-DASH-2026-007', @OtherOfficerId, N'Water-meter replacement and network testing - Zone 3',
         N'DEMO DATA: Tender dashboard test record. Replacement of aging water meters and pressure testing across Zone 3.',
         N'EOI', 2950000.00, N'TS Approval Pending', -1, 20, 50);
END;

BEGIN TRY
    BEGIN TRANSACTION;

    INSERT INTO dbo.Departments (DepartmentName, DepartmentCode)
    SELECT samples.DepartmentName, samples.DepartmentCode
    FROM (VALUES
        (N'Roads and Drainage', N'ROAD'),
        (N'Water Supply and Sewerage', N'WATER'),
        (N'Public Health', N'HEALTH'),
        (N'Solid Waste Management', N'SWM'),
        (N'Electrical and Street Lighting', N'ELECT'),
        (N'Garden and Parks', N'PARKS')
    ) samples (DepartmentName, DepartmentCode)
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.Departments existing
        WHERE existing.DepartmentName = samples.DepartmentName
    );

    INSERT INTO dbo.Tender
        (TenderNumber, Title, Description, DepartmentId, EstimatedAmount, TenderType, CurrentStatus,
         SubmissionStartDate, SubmissionDeadline, CreatedBy, CreatedDate, IsDeleted)
    SELECT d.TenderNumber, d.Title, d.Description, COALESCE(owner.DepartmentId,@DepartmentId),
           d.EstimatedAmount, d.TenderType,
           d.CurrentStatus,
           CASE WHEN d.StartOffset IS NULL THEN NULL ELSE DATEADD(DAY, d.StartOffset, @Now) END,
           DATEADD(DAY, d.DeadlineOffset, @Now),
           COALESCE(d.OwnerId,@OfficerId), DATEADD(DAY, d.CreatedOffset, @Now), 0
    FROM @DemoTenders d
    LEFT JOIN dbo.Users owner ON owner.UserId=d.OwnerId
    WHERE NOT EXISTS
    (
        SELECT 1 FROM dbo.Tender existing WHERE existing.TenderNumber = d.TenderNumber
    );

    INSERT INTO dbo.ApprovalHistory
        (TenderId, ApprovalStage, PreviousStatus, NewStatus, Status, ApprovedBy, RoleAtAction, Remarks, ApprovalDate)
    SELECT t.TenderId, h.ApprovalStage, h.PreviousStatus, h.NewStatus, h.Status,
           h.ApprovedBy, h.RoleAtAction, h.Remarks, DATEADD(DAY, h.ApprovalOffset, @Now)
    FROM
    (
        SELECT N'DEMO-DASH-2026-002' AS TenderNumber, N'RequirementSubmission' AS ApprovalStage,
               N'Draft' AS PreviousStatus, N'Requirement Submitted' AS NewStatus, N'Submitted' AS Status,
               @OfficerId AS ApprovedBy, N'DepartmentOfficer' AS RoleAtAction, CAST(NULL AS NVARCHAR(1000)) AS Remarks,
               -4 AS ApprovalOffset
        UNION ALL
        SELECT N'DEMO-DASH-2026-003', N'TechnicalSanction', N'Estimate Prepared', N'TS Approval Pending',
               N'Submitted', @OfficerId, N'DepartmentOfficer', NULL, -7
        UNION ALL
        SELECT N'DEMO-DASH-2026-004', N'PublicationApproval', N'Publication Approval Pending', N'Tender Published',
               N'Approved', @CompetentAuthorityId, N'CompetentAuthority', NULL, -28
        UNION ALL
        SELECT N'DEMO-DASH-2026-004', N'TechnicalEvaluation', N'Technical Bid Opening', N'Technical Evaluation',
               N'Submitted', @TenderCommitteeId, N'TenderCommittee', NULL, -12
        UNION ALL
        SELECT N'DEMO-DASH-2026-005', N'PublicationApproval', N'Publication Approval Pending', N'Tender Published',
               N'Approved', @CompetentAuthorityId, N'CompetentAuthority', NULL, -9
        UNION ALL
        SELECT N'DEMO-DASH-2026-006', N'PublicationApproval', N'Publication Approval Pending', N'Tender Published',
               N'Approved', @CompetentAuthorityId, N'CompetentAuthority', NULL, -45
        UNION ALL
        SELECT N'DEMO-DASH-2026-006', N'TechnicalEvaluation', N'Technical Evaluation', N'Rejected',
               N'Rejected', COALESCE(@TenderSectionId, @TenderCommitteeId),
               CASE WHEN @TenderSectionId IS NULL THEN N'TenderCommittee' ELSE N'TenderSection' END,
               N'Required technical specifications and supporting documents were incomplete.', -30
        UNION ALL
        SELECT N'DEMO-DASH-2026-008', N'TenderDraftPreparation', N'Budget Approved', N'Tender Draft Created',
               N'Submitted', @OfficerId, N'DepartmentOfficer', NULL, -3
        UNION ALL
        SELECT N'DEMO-DASH-2026-008', N'TenderCommitteeApproval', N'Tender Draft Created', N'Tender Committee Approval',
               N'Submitted', @OfficerId, N'DepartmentOfficer', NULL, -2
        UNION ALL
        SELECT N'DEMO-DASH-2026-009', N'TenderCommitteeApproval', N'Tender Committee Approval', N'Publication Approval Pending',
               N'Approved', @TenderCommitteeId, N'TenderCommittee', NULL, -5
        UNION ALL
        SELECT N'DEMO-DASH-2026-010', N'TechnicalSanction', N'TS Approval Pending', N'TS Approved',
               N'Approved', @TechnicalSanctionOfficerId, N'TechnicalSanctionOfficer', NULL, -13
        UNION ALL
        SELECT N'DEMO-DASH-2026-010', N'GBApproval', N'TS Approved', N'GB Approval Pending',
               N'Submitted', @OfficerId, N'DepartmentOfficer', NULL, -12
        UNION ALL
        SELECT N'DEMO-DASH-2026-011', N'BudgetApproval', N'Administrative Approved', N'Budget Approval Pending',
               N'Submitted', @OfficerId, N'DepartmentOfficer', NULL, -2
        UNION ALL
        SELECT N'DEMO-DASH-2026-012', N'CAFORateOpinion', N'Financial Bid Opening', N'CAFO Rate Opinion',
               N'Submitted', @TenderCommitteeId, N'TenderCommittee', NULL, -17
        UNION ALL
        SELECT N'DEMO-DASH-2026-012', N'CAFORateOpinion', N'CAFO Rate Opinion', N'Financial Evaluation',
               N'Approved', @CAFOOfficerId, N'CAFOOfficer', NULL, -16
        UNION ALL
        SELECT N'DEMO-DASH-2026-012', N'MCAOpinion', N'Financial Evaluation', N'MCA Opinion',
               N'Submitted', @TenderCommitteeId, N'TenderCommittee', NULL, -15
        UNION ALL
        SELECT N'DEMO-PUBLIC-2026-001', N'PublicationApproval', N'Publication Approval Pending', N'Tender Published',
               N'Approved', @CompetentAuthorityId, N'CompetentAuthority', NULL, -9
        UNION ALL
        SELECT N'DEMO-PUBLIC-2026-002', N'PublicationApproval', N'Publication Approval Pending', N'Tender Published',
               N'Approved', @CompetentAuthorityId, N'CompetentAuthority', NULL, -7
        UNION ALL
        SELECT N'DEMO-PUBLIC-2026-003', N'PublicationApproval', N'Publication Approval Pending', N'Tender Published',
               N'Approved', @CompetentAuthorityId, N'CompetentAuthority', NULL, -6
        UNION ALL
        SELECT N'DEMO-PUBLIC-2026-004', N'PublicationApproval', N'Publication Approval Pending', N'Tender Published',
               N'Approved', @CompetentAuthorityId, N'CompetentAuthority', NULL, -5
        UNION ALL
        SELECT N'DEMO-DASH-2026-007', N'TechnicalSanction', N'Estimate Prepared', N'TS Approval Pending',
               N'Submitted', @OtherOfficerId, N'DepartmentOfficer', NULL, -1
        WHERE @OtherOfficerId IS NOT NULL
    ) h
    INNER JOIN dbo.Tender t ON t.TenderNumber = h.TenderNumber AND t.IsDeleted = 0
        AND t.Description LIKE N'DEMO DATA:%'
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.ApprovalHistory existing
        WHERE existing.TenderId = t.TenderId
          AND existing.ApprovalStage = h.ApprovalStage
          AND existing.NewStatus = h.NewStatus
    );

    COMMIT TRANSACTION;

    SELECT t.TenderNumber, t.Title, t.TenderType, t.CurrentStatus, u.FullName AS AssignedRespOfficer,
           t.SubmissionStartDate, t.SubmissionDeadline
    FROM dbo.Tender t
    INNER JOIN dbo.Users u ON u.UserId=t.CreatedBy
    INNER JOIN @DemoTenders d ON d.TenderNumber = t.TenderNumber
    WHERE t.IsDeleted = 0
      AND t.Description LIKE N'DEMO DATA:%'
    ORDER BY t.TenderNumber;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
