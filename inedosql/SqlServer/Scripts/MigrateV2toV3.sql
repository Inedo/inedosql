INSERT INTO [__InedoDb_DbSchemaChanges]
SELECT [Script_Guid],
       [Script_Name],
       [Executed_Date],
       [Success_Indicator]
  FROM [__BuildMaster_DbSchemaChanges2]
