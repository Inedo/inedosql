using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Inedo.DbUpdater
{
    /// <summary>
    /// Represents the current state of a database that supports change scripts.
    /// </summary>
    public sealed class ChangeScriptState
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeScriptState"/> class.
        /// </summary>
        /// <param name="initialized">Value indicating whether the database has been intialized.</param>
        public ChangeScriptState(bool initialized)
            : this(initialized, 0, null)
        {
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeScriptState"/> class.
        /// </summary>
        /// <param name="initialized">Value indicating whether the database has been intialized.</param>
        /// <param name="version">The version of the change scripts table.</param>
        public ChangeScriptState(bool initialized, int version)
            : this(initialized, version, null)
        {
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeScriptState"/> class.
        /// </summary>
        /// <param name="scripts">The scripts which have been executed.</param>
        /// <param name="version">The version of the change scripts table.</param>
        public ChangeScriptState(int version, IEnumerable<ChangeScriptExecutionRecord> scripts)
            : this(true, version, scripts)
        {
        }
        private ChangeScriptState(bool initialized, int version, IEnumerable<ChangeScriptExecutionRecord> scripts)
        {
            this.IsInitialized = initialized;
            this.ChangeScripterVersion = version;
            this.Scripts = (scripts ?? []).ToList().AsReadOnly();
        }

        /// <summary>
        /// Gets a value indicating whether the database has been initialized.
        /// </summary>
        public bool IsInitialized { get; }
        /// <summary>
        /// Gets the version number of the change script schema table.
        /// </summary>
        public int ChangeScripterVersion { get; }
        /// <summary>
        /// Gets the scripts which have been executed.
        /// </summary>
        public ReadOnlyCollection<ChangeScriptExecutionRecord> Scripts { get; }

        /// <summary>
        /// Returns the execution record for the script with the specified name or ID.
        /// </summary>
        /// <param name="nameOrId">The name or ID of the script.</param>
        /// <returns>Execution record for the specified script.</returns>
        public ChangeScriptExecutionRecord GetExecutionRecord(string nameOrId)
        {
            if (Guid.TryParse(nameOrId, out var g))
                return this.Scripts.FirstOrDefault(s => s.Id.Guid == g);
            else
                return this.Scripts.FirstOrDefault(s => string.Equals(s.Name, nameOrId, StringComparison.OrdinalIgnoreCase));
        }
    }
}
