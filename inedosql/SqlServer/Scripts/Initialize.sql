CREATE TABLE [__InedoDb_DbSchemaChanges]
(
    [Script_Id] INT IDENTITY(1, 1) NOT NULL,
    [Script_Guid] UNIQUEIDENTIFIER NOT NULL,
    [Script_Name] NVARCHAR(200) NOT NULL,
    [Script_Sql] VARBINARY(MAX) NULL,
    [Executed_Date] DATETIME NOT NULL,
    [Success_Indicator] CHAR(1) NOT NULL,
    [Error_Text] NVARCHAR(MAX) NULL,
    [ErrorResolved_Text] NVARCHAR(MAX) NULL,
    [ErrorResolved_Date] DATETIME NULL
 
    CONSTRAINT [PK__InedoDb_DbSchemaChanges]
        PRIMARY KEY CLUSTERED ([Script_Id]),

    CONSTRAINT [UQ__InedoDb_DbSchemaChanges]
        UNIQUE ([Script_Guid]),

    CONSTRAINT [CK__InedoDb_DbSchemaChanges__Success_Indicator]
        CHECK ([Success_Indicator] IN ('Y', 'N'))
)
