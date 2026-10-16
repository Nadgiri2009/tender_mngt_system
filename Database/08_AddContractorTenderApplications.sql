/*
  Adds terms-acceptance and multi-document storage to the existing TechnicalBid
  application records. Safe to rerun; existing bid data is retained.
*/
USE TenderManagementDB;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF COL_LENGTH(N'dbo.TechnicalBid', N'TermsAcceptedAt') IS NULL
BEGIN
    ALTER TABLE dbo.TechnicalBid ADD TermsAcceptedAt DATETIME2 NULL;
END;

IF COL_LENGTH(N'dbo.TechnicalBid', N'TermsVersion') IS NULL
BEGIN
    ALTER TABLE dbo.TechnicalBid ADD TermsVersion NVARCHAR(50) NULL;
END;

IF OBJECT_ID(N'dbo.TechnicalBidDocuments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TechnicalBidDocuments (
        TechnicalBidDocumentId INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_TechnicalBidDocuments PRIMARY KEY,
        TechnicalBidId INT NOT NULL,
        FileName NVARCHAR(260) NOT NULL,
        FilePath NVARCHAR(400) NOT NULL,
        UploadedDate DATETIME2 NOT NULL
            CONSTRAINT DF_TechnicalBidDocuments_UploadedDate DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_TechnicalBidDocuments_TechnicalBid
            FOREIGN KEY (TechnicalBidId) REFERENCES dbo.TechnicalBid(TechnicalBidId) ON DELETE CASCADE
    );

    CREATE INDEX IX_TechnicalBidDocuments_TechnicalBidId
        ON dbo.TechnicalBidDocuments(TechnicalBidId);
END;
GO
