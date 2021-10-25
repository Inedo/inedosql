using System;

#if NET452
#else
using Microsoft.Data.SqlClient;
#endif

namespace Inedo.DbUpdater.SqlServer
{
    internal sealed class MessageLoggedEventArgs : EventArgs
    {
        public MessageLoggedEventArgs(string message) => this.Message = message;

        public string Message { get; }
    }
}
