/*
  Adds the optional tender submission opening date used by dashboard views.
  Safe to run repeatedly; existing tender rows remain unchanged.
*/
USE TenderManagementDB;
GO

IF COL_LENGTH(N'dbo.Tender', N'SubmissionStartDate') IS NULL
BEGIN
    ALTER TABLE dbo.Tender ADD SubmissionStartDate DATETIME2 NULL;
END;
GO
