using System;

namespace Inedo.DbUpdater;

/// <summary>
/// Represents an error that occurred in inedosql.
/// </summary>
public sealed class InedoSqlException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InedoSqlException"/> class.
    /// </summary>
    /// <param name="exitCode">The process exit code.</param>
    /// <param name="message">The error message.</param>
    public InedoSqlException(int exitCode, string message)
        : base(message)
    {
        this.ExitCode = exitCode;
    }
    /// <summary>
    /// Initializes a new instance of the <see cref="InedoSqlException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public InedoSqlException(string message)
        : base(message)
    {
        this.ExitCode = -1;
    }
    /// <summary>
    /// Initializes a new instance of the <see cref="InedoSqlException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="writeUsage">Value indicating whether usage instructions should be displayed.</param>
    public InedoSqlException(string message, bool writeUsage)
        : base(message)
    {
        this.ExitCode = -1;
        this.WriteUsage = writeUsage;
    }

    /// <summary>
    /// Gets the process exit code.
    /// </summary>
    public int ExitCode { get; }
    /// <summary>
    /// Gets a value indicating whether usage instructions should be displayed.
    /// </summary>
    public bool WriteUsage { get; }
}
