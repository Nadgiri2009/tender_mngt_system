SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.ContractorApplicationHistory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ContractorApplicationHistory
    (
        HistoryId bigint IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_ContractorApplicationHistory PRIMARY KEY,
        TechnicalBidId int NOT NULL,
        EventType nvarchar(40) NOT NULL,
        PreviousStatus nvarchar(30) NULL,
        NewStatus nvarchar(30) NULL,
        Remarks nvarchar(2000) NULL,
        CreatedBy int NOT NULL,
        RoleId int NULL,
        VisibleToContractor bit NOT NULL
            CONSTRAINT DF_ContractorApplicationHistory_VisibleToContractor DEFAULT (0),
        CreatedAtUtc datetime2(7) NOT NULL
            CONSTRAINT DF_ContractorApplicationHistory_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_ContractorApplicationHistory_TechnicalBid
            FOREIGN KEY (TechnicalBidId) REFERENCES dbo.TechnicalBid(TechnicalBidId) ON DELETE CASCADE,
        CONSTRAINT FK_ContractorApplicationHistory_User
            FOREIGN KEY (CreatedBy) REFERENCES dbo.Users(UserId),
        CONSTRAINT FK_ContractorApplicationHistory_Role
            FOREIGN KEY (RoleId) REFERENCES dbo.Roles(RoleId)
    );

    CREATE INDEX IX_ContractorApplicationHistory_TechnicalBidId_CreatedAtUtc
        ON dbo.ContractorApplicationHistory(TechnicalBidId, CreatedAtUtc);
END;

IF OBJECT_ID(N'dbo.ContractorApplicationRemarks', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ContractorApplicationRemarks
    (
        RemarkId bigint IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_ContractorApplicationRemarks PRIMARY KEY,
        TechnicalBidId int NOT NULL,
        RemarkType nvarchar(30) NOT NULL,
        Message nvarchar(2000) NOT NULL,
        CreatedBy int NOT NULL,
        RoleId int NULL,
        VisibleToContractor bit NOT NULL,
        CreatedAtUtc datetime2(7) NOT NULL
            CONSTRAINT DF_ContractorApplicationRemarks_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_ContractorApplicationRemarks_TechnicalBid
            FOREIGN KEY (TechnicalBidId) REFERENCES dbo.TechnicalBid(TechnicalBidId) ON DELETE CASCADE,
        CONSTRAINT FK_ContractorApplicationRemarks_User
            FOREIGN KEY (CreatedBy) REFERENCES dbo.Users(UserId),
        CONSTRAINT FK_ContractorApplicationRemarks_Role
            FOREIGN KEY (RoleId) REFERENCES dbo.Roles(RoleId)
    );

    CREATE INDEX IX_ContractorApplicationRemarks_TechnicalBidId_CreatedAtUtc
        ON dbo.ContractorApplicationRemarks(TechnicalBidId, CreatedAtUtc);
END;

COMMIT TRANSACTION;
