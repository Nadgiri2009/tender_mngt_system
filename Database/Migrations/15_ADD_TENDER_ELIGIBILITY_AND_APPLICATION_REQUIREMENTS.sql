SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH(N'dbo.Tender', N'EligibilityCriteria') IS NULL
BEGIN
    ALTER TABLE dbo.Tender
        ADD EligibilityCriteria nvarchar(4000) NULL;
END;

IF COL_LENGTH(N'dbo.TenderRequiredBidDocuments', N'Description') IS NULL
BEGIN
    ALTER TABLE dbo.TenderRequiredBidDocuments
        ADD Description nvarchar(1000) NULL;
END;

IF COL_LENGTH(N'dbo.TenderRequiredBidDocuments', N'IsMandatory') IS NULL
BEGIN
    ALTER TABLE dbo.TenderRequiredBidDocuments
        ADD IsMandatory bit NOT NULL
            CONSTRAINT DF_TenderRequiredBidDocuments_IsMandatory DEFAULT (1) WITH VALUES;
END;

IF COL_LENGTH(N'dbo.TenderRequiredBidDocuments', N'AllowedExtensions') IS NULL
BEGIN
    ALTER TABLE dbo.TenderRequiredBidDocuments
        ADD AllowedExtensions nvarchar(500) NULL;
END;

IF COL_LENGTH(N'dbo.TenderRequiredBidDocuments', N'MaxFileSizeBytes') IS NULL
BEGIN
    ALTER TABLE dbo.TenderRequiredBidDocuments
        ADD MaxFileSizeBytes int NOT NULL
            CONSTRAINT DF_TenderRequiredBidDocuments_MaxFileSizeBytes DEFAULT (10000000) WITH VALUES;
END;

COMMIT TRANSACTION;
