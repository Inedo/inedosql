namespace Inedo.DbUpdater;

/// <summary>
/// Represents a connection to a database.
/// </summary>
public abstract class DatabaseConnection : IDisposable
{
    private protected DatabaseConnection()
    {
    }

    /// <summary>
    /// Raised when an informational message is logged.
    /// </summary>
    public event EventHandler<MessageLoggedEventArgs>? LogInformationMessage;
    /// <summary>
    /// Raised when an error message is logged.
    /// </summary>
    public event EventHandler<MessageLoggedEventArgs>? LogErrorMessage;

    /// <summary>
    /// Gets a value indicating whether an error has been logged.
    /// </summary>
    public bool ErrorLogged { get; private set; }

    /// <summary>
    /// Writes metadata tables to the database if necessary.
    /// </summary>
    public abstract void InitializeDatabase();
    /// <summary>
    /// Upgrades a changescript schema.
    /// </summary>
    /// <param name="canoncialGuids">Legacy ID mapping table.</param>
    /// <exception cref="InvalidOperationException">Database has not been initialized or has already been upgraded.</exception>
    public abstract void UpgradeSchema(IReadOnlyDictionary<int, Guid>? canoncialGuids);
    /// <summary>
    /// Returns the current state of change scripts in the database.
    /// </summary>
    /// <returns>Current state of change scripts in the database.</returns>
    public abstract ChangeScriptState GetState();
    /// <summary>
    /// Marks an error as resolved.
    /// </summary>
    /// <param name="scriptId">Unique ID of the script.</param>
    /// <param name="comment">Resolution comment.</param>
    public abstract void ResolveError(Guid scriptId, string? comment);
    /// <summary>
    /// Marks all errors as resolved.
    /// </summary>
    /// <param name="comment">Resolution comment.</param>
    public abstract void ResolveAllErrors(string? comment);
    /// <summary>
    /// Drops all tables that have been marked as struck.
    /// </summary>
    public abstract void StrikeStruckTables();
    /// <summary>
    /// Executes change scripts.
    /// </summary>
    /// <param name="scripts">Scripts to execute.</param>
    /// <param name="state">Current state.</param>
    /// <returns>True if all scripts were successful; otherwise false.</returns>
    public abstract bool ExecuteScripts(IEnumerable<Script> scripts, ChangeScriptState state);

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        this.Dispose(true);
    }

    /// <summary>
    /// Release resources used by the connection.
    /// </summary>
    /// <param name="disposing">Value indicating whether to release managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
    }

    /// <summary>
    /// Raises the <see cref="LogInformationMessage"/> event.
    /// </summary>
    /// <param name="s">Log message.</param>
    protected void LogInformation(string s) => this.LogInformationMessage?.Invoke(this, new MessageLoggedEventArgs(s));
    /// <summary>
    /// Raises the <see cref="LogErrorMessage"/> event and sets the <see cref="ErrorLogged"/> property.
    /// </summary>
    /// <param name="s">Log message.</param>
    protected void LogError(string s)
    {
        this.ErrorLogged = true;
        this.LogErrorMessage?.Invoke(this, new MessageLoggedEventArgs(s));
    }
}
