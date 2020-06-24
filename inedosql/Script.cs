using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Inedo.DbUpdater
{
    internal sealed class Script : IComparable<Script>, IEquatable<Script>
    {
        private static readonly Regex ScriptIdRegex = new Regex(@"^--\s*AH:(?<1>.+)\n", RegexOptions.Compiled | RegexOptions.ExplicitCapture);

        public Script(string fileName, string scriptText)
        {
            this.FileName = fileName;
            this.ScriptText = scriptText;
            if (ExtractId(scriptText, out var id))
                this.Id = id;
        }

        public string FileName { get; }
        public string ScriptText { get; }
        public CanonicalScriptId? Id { get; }

        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(this.FileName ?? string.Empty);
        public override bool Equals(object obj) => this.Equals(obj as Script);
        public bool Equals(Script other)
        {
            if (ReferenceEquals(this, other))
                return true;
            if (other is null)
                return false;

            return string.Equals(this.FileName, other.FileName, StringComparison.OrdinalIgnoreCase);
        }
        public int CompareTo(Script other)
        {
            int res;

            var myName = this.FileName.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            var otherName = other.FileName.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < Math.Min(myName.Length, otherName.Length); i++)
            {
                res = string.Compare(myName[i], otherName[i], StringComparison.OrdinalIgnoreCase);
                if (res != 0)
                    return res;
            }

            if (myName.Length < otherName.Length)
                res = -1;
            else if (myName.Length > otherName.Length)
                res = 1;
            else
                res = 0;

            return res;
        }
        public override string ToString() => this.FileName;

        public static bool ExtractId(string scriptText, out CanonicalScriptId id)
        {
            var match = ScriptIdRegex.Match(scriptText);
            if (match.Success)
            {
                var parts = match.Groups[1].Value
                    .Split(';')
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.Trim())
                    .ToList();

                Guid? guid = null;
                int? legacyId = null;
                var mode = ExecutionMode.Once;

                foreach (var p in parts)
                {
                    if (p.StartsWith("ScriptId=", StringComparison.OrdinalIgnoreCase) && Guid.TryParse(p.Substring("ScriptId=".Length), out var g))
                        guid ??= g;
                    else if (p.StartsWith("ExecutionMode=", StringComparison.OrdinalIgnoreCase) && Enum.TryParse(p.Substring("ExecutionMode=".Length), out ExecutionMode m))
                        mode = m;
                    else if (int.TryParse(p, out int i))
                        legacyId = i;
                }

                if (guid.HasValue)
                {
                    id = new CanonicalScriptId(guid.GetValueOrDefault(), legacyId, mode);
                    return true;
                }
            }

            id = default;
            return false;
        }

        public static List<Script> GetScriptFiles(string scriptPath)
        {
            return Directory.EnumerateFiles(scriptPath, "*.sql", SearchOption.AllDirectories)
                .Select(s => new Script(s.Substring(scriptPath.Length).TrimStart('/', '\\'), File.ReadAllText(s)))
                .ToList();
        }
        public static List<Script> GetScriptZipEntries(ZipArchive zip)
        {
            var scripts = new List<Script>(zip.Entries.Count);

            foreach (var entry in zip.Entries.Where(e => e.FullName.EndsWith(".sql", StringComparison.OrdinalIgnoreCase)))
            {
                using var entryReader = new StreamReader(entry.Open(), Encoding.UTF8);
                scripts.Add(new Script(entry.FullName, entryReader.ReadToEnd()));
            }

            return scripts;
        }
    }
}
