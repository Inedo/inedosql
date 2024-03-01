using System;

namespace Inedo.DbUpdater;

/// <summary>
/// Uniquely identifies a change script.
/// </summary>
public readonly struct CanonicalScriptId
{
    internal CanonicalScriptId(Guid guid, int? legacyId, ExecutionMode mode, bool useTransaction)
    {
        this.Guid = guid;
        this.LegacyId = legacyId;
        this.Mode = mode;
        this.UseTransaction = useTransaction;
    }

    /// <summary>
    /// Gets the script guid.
    /// </summary>
    public Guid Guid { get; }
    /// <summary>
    /// Gets the legacy ID if applicable.
    /// </summary>
    public int? LegacyId { get; }
    /// <summary>
    /// Gets the script execution mode.
    /// </summary>
    public ExecutionMode Mode { get; }
    /// <summary>
    /// Gets a value indicating whether to use a transaction.
    /// </summary>
    public bool UseTransaction { get; }
}
