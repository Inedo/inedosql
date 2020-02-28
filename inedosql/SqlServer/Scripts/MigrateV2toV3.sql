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
