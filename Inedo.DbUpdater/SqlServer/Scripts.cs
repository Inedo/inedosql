using System;
using System.Text;

namespace Inedo.DbUpdater.SqlServer;

internal static class Scripts
{
    public static string GetStruckTables => Encoding.UTF8.GetString(GetStruckTablesBytes);
    public static string Initialize => Encoding.UTF8.GetString(InitializeBytes);
    public static string MigrateV2toV3 => Encoding.UTF8.GetString(MigrateV2toV3Bytes);
    public static string ReadV1Scripts => Encoding.UTF8.GetString(ReadV1ScriptsBytes);
    public static string RecordExecution { get; } = Encoding.UTF8.GetString(RecordExecutionBytes);
    public static string UpdateExecution { get; } = Encoding.UTF8.GetString(UpdateExecutionBytes);
    public static string ResolveError => Encoding.UTF8.GetString(ResolveErrorBytes);
    public static string ResolveAllErrors => Encoding.UTF8.GetString(ResolveAllErrorsBytes);

    private static ReadOnlySpan<byte> GetStruckTablesBytes =>
    """
        IF OBJECT_ID('[__UninclusedObjects]') IS NOT NULL
            SELECT [Object_Name]
              FROM [__UninclusedObjects]
             WHERE [Struck_Indicator] = 'Y'
               AND OBJECT_ID('[' + [Object_Name] + ']') IS NOT NULL
        """u8;

    private static ReadOnlySpan<byte> InitializeBytes =>
        """
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
        """u8;

    private static ReadOnlySpan<byte> MigrateV2toV3Bytes =>
        """
        INSERT INTO [__InedoDb_DbSchemaChanges]
        (
            [Script_Guid],
            [Script_Name],
            [Executed_Date],
            [Success_Indicator],
            [ErrorResolved_Text],
            [ErrorResolved_Date]
        )
        SELECT [Script_Guid],
               [Script_Name],
               [Executed_Date],
               [Success_Indicator],
               [ErrorResolved_Text] = CASE WHEN [Success_Indicator] = 'N' THEN N'Migrated to dbschema v3.' ELSE NULL END,
               [ErrorResolved_Date] = CASE WHEN [Success_Indicator] = 'N' THEN GETUTCDATE() ELSE NULL END
          FROM [__BuildMaster_DbSchemaChanges2]
        """u8;

    private static ReadOnlySpan<byte> ReadV1ScriptsBytes =>
        """
        SELECT [Script_Id],
               [Numeric_Release_Number],
               [Batch_Name], 
               [Executed_Date] = MIN([Executed_Date]),
               [Success_Indicator] = MIN([Success_Indicator])
          FROM [__BuildMaster_DbSchemaChanges]
         WHERE [Script_Id] > 0
         GROUP BY [Numeric_Release_Number], [Script_Id], [Batch_Name]
        """u8;

    private static ReadOnlySpan<byte> RecordExecutionBytes =>
        """
        INSERT INTO [__InedoDb_DbSchemaChanges] ([Script_Guid], [Script_Name], [Script_Sql], [Executed_Date], [Success_Indicator], [Error_Text])
        VALUES (@Script_Guid, @Script_Name, NULLIF(@Script_Sql, 0x), @Executed_Date, @Success_Indicator, NULLIF(@Error_Text, ''))
        """u8;

    private static ReadOnlySpan<byte> UpdateExecutionBytes =>
        """
        UPDATE [__InedoDb_DbSchemaChanges]
           SET [Script_Name] = @Script_Name,
               [Script_Sql] = @Script_Sql,
               [Executed_Date] = @Executed_Date,
               [Success_Indicator] = @Success_Indicator,
               [Error_Text] = NULLIF(@Error_Text, ''),
               [ErrorResolved_Text] = NULL,
               [ErrorResolved_Date] = NULL
         WHERE [Script_Guid] = @Script_Guid
        """u8;

    private static ReadOnlySpan<byte> ResolveErrorBytes =>
        """
        UPDATE [__InedoDb_DbSchemaChanges]
           SET [ErrorResolved_Text] = NULLIF(@ErrorResolved_Text, N''),
               [ErrorResolved_Date] = GETUTCDATE()
         WHERE [Script_Guid] = @Script_Guid
        """u8;

    private static ReadOnlySpan<byte> ResolveAllErrorsBytes =>
        """
        UPDATE [__InedoDb_DbSchemaChanges]
           SET [ErrorResolved_Text] = NULLIF(@ErrorResolved_Text, N''),
               [ErrorResolved_Date] = GETUTCDATE()
         WHERE [Success_Indicator] = 'N'
           AND [ErrorResolved_Date] IS NULL
        """u8;
}
