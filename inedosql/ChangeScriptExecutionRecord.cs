using System;
using System.Text;

namespace Inedo.DbUpdater
{
    /// <summary>
    /// Contains information about a previously executed database change script.
    /// </summary>
    public sealed class ChangeScriptExecutionRecord
    {
        private readonly Lazy<string> scriptText;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeScriptExecutionRecord"/> class.
        /// </summary>
        /// <param name="id">The script ID.</param>
        /// <param name="name">The script name.</param>
        /// <param name="executionDate">The script execution date.</param>
        /// <param name="successfullyExecuted">Value indicating whether the script was successful.</param>
        public ChangeScriptExecutionRecord(ChangeScriptId id, string name, DateTime executionDate, bool successfullyExecuted, string errorText = null, string errorResolvedText = null, DateTime? errorResolvedDate = null, byte[] scriptBytes = null)
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
        public string ErrorText { get; }
        public string ErrorResolvedText { get; }
        public DateTime? ErrorResolvedDate { get; }
        public string ScriptText => this.scriptText?.Value;
    }
}
