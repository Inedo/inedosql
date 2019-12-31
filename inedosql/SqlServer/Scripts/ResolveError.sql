UPDATE [__InedoDb_DbSchemaChanges]
   SET [ErrorResolved_Text] = NULLIF(@ErrorResolved_Text, N''),
       [ErrorResolved_Date] = GETUTCDATE()
 WHERE [Script_Guid] = @Script_Guid
