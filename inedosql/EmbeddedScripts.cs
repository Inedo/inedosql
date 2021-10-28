using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace Inedo.DbUpdater
{
    internal static class EmbeddedScripts
    {
        private static readonly Lazy<IReadOnlyCollection<Script>> scriptsIfAvailable = new(ReadScripts);

        public static IReadOnlyCollection<Script> All => scriptsIfAvailable.Value;
        public static bool Available => scriptsIfAvailable.Value != null;

        private static IReadOnlyCollection<Script> ReadScripts()
        {
#if NETFRAMEWORK
            var path = typeof(EmbeddedScripts).Assembly.Location;
            if (!string.IsNullOrEmpty(path))
            {
                using var thisStream = File.OpenRead(path);
                try
                {
                    using var zip = new ZipArchive(thisStream, ZipArchiveMode.Read);
                    var scripts = Script.GetScriptZipEntries(zip);
                    scripts.Sort();
                    return scripts.AsReadOnly();
                }
                catch
                {
                }
            }
#endif

            return null;
        }
    }
}
