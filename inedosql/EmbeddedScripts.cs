using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace Inedo.DbUpdater
{
    internal static class EmbeddedScripts
    {
        private static readonly Lazy<IReadOnlyCollection<Script>> scriptsIfAvailable = new Lazy<IReadOnlyCollection<Script>>(ReadScripts);

        public static IReadOnlyCollection<Script> All => scriptsIfAvailable.Value;
        public static bool Available => scriptsIfAvailable.Value != null;

        private static IReadOnlyCollection<Script> ReadScripts()
        {
            using var thisStream = File.OpenRead(typeof(EmbeddedScripts).Assembly.Location);
            try
            {
                using var zip = new ZipArchive(thisStream, ZipArchiveMode.Read);
                var scripts = Script.GetScriptZipEntries(zip);
                scripts.Sort();
                return scripts.AsReadOnly();
            }
            catch
            {
                return null;
            }
        }
    }
}
