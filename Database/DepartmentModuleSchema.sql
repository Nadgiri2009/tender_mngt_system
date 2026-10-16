/*
  Department Web Forms module schema preflight.
  The module reuses the existing TenderManagementDB objects and makes no DDL
  changes. Run this against the target database before deployment. No seed
  records, stored procedures, or migrations are created by this script.
*/
USE TenderManagementDB;
GO

SET NOCOUNT ON;

DECLARE @RequiredTables TABLE (TableName sysname NOT NULL);
INSERT INTO @RequiredTables (TableName) VALUES
    (N'Roles'), (N'Departments'), (N'Users'), (N'Contractors'), (N'Tender'),
    (N'ApprovalHistory'), (N'TenderDocuments'), (N'BOQ'), (N'TechnicalBid'),
    (N'FinancialBid'), (N'Agreement'), (N'WorkOrder'), (N'WorkExecution'),
    (N'MeasurementBook'), (N'Bills'), (N'Payments'), (N'AuditLog'), (N'WorkflowStages');

IF EXISTS
(
    SELECT 1
    FROM @RequiredTables required
    WHERE OBJECT_ID(N'dbo.' + QUOTENAME(required.TableName), N'U') IS NULL
)
BEGIN
    SELECT required.TableName AS MissingTable
    FROM @RequiredTables required
    WHERE OBJECT_ID(N'dbo.' + QUOTENAME(required.TableName), N'U') IS NULL;
    THROW 51000, 'The database is missing required Tender Management tables. Apply the approved existing database setup scripts first.', 1;
END;

DECLARE @RequiredColumns TABLE (TableName sysname NOT NULL, ColumnName sysname NOT NULL);
INSERT INTO @RequiredColumns (TableName, ColumnName) VALUES
    (N'Tender',N'TenderNumber'), (N'Tender',N'TrackingId'), (N'Tender',N'DepartmentId'),
    (N'Tender',N'EstimatedAmount'), (N'Tender',N'CurrentStatus'), (N'Tender',N'CreatedBy'),
    (N'Tender',N'IsDeleted'), (N'Tender',N'SubmissionStartDate'),
    (N'ApprovalHistory',N'ApprovalStage'), (N'ApprovalHistory',N'ApprovedBy'),
    (N'TenderDocuments',N'FilePath'), (N'BOQ',N'Amount'), (N'Contractors',N'Name'),
    (N'Agreement',N'AgreementNo'), (N'WorkOrder',N'OrderNo'), (N'WorkExecution',N'ProgressPercentage'),
    (N'MeasurementBook',N'ApprovalStatus'), (N'Bills',N'Status'), (N'Payments',N'TransactionNo'),
    (N'WorkflowStages',N'ApproverRoleId'), (N'Users',N'RoleId');

IF EXISTS
(
    SELECT 1
    FROM @RequiredColumns required
    WHERE NOT EXISTS
    (
        SELECT 1 FROM sys.columns columnInfo
        WHERE columnInfo.object_id = OBJECT_ID(N'dbo.' + QUOTENAME(required.TableName))
          AND columnInfo.name = required.ColumnName
    )
)
BEGIN
    SELECT required.TableName, required.ColumnName AS MissingColumn
    FROM @RequiredColumns required
    WHERE NOT EXISTS
    (
        SELECT 1 FROM sys.columns columnInfo
        WHERE columnInfo.object_id = OBJECT_ID(N'dbo.' + QUOTENAME(required.TableName))
          AND columnInfo.name = required.ColumnName
    );
    THROW 51001, 'The database schema is incompatible with the department module. No changes were made.', 1;
END;

PRINT 'Department module schema preflight passed. Existing schema was not modified.';
GO
