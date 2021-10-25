using System;

namespace Inedo.DbUpdater
{
    internal readonly struct CanonicalScriptId
    {
        public CanonicalScriptId(Guid guid, int? legacyId, ExecutionMode mode, bool useTransaction)
        {
            this.Guid = guid;
            this.LegacyId = legacyId;
            this.Mode = mode;
            this.UseTransaction = useTransaction;
        }

        public Guid Guid { get; }
        public int? LegacyId { get; }
        public ExecutionMode Mode { get; }
        public bool UseTransaction { get; }
    }
}
