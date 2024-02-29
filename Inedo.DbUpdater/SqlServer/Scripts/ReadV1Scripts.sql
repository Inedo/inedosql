SELECT [Script_Id],
       [Numeric_Release_Number],
       [Batch_Name], 
       [Executed_Date] = MIN([Executed_Date]),
       [Success_Indicator] = MIN([Success_Indicator])
  FROM [__BuildMaster_DbSchemaChanges]
 WHERE [Script_Id] > 0
 GROUP BY [Numeric_Release_Number], [Script_Id], [Batch_Name]
