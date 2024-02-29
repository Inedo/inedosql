using System;
using System.IO;
using System.Text;

namespace Inedo.DbUpdater.SqlServer;

internal static class Scripts
{
    private static readonly Lazy<string> initialize = new(() => Read("Initialize.sql"));
    private static readonly Lazy<string> recordExecution = new(() => Read("RecordExecution.sql"));
    private static readonly Lazy<string> updateExecution = new(() => Read("UpdateExecution.sql"));
    private static readonly Lazy<string> readV1Scripts = new(() => Read("ReadV1Scripts.sql"));
    private static readonly Lazy<string> migrateV2toV3 = new(() => Read("MigrateV2toV3.sql"));
    private static readonly Lazy<string> resolveError = new(() => Read("ResolveError.sql"));
    private static readonly Lazy<string> resolveAllErrors = new(() => Read("ResolveAllErrors.sql"));

    public static string Initialize => initialize.Value;
    public static string RecordExecution => recordExecution.Value;
    public static string UpdateExecution => updateExecution.Value;
    public static string ReadV1Scripts => readV1Scripts.Value;
    public static string MigrateV2toV3 => migrateV2toV3.Value;
    public static string ResolveError => resolveError.Value;
    public static string ResolveAllErrors => resolveAllErrors.Value;

    private static string Read(string name)
    {
        using var reader = new StreamReader(typeof(Scripts).Assembly.GetManifestResourceStream(typeof(Scripts).FullName + "." + name), Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
