using System;

namespace Inedo.DbUpdater.SqlServer;

/// <summary>
/// Contains a logged message.
/// </summary>
public sealed class MessageLoggedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessageLoggedEventArgs"/> class.
    /// </summary>
    /// <param name="message">The message.</param>
    public MessageLoggedEventArgs(string message)
    {
        this.Message = message;
    }

    /// <summary>
    /// Gets the logged message.
    /// </summary>
    public string Message { get; }
}
