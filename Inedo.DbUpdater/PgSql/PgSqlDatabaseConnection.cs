using System.Text;
using Npgsql;

namespace Inedo.DbUpdater.PgSql;

/// <summary>
/// Represents a connection to a PostgreSQL database.
/// </summary>
/// <param name="connectionString">Connection string.</param>
public sealed class PgSqlDatabaseConnection(string connectionString) : DatabaseConnection, IDatabaseConnection<PgSqlDatabaseConnection>
{
    private NpgsqlConnection? connection;
    private NpgsqlCommand? command;
    private bool disposed;

    /// <summary>
    /// Gets the connection string.
    /// </summary>
    public string ConnectionString { get; } = connectionString;

    /// <inheritdoc/>
    public override bool ExecuteScripts(IEnumerable<Script> scripts, ChangeScriptState state)
    {
        var lookup = state.Scripts.Where(s => s.Id?.Guid.HasValue == true).ToDictionary(s => s.Id.Guid.GetValueOrDefault());

        foreach (var script in scripts)
        {
            if (script.Id.HasValue)
            {
                if (!this.ExecuteTrackedScript(script, lookup))
                    return false;
            }
            else
            {
                this.LogInformation($"Executing untracked script {script.FileName}...");
                this.ExecuteNonQuery(script.ScriptText, null);
            }
        }

        return true;
    }

    /// <inheritdoc/>
    public override ChangeScriptState GetState()
    {
        int version = this.GetChangeScriptVersion();

        if (version == 3)
        {
            return new ChangeScriptState(
                3,
                this.ExecuteTable(
                    "SELECT * FROM \"__InedoDb_DbSchemaChanges\"",
                    r =>
                    {
                        return new ChangeScriptExecutionRecord(
                            new ChangeScriptId(r.GetInt32(0), r.GetGuid(1)),
                            r.GetString(2),
                            r.GetDateTime(4),
                            r.GetBoolean(5),
                            r.IsDBNull(6) ? null : r.GetString(6),
                            r.IsDBNull(7) ? null : r.GetString(7),
                            r.IsDBNull(8) ? null : r.GetDateTime(8),
                            r.IsDBNull(3) ? null : Encoding.UTF8.GetBytes(r.GetString(3))
                        );
                    }
                )
            );
        }

        return new ChangeScriptState(false);
    }

    /// <inheritdoc/>
    public override void InitializeDatabase()
    {
        using var transaction = this.GetConnection().BeginTransaction();

        int version = this.GetChangeScriptVersion(transaction);
        if (version > 0)
            return;

        this.ExecuteNonQuery(Scripts.Initialize, transaction);

        transaction.Commit();
    }

    /// <inheritdoc/>
    public override void ResolveAllErrors(string comment)
    {
        this.ExecuteNonQuery(
            Scripts.ResolveAllErrors,
            null,
            new NpgsqlParameter<string> { TypedValue = comment }
        );
    }

    /// <inheritdoc/>
    public override void ResolveError(Guid scriptId, string comment)
    {
        this.ExecuteNonQuery(
            Scripts.ResolveError,
            null,
            new NpgsqlParameter<Guid> { TypedValue = scriptId },
            new NpgsqlParameter<string> { TypedValue = comment }
        );
    }

    /// <inheritdoc/>
    public override void StrikeStruckTables()
    {
        var struckTables = this.ExecuteTable(Scripts.GetStruckTables, r => r.GetString(0));
        if (struckTables.Count > 0)
        {
            foreach (var t in struckTables)
            {
                Console.WriteLine($"Dropping {t}...");
                this.ExecuteNonQuery($"DROP TABLE \"{t}\"", null);
            }
        }
    }

    /// <inheritdoc/>
    public override void UpgradeSchema(IReadOnlyDictionary<int, Guid> canoncialGuids) => throw new InvalidOperationException("The database has already been upgraded.");

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing && !this.disposed)
        {
            this.command?.Dispose();
            this.connection?.Dispose();
            this.disposed = true;
        }
    }

    private bool ExecuteTrackedScript(Script script, Dictionary<Guid, ChangeScriptExecutionRecord> currentState)
    {
        var scriptId = script.Id!.Value;
        currentState.TryGetValue(scriptId.Guid, out var previousExecution);

        if (scriptId.Mode == ExecutionMode.Once && previousExecution != null)
        {
            this.LogInformation($"Tracked script {script.FileName} has already been executed; skipping...");
            return true;
        }
        else if (scriptId.Mode == ExecutionMode.OnChange)
        {
            if (previousExecution != null)
            {
                if (previousExecution.ScriptText == script.ScriptText)
                {
                    this.LogInformation($"Tracked script {script.FileName} has already been executed and it has not changed; skipping...");
                    return true;
                }
                else
                {
                    this.LogInformation($"Tracked script {script.FileName} has changed; executing...");
                }
            }
            else
            {
                this.LogInformation($"Executing tracked script {script.FileName}...");
            }
        }
        else
        {
            this.LogInformation($"Executing tracked script {script.FileName}...");
        }

        var errors = new List<string>();

        bool success;
        NpgsqlTransaction? transaction = null;
        try
        {
            if (scriptId.UseTransaction)
                transaction = this.GetConnection().BeginTransaction();

            this.ExecuteNonQuery(script.ScriptText, transaction);
            success = true;
            this.LogInformation(script.FileName + " executed successfully.");
            transaction?.Commit();
        }
        catch (NpgsqlException ex)
        {
            this.LogError(ex.Message);
            errors.Add(ex.Message);
            success = false;
            transaction?.Rollback();
        }

        var command = this.GetCommand(previousExecution == null ? Scripts.RecordExecution : Scripts.UpdateExecution);
        var scriptIdParam = new NpgsqlParameter<Guid> { NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Uuid };
        var fileNameParam = new NpgsqlParameter<string> { NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Varchar };
        var scriptTextParam = new NpgsqlParameter<string> { NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Text };
        var timestampParam = new NpgsqlParameter<DateTime> { NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.TimestampTz };
        var successParam = new NpgsqlParameter<bool> { NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Boolean };
        var errorsParam = new NpgsqlParameter<string> { NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Text };
        command.Parameters.Add(scriptIdParam);
        command.Parameters.Add(fileNameParam);
        command.Parameters.Add(scriptTextParam);
        command.Parameters.Add(timestampParam);
        command.Parameters.Add(successParam);
        command.Parameters.Add(errorsParam);

        command.Prepare();

        scriptIdParam.TypedValue = scriptId.Guid;
        fileNameParam.TypedValue = script.FileName;
        scriptTextParam.TypedValue = script.ScriptText;
        timestampParam.TypedValue = DateTime.UtcNow;
        successParam.TypedValue = success;
        errorsParam.TypedValue = string.Join(Environment.NewLine, errors);

        command.ExecuteNonQuery();

        return success;
    }

    private List<TResult> ExecuteTable<TResult>(string query, Func<NpgsqlDataReader, TResult> adapter, params NpgsqlParameter[] args) => this.ExecuteTable(query, adapter, transaction: null, args);
    private List<TResult> ExecuteTable<TResult>(string query, Func<NpgsqlDataReader, TResult> adapter, NpgsqlTransaction? transaction, params NpgsqlParameter[] args)
    {
        using var command = new NpgsqlCommand(query, this.GetConnection(), transaction) { CommandTimeout = 0 };
        command.Parameters.AddRange(args);

        using var reader = command.ExecuteReader();
        var table = new List<TResult>();

        while (reader.Read())
        {
            table.Add(adapter(reader));
        }

        return table;
    }
    private NpgsqlConnection GetConnection()
    {
        if (this.connection == null)
        {
            var conn = new NpgsqlConnection(this.ConnectionString);
            conn.Open();
            conn.Notice += this.Connection_Notice;
            this.connection = conn;
        }

        return this.connection;
    }

    private int GetChangeScriptVersion(NpgsqlTransaction? transaction = null)
    {
        var table = this.ExecuteTable(
            "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '__InedoDb_DbSchemaChanges'",
            t => t.GetString(0),
            transaction
        );

        if (table.Contains("__InedoDb_DbSchemaChanges", StringComparer.OrdinalIgnoreCase))
            return 3;

        return 0;
    }
    private void ExecuteNonQuery(string query, NpgsqlTransaction? transaction, params NpgsqlParameter[] args)
    {
        foreach (var part in NaiveSqlSplitter.SplitSqlScript(query))
        {
            var command = this.GetCommand(part.Text, transaction, args);
            if (part.Atomic)
                command.Parameters.Add(new() { Value = 0 });
            command.ExecuteNonQuery();
        }
    }
    private NpgsqlCommand GetCommand(string query, NpgsqlTransaction? transaction = null, NpgsqlParameter[]? args = null)
    {
        if (this.command == null)
        {
            this.command = new NpgsqlCommand(query, this.GetConnection(), transaction)
            {
                CommandTimeout = 0
            };
        }
        else
        {
            this.command.Parameters.Clear();
            this.command.CommandText = query;
            this.command.Transaction = transaction;
        }

        if (args?.Length > 0)
            this.command.Parameters.AddRange(args);

        return this.command;
    }

    private void Connection_Notice(object sender, NpgsqlNoticeEventArgs e)
    {
        if (e.Notice.InvariantSeverity == "EXCEPTION")
            this.LogError(e.Notice.MessageText);
        else
            this.LogInformation(e.Notice.MessageText);
    }

    static PgSqlDatabaseConnection IDatabaseConnection<PgSqlDatabaseConnection>.Create(string connectionString) => new(connectionString);
}
