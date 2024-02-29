using System;

namespace Inedo.DbUpdater;

internal readonly struct CanonicalScriptId(Guid guid, int? legacyId, ExecutionMode mode, bool useTransaction)
{
    public Guid Guid { get; } = guid;
    public int? LegacyId { get; } = legacyId;
    public ExecutionMode Mode { get; } = mode;
    public bool UseTransaction { get; } = useTransaction;
}
