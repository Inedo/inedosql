using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

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

                var scripts = new List<Script>(zip.Entries.Count);

                foreach (var entry in zip.Entries.Where(e => e.FullName.EndsWith(".sql", StringComparison.OrdinalIgnoreCase)))
                {
                    using var entryReader = new StreamReader(entry.Open(), Encoding.UTF8);
                    scripts.Add(new Script(entry.FullName, entryReader.ReadToEnd()));
                }

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
