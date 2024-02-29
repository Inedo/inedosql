using System;
using System.Text;

namespace Inedo.DbUpdater;

/// <summary>
/// Contains information about a previously executed database change script.
/// </summary>
public sealed class ChangeScriptExecutionRecord
{
    private readonly Lazy<string> scriptText;

    internal ChangeScriptExecutionRecord(ChangeScriptId id, string name, DateTime executionDate, bool successfullyExecuted, string errorText = null, string errorResolvedText = null, DateTime? errorResolvedDate = null, byte[] scriptBytes = null)
    {
        this.Id = id;
        this.Name = name;
        this.ExecutionDate = executionDate;
        this.SuccessfullyExecuted = successfullyExecuted;
        this.ErrorText = errorText;
        this.ErrorResolvedText = errorResolvedText;
        this.ErrorResolvedDate = errorResolvedDate;
        if (scriptBytes != null)
            this.scriptText = new Lazy<string>(() => Encoding.UTF8.GetString(scriptBytes));
    }

    /// <summary>
    /// Gets the unique script ID.
    /// </summary>
    public ChangeScriptId Id { get; }
    /// <summary>
    /// Gets the script name.
    /// </summary>
    public string Name { get; }
    /// <summary>
    /// Gets the script execution date.
    /// </summary>
    public DateTime ExecutionDate { get; }
    /// <summary>
    /// Gets a value indicating whether the script was successful.
    /// </summary>
    public bool SuccessfullyExecuted { get; }
    /// <summary>
    /// Gets the error message.
    /// </summary>
    public string ErrorText { get; }
    /// <summary>
    /// Gets notes about how the error was resolved.
    /// </summary>
    public string ErrorResolvedText { get; }
    /// <summary>
    /// Gets the date the error was resolved, or null if it has not been resolved.
    /// </summary>
    public DateTime? ErrorResolvedDate { get; }
    /// <summary>
    /// Gets the script contents.
    /// </summary>
    public string ScriptText => this.scriptText?.Value;
}
