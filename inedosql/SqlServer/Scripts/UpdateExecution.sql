UPDATE [__InedoDb_DbSchemaChanges]
   SET [Script_Name] = @Script_Name,
       [Script_Sql] = @Script_Sql,
       [Executed_Date] = @Executed_Date,
       [Success_Indicator] = @Success_Indicator,
       [Error_Text] = NULLIF(@Error_Text, ''),
       [ErrorResolved_Text] = NULL,
       [ErrorResolved_Date] = NULL
 WHERE [Script_Guid] = @Script_Guid
