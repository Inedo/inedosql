using System;

namespace Inedo.DbUpdater
{
    public sealed class InedoSqlException : Exception
    {
        public InedoSqlException(int exitCode, string message)
            : base(message)
        {
            this.ExitCode = exitCode;
        }
        public InedoSqlException(string message)
            : base(message)
        {
            this.ExitCode = -1;
        }
        public InedoSqlException(string message, bool writeUsage)
            : base(message)
        {
            this.ExitCode = -1;
            this.WriteUsage = writeUsage;
        }

        public int ExitCode { get; }
        public bool WriteUsage { get; }
    }
}
