-- Apply once to an empty Azure SQL or SQL Server database with a deployment identity.
SET XACT_ABORT ON;
BEGIN TRANSACTION;

CREATE TABLE dbo.Campaigns (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Campaigns PRIMARY KEY,
    Name nvarchar(120) NOT NULL,
    StartDate datetime2(7) NOT NULL,
    EndDate datetime2(7) NOT NULL,
    CONSTRAINT CK_Campaigns_DateRange CHECK (EndDate > StartDate)
);

CREATE TABLE dbo.Events (
    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Events PRIMARY KEY,
    EventType nvarchar(80) NOT NULL,
    [Timestamp] datetime2(7) NOT NULL,
    CampaignId int NOT NULL,
    CONSTRAINT FK_Events_Campaigns_CampaignId FOREIGN KEY (CampaignId)
        REFERENCES dbo.Campaigns (Id) ON DELETE CASCADE
);

CREATE INDEX IX_Events_CampaignId_Timestamp_Id
    ON dbo.Events (CampaignId ASC, [Timestamp] DESC, Id DESC);

COMMIT TRANSACTION;
