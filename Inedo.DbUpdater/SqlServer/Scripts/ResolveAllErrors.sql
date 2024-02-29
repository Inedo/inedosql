UPDATE [__InedoDb_DbSchemaChanges]
   SET [ErrorResolved_Text] = NULLIF(@ErrorResolved_Text, N''),
       [ErrorResolved_Date] = GETUTCDATE()
 WHERE [Success_Indicator] = 'N'
   AND [ErrorResolved_Date] IS NULL
