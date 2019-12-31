INSERT INTO [__InedoDb_DbSchemaChanges] ([Script_Guid], [Script_Name], [Script_Sql], [Executed_Date], [Success_Indicator], [Error_Text])
VALUES (@Script_Guid, @Script_Name, NULLIF(@Script_Sql, 0x), @Executed_Date, @Success_Indicator, NULLIF(@Error_Text, ''))
