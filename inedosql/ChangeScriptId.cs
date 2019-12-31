using System;

namespace Inedo.DbUpdater
{
    /// <summary>
    /// A value which uniquely identifies a change script.
    /// </summary>
    public sealed class ChangeScriptId
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeScriptId"/> class.
        /// </summary>
        /// <param name="scriptId">The somewhat unique identifier.</param>
        /// <param name="guid">The unique identifier.</param>
        public ChangeScriptId(int scriptId, Guid guid)
        {
            this.ScriptId = scriptId;
            this.Guid = guid;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeScriptId"/> class.
        /// </summary>
        /// <param name="scriptId">The somewhat unique identifier.</param>
        /// <param name="legacyReleaseSequence">The legacy release sequence.</param>
        public ChangeScriptId(int scriptId, long legacyReleaseSequence)
        {
            this.ScriptId = scriptId;
            this.LegacyReleaseSequence = legacyReleaseSequence;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeScriptId"/> class.
        /// </summary>
        /// <param name="scriptId">The somewhat unique identifier.</param>
        /// <param name="legacyReleaseSequence">The legacy release sequence.</param>
        /// <param name="guid">The unique identifier.</param>
        public ChangeScriptId(int scriptId, long legacyReleaseSequence, Guid guid)
        {
            this.ScriptId = scriptId;
            this.LegacyReleaseSequence = legacyReleaseSequence;
            this.Guid = guid;
        }

        /// <summary>
        /// Gets the database-unique identifier.
        /// </summary>
        public int ScriptId { get; }
        /// <summary>
        /// Gets the release sequence number if available. This is only used for v1 scripts.
        /// </summary>
        public long? LegacyReleaseSequence { get; }
        /// <summary>
        /// Gets the identifier as a <see cref="Guid"/> if available.
        /// </summary>
        public Guid? Guid { get; }

        /// <summary>
        /// Returns a string representation of the ID.
        /// </summary>
        /// <returns>String representation of the ID.</returns>
        public override string ToString() => this.Guid?.ToString() ?? this.ScriptId.ToString();
    }
}
