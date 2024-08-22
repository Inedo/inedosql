namespace Inedo.DbUpdater;

/// <summary>
/// Abstract factory for creating a <see cref="DatabaseConnection"/>.
/// </summary>
/// <typeparam name="TSelf">Type of the database connection.</typeparam>
public interface IDatabaseConnection<TSelf> where TSelf : DatabaseConnection, IDatabaseConnection<TSelf>
{
    /// <summary>
    /// Creates a new database connection instance.
    /// </summary>
    /// <param name="connectionString">Connection string.</param>
    /// <returns>Database connection instance.</returns>
    static abstract TSelf Create(string connectionString);
}
