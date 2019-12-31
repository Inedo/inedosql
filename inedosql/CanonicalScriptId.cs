using System;

namespace Inedo.DbUpdater
{
    internal readonly struct CanonicalScriptId
    {
        public CanonicalScriptId(Guid guid, int? legacyId, ExecutionMode mode)
        {
            this.Guid = guid;
            this.LegacyId = legacyId;
            this.Mode = mode;
        }

        public Guid Guid { get; }
        public int? LegacyId { get; }
        public ExecutionMode Mode { get; }
    }
}
