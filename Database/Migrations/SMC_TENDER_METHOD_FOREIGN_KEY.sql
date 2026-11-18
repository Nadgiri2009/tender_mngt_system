SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.Tender', N'U') IS NULL
    THROW 50001, 'Required table dbo.Tender was not found.', 1;

IF OBJECT_ID(N'dbo.TenderMethods', N'U') IS NULL
    THROW 50002, 'Required table dbo.TenderMethods was not found.', 1;

IF EXISTS
(
    SELECT 1
    FROM dbo.Tender AS tender
    LEFT JOIN dbo.TenderMethods AS method
        ON method.MethodName = tender.TenderType
    WHERE method.MethodName IS NULL
)
    THROW 50003, 'Tender rows contain method values without a matching TenderMethods record. Resolve these values before applying this migration.', 1;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_Tender_TenderMethods_TenderType'
      AND parent_object_id = OBJECT_ID(N'dbo.Tender')
)
BEGIN
    ALTER TABLE dbo.Tender WITH CHECK
        ADD CONSTRAINT FK_Tender_TenderMethods_TenderType
        FOREIGN KEY (TenderType)
        REFERENCES dbo.TenderMethods (MethodName)
        ON DELETE NO ACTION;
    ALTER TABLE dbo.Tender CHECK CONSTRAINT FK_Tender_TenderMethods_TenderType;
END;

COMMIT TRANSACTION;
