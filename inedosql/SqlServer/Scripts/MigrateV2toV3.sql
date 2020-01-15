INSERT INTO [__InedoDb_DbSchemaChanges]
(
    [Script_Guid],
    [Script_Name],
    [Executed_Date],
    [Success_Indicator]
)
SELECT [Script_Guid],
       [Script_Name],
       [Executed_Date],
       [Success_Indicator]
  FROM [__BuildMaster_DbSchemaChanges2]
