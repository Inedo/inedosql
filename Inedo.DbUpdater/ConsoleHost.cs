using System.IO.Compression;
using Inedo.DbUpdater.PgSql;
using Inedo.DbUpdater.SqlServer;

namespace Inedo.DbUpdater;

/// <summary>
/// Contains the standard inedosql console implementation.
/// </summary>
public static class ConsoleHost
{
    /// <summary>
    /// Runs inedosql as a console application.
    /// </summary>
    /// <param name="args">Arguments supplied to inedosql.</param>
    /// <returns>Process exit code.</returns>
    public static Task<int> RunAsync(IEnumerable<string> args)
    {
        try
        {
            return Task.FromResult(Run(new ArgList(args)));
        }
        catch (InedoSqlException ex)
        {
            Console.Error.WriteLine(ex.Message);
            if (ex.WriteUsage)
                Usage();

            return Task.FromResult(ex.ExitCode);
        }
    }

    private static int Run(ArgList args)
    {
        if (args.Command == null)
        {
            Usage();
            return 1;
        }

        using var db = CreateConnection(args);

        return args.Command switch
        {
            "update" => Update(args.TryGetPositional(0)!, db, args.Named.ContainsKey("force"), args.Named.GetValueOrDefault("ignore")?.Split(';', StringSplitOptions.RemoveEmptyEntries)),
            "errors" => ListErrors(args.Named.ContainsKey("all"), db),
            "error" => ShowErrorDetails(args.TryGetPositional(0)!, db, false),
            "script" => ShowErrorDetails(args.TryGetPositional(0)!, db, true),
            "resolve-error" => resolveErrors(),
            "strike-struck" => StrikeStruckTables(db),
            _ => throw new InedoSqlException("Invalid command: " + args.Command, true)
        };

        // This is a little more complicated than the other commands, so handle abstraction here
        int resolveErrors()
        {
            if (args.Named.ContainsKey("all") && args.Positional.Count > 0)
                throw new InedoSqlException("--all cannot be specified with a script GUID.");

            args.Named.TryGetValue("comment", out var comment);

            if (args.Named.ContainsKey("all"))
                return ResolveAllErrors(db, comment);
            else
                return ResolveError(args.Positional[0], db, comment);
        }
    }

    private static int Update(string scriptPath, DatabaseConnection db, bool force, string[]? ignoreScripts)
    {
        ArgumentException.ThrowIfNullOrEmpty(scriptPath);

        var uninclusedScripts = (ignoreScripts ?? []).Select(s => s.Replace('\\', '/')).ToHashSet(StringComparer.OrdinalIgnoreCase);

        List<Script> sqlScripts;

        if (scriptPath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) && File.Exists(scriptPath))
        {
            using var zip = new ZipArchive(File.OpenRead(scriptPath), ZipArchiveMode.Read);
            var zipScripts = Script.GetScriptZipEntries(zip);
            zipScripts.Sort();
            sqlScripts = zipScripts;
        }
        else if (Directory.Exists(scriptPath))
        {
            var fileScripts = Script.GetScriptFiles(scriptPath);
            fileScripts.Sort();
            sqlScripts = fileScripts;
        }
        else
        {
            Console.Error.WriteLine($"Script path \"{scriptPath}\" not found.");
            return -1;
        }

        ChangeScriptState state;
        try
        {
            state = db.GetState();
            if (!state.IsInitialized)
            {
                Console.WriteLine("Database is not initialized; initializing...");
                db.InitializeDatabase();
                Console.WriteLine("Database initialized.");

                state = db.GetState(); // refresh state
            }
            else if (state.ChangeScripterVersion == 1)
            {
                Console.WriteLine("Database uses old change script schema; upgrading...");

                var lookup = new SortedList<int, Guid>(sqlScripts.Count);
                foreach (var script in sqlScripts)
                {
                    if (script.Id.HasValue)
                    {
                        var id = script.Id.GetValueOrDefault();
                        if (id.LegacyId == null)
                        {
                            Console.WriteLine($"Script {script.FileName} is missing a legacy ID.");
                            continue;
                        }

                        lookup[id.LegacyId.Value] = id.Guid;
                    }
                }

                db.UpgradeSchema(lookup);
                Console.WriteLine("Schema upgraded.");

                state = db.GetState(); // refresh state
            }
            else if (state.ChangeScripterVersion == 2)
            {
                Console.WriteLine("Database uses v2 change script schema; upgrading...");

                db.UpgradeSchema(null);
                state = db.GetState(); // refresh state
            }
        }
        catch (NotSupportedException ex)
        {
            throw new InedoSqlException(ex.Message);
        }

        if (!force && state.Scripts.Any(s => !s.SuccessfullyExecuted && !s.ErrorResolvedDate.HasValue))
        {
            Console.Error.WriteLine("Scripts not executed; at least one script has unresolved errors.");
            Console.Error.WriteLine("Use the \"errors\" command to view unresolved errors, or use the --force argument to run anyway.");
            return -1;
        }

        if (uninclusedScripts.Count > 0)
            sqlScripts.RemoveAll(s => uninclusedScripts.Contains(s.FileName.Replace('\\', '/')));

        if (!db.ExecuteScripts(sqlScripts, state))
            return -1;

        if (db.ErrorLogged)
            return -1;

        return 0;
    }
    private static int ListErrors(bool all, DatabaseConnection db)
    {
        var state = db.GetState();
        if (!state.IsInitialized)
            throw new InedoSqlException("Database has not been initialized.");

        var errors = state.Scripts.Where(s => !s.SuccessfullyExecuted);
        if (!all)
            errors = errors.Where(s => !s.ErrorResolvedDate.HasValue);

        foreach (var script in errors.OrderByDescending(e => e.Id.ScriptId))
            Console.WriteLine($"{script.Id} {script.Name}");

        return 0;
    }
    private static int ShowErrorDetails(string nameOrId, DatabaseConnection db, bool writeScript)
    {
        var state = db.GetState();
        if (!state.IsInitialized)
            throw new InedoSqlException("Database has not been initialized.");

        var script = state.GetExecutionRecord(nameOrId)
            ?? throw new InedoSqlException($"Tracked script {nameOrId} has not been executed against this database.");

        Console.WriteLine($"GUID: {script.Id}");
        Console.WriteLine($"Name: {script.Name}");
        Console.WriteLine($"Executed: {script.ExecutionDate.ToLocalTime()}");
        Console.WriteLine($"Resolved: {script.ErrorResolvedDate?.ToLocalTime().ToString() ?? "-"}");
        Console.WriteLine();
        Console.WriteLine("Resolution:");
        Console.WriteLine(script.ErrorResolvedText ?? "-");
        Console.WriteLine();
        Console.WriteLine("Error:");
        Console.WriteLine(script.ErrorText ?? "-");
        Console.WriteLine();

        if (writeScript)
        {
            Console.WriteLine("Script:");
            Console.WriteLine(script.ScriptText ?? "-");
            Console.WriteLine();
        }

        return 0;
    }
    private static int ResolveError(string scriptId, DatabaseConnection db, string? comment)
    {
        if (!Guid.TryParse(scriptId, out var guid))
            throw new InedoSqlException($"Invalid script GUID: {scriptId}");

        var state = db.GetState();
        if (!state.IsInitialized)
            throw new InedoSqlException("Database has not been initialized.");

        var script = state.Scripts.FirstOrDefault(s => s.Id.Guid == guid)
            ?? throw new InedoSqlException($"Tracked script {scriptId} has not been executed against this database.");

        if (script.SuccessfullyExecuted)
        {
            Console.WriteLine($"[warn] Tracked script {scriptId} executed with no errors; nothing to resolve.");
            return 0;
        }

        db.ResolveError(guid, comment);
        return 0;
    }
    private static int ResolveAllErrors(DatabaseConnection db, string? comment)
    {
        var state = db.GetState();
        if (!state.IsInitialized)
            throw new InedoSqlException("Database has not been initialized.");

        db.ResolveAllErrors(comment);
        return 0;
    }
    private static int StrikeStruckTables(DatabaseConnection db)
    {
        var state = db.GetState();
        if (!state.IsInitialized)
            throw new InedoSqlException("Database has not been initialized.");

        db.StrikeStruckTables();
        return 0;
    }

    private static void Usage()
    {
        Console.WriteLine();
        Console.WriteLine("Usage: inedosql <command> [arguments...]");
        Console.WriteLine("Commands:");
        Console.WriteLine(" update <script-path> [--connection-string=<connection-string>] [--force]");
        Console.WriteLine(" errors [--connection-string=<connection-string>] [--all]");
        Console.WriteLine(" error <script-guid-or-name> [--connection-string=<connection-string>]");
        Console.WriteLine(" script <script-guid-or-name> [--connection-string=<connection-string>]");
        Console.WriteLine(" resolve-error [<script-guid>] [--connection-string=<connection-string>] [--all] [--comment=<comment>]");
    }

    private static string GetConnectionString(ArgList args)
    {
        if (args.Named.TryGetValue("connection-string", out var cs) && !string.IsNullOrWhiteSpace(cs))
            return cs;

        return Environment.GetEnvironmentVariable("inedosql_cs")
            ?? throw new InedoSqlException("Connection string is required. Argument --connection-string=<value> is missing and inedosql_cs environment variable is not set.");
    }

    private static DatabaseConnection CreateConnection(ArgList args)
    {
        var connectionString = GetConnectionString(args);

        return args.Named.GetValueOrDefault("platform") switch
        {
            "pgsql" => CreateConnection<PgSqlDatabaseConnection>(connectionString),
            "mssql" or null => CreateConnection<SqlServerDatabaseConnection>(connectionString),
            _ => throw new InedoSqlException("Invalid database platform: must be pgsql or mssql")
        };
    }
    private static TConnection CreateConnection<TConnection>(string connectionString) where TConnection : DatabaseConnection, IDatabaseConnection<TConnection>
    {
        var connection = TConnection.Create(connectionString);
        connection.LogInformationMessage += (s, e) => Console.WriteLine(e.Message);
        connection.LogErrorMessage += (s, e) => Console.Error.WriteLine(e.Message);
        return connection;
    }
}
