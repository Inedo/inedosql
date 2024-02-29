using System;

namespace Inedo.DbUpdater.SqlServer;

internal sealed class MessageLoggedEventArgs(string message) : EventArgs
{
    public string Message { get; } = message;
}
