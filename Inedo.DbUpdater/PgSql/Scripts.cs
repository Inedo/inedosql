using System.Text;

namespace Inedo.DbUpdater.PgSql;

internal static class Scripts
{
    public static string Initialize => Encoding.UTF8.GetString(InitializeBytes);
    public static string RecordExecution { get; } = Encoding.UTF8.GetString(RecordExecutionBytes);
    public static string UpdateExecution { get; } = Encoding.UTF8.GetString(UpdateExecutionBytes);
    public static string ResolveError => Encoding.UTF8.GetString(ResolveErrorBytes);
    public static string ResolveAllErrors => Encoding.UTF8.GetString(ResolveAllErrorsBytes);
    public static string GetStruckTables => Encoding.UTF8.GetString(GetStruckTablesBytes);

    private static ReadOnlySpan<byte> GetStruckTablesBytes =>
        """
        DO $$
        BEGIN
           	IF (SELECT to_regclass('"__UninclusedObjects"')) IS NOT NULL THEN
                    SELECT "Object_Name"
                      FROM "__UninclusedObjects"
                     WHERE "Struck_Indicator" = TRUE
                       AND to_regclass('"' || "Object_Name" || '"') IS NOT NULL
            END IF;
        END $$
        """u8;

    public static ReadOnlySpan<byte> InitializeBytes =>
        """
        CREATE TABLE "__InedoDb_DbSchemaChanges"
        (
            "Script_Id" INT GENERATED ALWAYS AS IDENTITY,
            "Script_Guid" UUID NOT NULL,
            "Script_Name" VARCHAR(200) NOT NULL,
            "Script_Sql" TEXT NULL,
            "Executed_Date" TIMESTAMP WITH TIME ZONE NOT NULL,
            "Success_Indicator" BOOLEAN NOT NULL,
            "Error_Text" TEXT NULL,
            "ErrorResolved_Text" TEXT NULL,
            "ErrorResolved_Date" TIMESTAMP WITH TIME ZONE NULL,

            CONSTRAINT "PK__InedoDb_DbSchemaChanges"
                PRIMARY KEY ("Script_Id"),

            CONSTRAINT "UQ__InedoDb_DbSchemaChanges"
                UNIQUE ("Script_Guid")
        )
        """u8;

    private static ReadOnlySpan<byte> RecordExecutionBytes =>
        """
        INSERT INTO "__InedoDb_DbSchemaChanges" ("Script_Guid", "Script_Name", "Script_Sql", "Executed_Date", "Success_Indicator", "Error_Text")
        VALUES ($1, $2, NULLIF($3, ''), $4, $5, NULLIF($6, ''))
        """u8;

    private static ReadOnlySpan<byte> UpdateExecutionBytes =>
        """
        UPDATE "__InedoDb_DbSchemaChanges"
           SET "Script_Name" = $2,
               "Script_Sql" = $3,
               "Executed_Date" = $4,
               "Success_Indicator" = $5,
               "Error_Text" = NULLIF($6, ''),
               "ErrorResolved_Text" = NULL,
               "ErrorResolved_Date" = NULL
         WHERE "Script_Guid" = $1
        """u8;

    private static ReadOnlySpan<byte> ResolveErrorBytes =>
        """
        UPDATE "__InedoDb_DbSchemaChanges"
           SET "ErrorResolved_Text" = NULLIF($2, ''),
               "ErrorResolved_Date" = CURRENT_TIMESTAMP
         WHERE "Script_Guid" = $1
        """u8;

    private static ReadOnlySpan<byte> ResolveAllErrorsBytes =>
        """
        UPDATE "__InedoDb_DbSchemaChanges"
           SET "ErrorResolved_Text" = NULLIF($1, ''),
               "ErrorResolved_Date" = CURRENT_TIMESTAMP
         WHERE "Success_Indicator" = 'N'
           AND "ErrorResolved_Date" IS NULL
        """u8;
}
