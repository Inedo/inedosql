using System;
using System.Data.Common;
using System.Text;
using System.Text.RegularExpressions;

#nullable enable

namespace Inedo.DbUpdater;

/// <summary>
/// Contains utility methods.
/// </summary>
public static partial class InedoSqlUtil
{
    /// <summary>
    /// Ensures that the default value of require encryption for a connection string
    /// does not flip to true unexpectedly. See Remarks.
    /// </summary>
    /// <param name="connectionString">Connection string to check.</param>
    /// <returns>Updated connection string with require encryption disabled if necessary.</returns>
    /// <remarks>
    /// <para>
    /// Microsoft.Data.SqlClient 4.0.0 and later introduced a breaking change by changing the default of the
    /// <c>Encrypt=...</c> connection string file from <c>false</c> to <c>true</c>. In practice, this will make
    /// connection strings that used to work suddenly fail unless the SQL Server instance actually has SSL configured.
    /// </para>
    /// <para>
    /// This method is used to restore the default to the original behavior. If <c>Encrypt=</c> is already specified in
    /// the connection string, it will not be changed, but if it is not specified, <c>Encrypt=false</c> will be added
    /// if using Microsoft.Data.SqlClient 4.0.0 or greater.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="connectionString"/> is null or empty.</exception>
    public static string EnsureRequireEncryptionDefaultsToFalse(string connectionString)
    {
        ArgumentException.ThrowIfNullOrEmpty(connectionString);

        if (ConnStringRegex().IsMatch(connectionString))
            return connectionString;

        var sb = new StringBuilder(connectionString);
        DbConnectionStringBuilder.AppendKeyValuePair(sb, "Encrypt", "false");
        return sb.ToString();
    }

    [GeneratedRegex(@"(^|;)\s*Encrypt\s*=", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture)]
    private static partial Regex ConnStringRegex();
}
