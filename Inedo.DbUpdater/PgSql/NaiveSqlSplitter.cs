namespace Inedo.DbUpdater.PgSql;

/// <summary>
/// This is needed to work around a bug in Npgsql.
/// </summary>
/// <remarks>
/// If this ever gets fixed, we can remove this: https://github.com/npgsql/npgsql/issues/4445
/// </remarks>
internal static partial class NaiveSqlSplitter
{
    public static IEnumerable<ScriptPart> SplitSqlScript(string script)
    {
        if (script.AsSpan().Trim() is "" or ";")
            return [];

        int atomicIndex = script.IndexOf("BEGIN ATOMIC", StringComparison.OrdinalIgnoreCase);
        if (atomicIndex >= 0)
        {
            int beginRoutineIndex = findMax(script, atomicIndex, ["CREATE OR REPLACE PROCEDURE", "CREATE OR REPLACE FUNCTION", "CREATE PROCEDURE", "CREATE FUNCTION"]);
            if (beginRoutineIndex >= 0)
            {
                int endRoutineIndex = script.IndexOf("\nEND", atomicIndex, StringComparison.OrdinalIgnoreCase);
                if (endRoutineIndex >= 0)
                {
                    return [
                        .. SplitSqlScript(script[0..beginRoutineIndex]),
                        new ScriptPart(script[beginRoutineIndex..(endRoutineIndex + 4)], true),
                        .. SplitSqlScript(script[(endRoutineIndex + 4)..])
                    ];
                }
            }
        }

        return [new ScriptPart(script, false)];

        static int findMax(string script, int startIndex, ReadOnlySpan<string> values)
        {
            int index = -1;

            foreach (var v in values)
            {
                int i = script.LastIndexOf(v, startIndex, StringComparison.OrdinalIgnoreCase);
                if (i > index)
                    index = i;
            }

            return index;
        }
    }
}
