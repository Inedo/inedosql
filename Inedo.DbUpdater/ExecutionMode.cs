namespace Inedo.DbUpdater;

/// <summary>
/// Specifies SQL script execution mode.
/// </summary>
public enum ExecutionMode
{
    /// <summary>
    /// Script should run once and only once.
    /// </summary>
    Once,
    /// <summary>
    /// Script should run only when it has changed.
    /// </summary>
    OnChange,
    /// <summary>
    /// Script should always run.
    /// </summary>
    Always
}
