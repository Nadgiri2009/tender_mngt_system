/* =====================================================================
   INCREMENTAL SCRIPT: adds the Announcements table to an EXISTING
   TenderManagementDB. Skip this if you're running DatabaseSetup.sql
   fresh -- it already includes this table.
   ===================================================================== */

USE TenderManagementDB;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Announcements' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.Announcements (
        AnnouncementId  INT IDENTITY(1,1) PRIMARY KEY,
        Title           NVARCHAR(250)  NOT NULL,
        Body            NVARCHAR(MAX)  NULL,
        IsPublished     BIT            NOT NULL DEFAULT 0,
        PublishDate     DATETIME2      NULL,
        ExpiryDate      DATETIME2      NULL,
        CreatedBy       INT            NOT NULL,
        CreatedDate     DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
        ModifiedBy      INT            NULL,
        ModifiedDate    DATETIME2      NULL,
        IsDeleted       BIT            NOT NULL DEFAULT 0,
        CONSTRAINT FK_Announcements_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.Users(UserId)
    );

    IF COL_LENGTH('dbo.Departments', 'HeadOfficerName') IS NULL
    BEGIN
        ALTER TABLE dbo.Departments ADD HeadOfficerName NVARCHAR(150) NULL;
    END

    CREATE INDEX IX_Announcements_IsPublished ON dbo.Announcements(IsPublished, PublishDate);
END
GO
