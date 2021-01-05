using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

#if NET452
using System.Data.SqlClient;
#else
using Microsoft.Data.SqlClient;
#endif

namespace Inedo.DbUpdater.SqlServer
{
    internal sealed class SqlServerDatabaseConnection : IDisposable
    {
        private SqlConnection connection;
        private SqlCommand command;
        private bool disposed;

        public SqlServerDatabaseConnection(string connectionString) => this.ConnectionString = connectionString;

        public string ConnectionString { get; }
        public bool ErrorLogged { get; private set; }

        public void InitializeDatabase()
        {
            using var transaction = this.GetConnection().BeginTransaction();
            
            int version = this.GetChangeScriptVersion(transaction);
            if (version > 0)
                return;

            this.ExecuteNonQuery(Scripts.Initialize, transaction);

            transaction.Commit();
        }
        public void UpgradeSchema(IDictionary<int, Guid> canoncialGuids)
        {
            var state = this.GetState();
            if (state.ChangeScripterVersion == 0)
                throw new InvalidOperationException("The database has not been initialized.");
            if (state.ChangeScripterVersion >= 3)
                throw new InvalidOperationException("The database has already been upgraded.");

            using var transaction = this.GetConnection().BeginTransaction();
            this.ExecuteNonQuery(Scripts.Initialize, transaction);

            if (state.ChangeScripterVersion == 1)
            {
                if (canoncialGuids == null)
                    throw new ArgumentNullException(nameof(canoncialGuids));

                foreach (var script in state.Scripts)
                {
                    if (canoncialGuids.TryGetValue(script.Id.ScriptId, out var guid))
                    {
                        this.ExecuteNonQuery(
                            Scripts.RecordExecution,
                            transaction,
                            new SqlParameter("Script_Guid", guid),
                            new SqlParameter("Script_Name", SqlDbType.NVarChar, 200) { Value = script.Name },
                            new SqlParameter("Script_Sql", SqlDbType.VarBinary, -1) { Value = null },
                            new SqlParameter("Executed_Date", script.ExecutionDate),
                            new SqlParameter("Success_Indicator", SqlDbType.Char, 1) { Value = script.SuccessfullyExecuted ? "Y" : "N" },
                            new SqlParameter("Error_Text", SqlDbType.NVarChar, -1) { Value = null }
                        );

                        if (!script.SuccessfullyExecuted)
                        {
                            this.ExecuteNonQuery(
                                Scripts.ResolveError,
                                transaction,
                                new SqlParameter("Script_Guid", guid),
                                new SqlParameter("ErrorResolved_Text", SqlDbType.NVarChar, -1) { Value = "Migrated to dbschema v3." }
                            );
                        }
                    }
                }
            }
            else if (state.ChangeScripterVersion == 2)
            {
                this.ExecuteNonQuery(Scripts.MigrateV2toV3, transaction);
            }

            transaction.Commit();
        }

        public ChangeScriptState GetState()
        {
            int version = this.GetChangeScriptVersion();

            if (version == 3)
            {
                return new ChangeScriptState(
                    3,
                    this.ExecuteTable(
                        "SELECT * FROM [__InedoDb_DbSchemaChanges]",
                        r =>
                        {
                            return new ChangeScriptExecutionRecord(
                                new ChangeScriptId((int)r["Script_Id"], (Guid)r["Script_Guid"]),
                                r["Script_Name"]?.ToString(),
                                readUtcDateTime(r["Executed_Date"]).Value,
                                r["Success_Indicator"]?.ToString() == "Y",
                                r["Error_Text"]?.ToString(),
                                r["ErrorResolved_Text"]?.ToString(),
                                readUtcDateTime(r["ErrorResolved_Date"]),
                                r["Script_Sql"] as byte[]
                            );
                        }
                    )
                );
            }

            if (version == 2)
            {
                return new ChangeScriptState(
                    2,
                    this.ExecuteTable(
                        "SELECT * FROM [__BuildMaster_DbSchemaChanges2]",
                        r =>
                        {
                            return new ChangeScriptExecutionRecord(
                                new ChangeScriptId((int)r["Script_Id"], (Guid)r["Script_Guid"]),
                                r["Script_Name"]?.ToString(),
                                readUtcDateTime(r["Executed_Date"]).Value,
                                r["Success_Indicator"]?.ToString() == "Y"
                            );
                        }
                    )
                );
            }

            if (version == 1)
            {
                return new ChangeScriptState(
                    1,
                    this.ExecuteTable(
                        Scripts.ReadV1Scripts,
                        r =>
                        {
                            return new ChangeScriptExecutionRecord(
                                new ChangeScriptId((int)r["Script_Id"], (long)r["Numeric_Release_Number"]),
                                r["Batch_Name"]?.ToString(),
                                readLocalDateTime(r["Executed_Date"]).Value,
                                r["Success_Indicator"]?.ToString() == "Y"
                            );
                        }
                    )
                );
            }

            return new ChangeScriptState(false);

            static DateTime? readUtcDateTime(object o) => o is DateTime d ? (DateTime?)new DateTime(d.Ticks, DateTimeKind.Utc) : null;

            static DateTime? readLocalDateTime(object o) => o is DateTime d ? (DateTime?)new DateTime(d.Ticks, DateTimeKind.Local).ToUniversalTime() : null;
        }

        public void ResolveError(Guid scriptId, string comment)
        {
            this.ExecuteNonQuery(
                Scripts.ResolveError,
                null,
                new SqlParameter("Script_Guid", scriptId),
                new SqlParameter("ErrorResolved_Text", SqlDbType.NVarChar, -1) { Value = (object)comment ?? DBNull.Value }
            );
        }
        public void ResolveAllErrors(string comment)
        {
            this.ExecuteNonQuery(
                Scripts.ResolveAllErrors,
                null,
                new SqlParameter("ErrorResolved_Text", SqlDbType.NVarChar, -1) { Value = (object)comment ?? DBNull.Value }
            );
        }

        public bool ExecuteScripts(IEnumerable<Script> scripts, ChangeScriptState state)
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
                    this.ExecuteQueryWithSplitter(script.ScriptText);
                }
            }

            return true;
        }

        public void Dispose() => this.Dispose(true);

        private void Dispose(bool disposing)
        {
            if (disposing && !this.disposed)
            {
                this.command?.Dispose();
                this.connection?.Dispose();
                this.disposed = true;
            }
        }

        private void LogInformation(string s) => Console.WriteLine(s);
        private void LogError(string s)
        {
            this.ErrorLogged = true;
            Console.Error.WriteLine(s);
        }

        private bool ExecuteTrackedScript(Script script, IReadOnlyDictionary<Guid, ChangeScriptExecutionRecord> currentState)
        {
            var scriptId = script.Id.Value;
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
            SqlTransaction transaction = null;
            try
            {
                if (scriptId.UseTransaction)
                    transaction = this.GetConnection().BeginTransaction();

                this.ExecuteQueryWithSplitter(script.ScriptText, transaction);
                success = true;
                this.LogInformation(script.FileName + " executed successfully.");
                transaction?.Commit();
            }
            catch (SqlException ex)
            {
                foreach (SqlError error in ex.Errors)
                {
                    errors.Add(error.Message);
                
                    if (error.Class > 10)
                        this.LogError(error.Message);
                    else
                        this.LogInformation(error.Message);
                    if (error.Number == 226)
                        this.LogInformation("This error may be resolved by adding \"UseTransaction=False\" to the script's AH: header statement.");
                }   

                success = false;
                transaction?.Rollback();
            }

            this.ExecuteNonQuery(
                previousExecution == null ? Scripts.RecordExecution : Scripts.UpdateExecution,
                null,
                new SqlParameter("Script_Guid", scriptId.Guid),
                new SqlParameter("Script_Name", SqlDbType.NVarChar, 200) { Value = script.FileName },
                new SqlParameter("Script_Sql", SqlDbType.VarBinary, -1) { Value = Encoding.UTF8.GetBytes(script.ScriptText) },
                new SqlParameter("Executed_Date", DateTime.UtcNow),
                new SqlParameter("Success_Indicator", SqlDbType.Char, 1) { Value = success ? "Y" : "N" },
                new SqlParameter("Error_Text", SqlDbType.NVarChar, -1) { Value = string.Join(Environment.NewLine, errors) }
            );

            return success;
        }
        
        private void ExecuteNonQuery(string query, SqlTransaction transaction, params SqlParameter[] args)
        {
            var command = this.GetCommand(query, transaction, args);
            command.ExecuteNonQuery();
        }
        private void ExecuteQueryWithSplitter(string query, SqlTransaction transaction = null)
        {
            foreach (var sql in SqlSplitter.SplitSqlScript(query))
            {
                if (!string.IsNullOrWhiteSpace(sql))
                {
                    var command = this.GetCommand(sql, transaction);
                    command.ExecuteNonQuery();
                }
            }
        }
        private List<TResult> ExecuteTable<TResult>(string query, Func<SqlDataReader, TResult> adapter, params SqlParameter[] args) => this.ExecuteTable(query, adapter, transaction: null, args);
        private List<TResult> ExecuteTable<TResult>(string query, Func<SqlDataReader, TResult> adapter, SqlTransaction transaction, params SqlParameter[] args)
        {
            using var command = new SqlCommand(query, this.GetConnection(), transaction) { CommandTimeout = 0 };
            command.Parameters.AddRange(args);

            using var reader = command.ExecuteReader();
            var table = new List<TResult>();

            while (reader.Read())
            {
                table.Add(adapter(reader));
            }

            return table;
        }
        private SqlConnection GetConnection()
        {
            if (this.connection == null)
            {
                var conn = new SqlConnection(this.ConnectionString);
                conn.Open();
                conn.InfoMessage += this.Connection_InfoMessage;
                this.connection = conn;
            }

            return this.connection;
        }
        private SqlCommand GetCommand(string query, SqlTransaction transaction = null, SqlParameter[] args = null)
        {
            if (this.command == null)
            {
                this.command = new SqlCommand(query, this.GetConnection(), transaction)
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

        private void Connection_InfoMessage(object sender, SqlInfoMessageEventArgs e)
        {
            foreach (SqlError error in e.Errors)
            {
                if (error.Class > 10)
                    this.LogError(error.Message);
                else
                    this.LogInformation(error.Message);
            }
        }
        private int GetChangeScriptVersion(SqlTransaction transaction = null)
        {
            var table = this.ExecuteTable(
                "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME IN ('__BuildMaster_DbSchemaChanges', '__BuildMaster_DbSchemaChanges2', '__InedoDb_DbSchemaChanges')",
                t => t["TABLE_NAME"]?.ToString(),
                transaction
            );

            if (table.Contains("__InedoDb_DbSchemaChanges", StringComparer.OrdinalIgnoreCase))
                return 3;
            if (table.Contains("__BuildMaster_DbSchemaChanges2", StringComparer.OrdinalIgnoreCase))
                return 2;
            if (table.Contains("__BuildMaster_DbSchemaChanges", StringComparer.OrdinalIgnoreCase))
                return 1;

            return 0;
        }
    }
}
