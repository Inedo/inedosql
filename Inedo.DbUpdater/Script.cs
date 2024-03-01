using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Inedo.DbUpdater;

/// <summary>
/// Represents a SQL script.
/// </summary>
public sealed partial class Script : IComparable<Script>, IEquatable<Script>
{
    private static readonly char[] separators = ['/', '\\'];

    internal Script(string fileName, string scriptText)
    {
        this.FileName = fileName;
        this.ScriptText = scriptText;
        if (ExtractId(scriptText, out var id))
            this.Id = id;
    }

    /// <summary>
    /// Gets the file name.
    /// </summary>
    public string FileName { get; }
    /// <summary>
    /// Gets ths content of the file.
    /// </summary>
    public string ScriptText { get; }
    /// <summary>
    /// Gets the unique script ID.
    /// </summary>
    public CanonicalScriptId? Id { get; }

    /// <inheritdoc/>
    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(this.FileName ?? string.Empty);
    /// <inheritdoc/>
    public override bool Equals(object? obj) => this.Equals(obj as Script);
    /// <inheritdoc/>
    public bool Equals(Script? other)
    {
        if (ReferenceEquals(this, other))
            return true;
        if (other is null)
            return false;

        return string.Equals(this.FileName, other.FileName, StringComparison.OrdinalIgnoreCase);
    }
    /// <inheritdoc/>
    public int CompareTo(Script? other)
    {
        if (ReferenceEquals(this, other))
            return 0;
        if (other is null)
            return -1;

        var myName = this.FileName.Split(separators, StringSplitOptions.RemoveEmptyEntries);
        var otherName = other.FileName.Split(separators, StringSplitOptions.RemoveEmptyEntries);

        int res;
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
    /// <inheritdoc/>
    public override string ToString() => this.FileName;

    /// <summary>
    /// Attempts to extract script ID information.
    /// </summary>
    /// <param name="scriptText">The script to parse.</param>
    /// <param name="id">The resulting script ID.s</param>
    /// <returns>True if ID was found; otherwise false.</returns>
    public static bool ExtractId(string scriptText, out CanonicalScriptId id)
    {
        var match = ScriptIdRegex().Match(scriptText);
        if (match.Success)
        {
            var parts = match.Groups[1].Value
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            Guid? guid = null;
            int? legacyId = null;
            var mode = ExecutionMode.Once;
            bool useTransaction = true;

            foreach (var p in parts)
            {
                if (p.StartsWith("ScriptId=", StringComparison.OrdinalIgnoreCase) && Guid.TryParse(p.AsSpan("ScriptId=".Length), out var g))
                    guid ??= g;
                else if (p.StartsWith("ExecutionMode=", StringComparison.OrdinalIgnoreCase) && Enum.TryParse(p.AsSpan("ExecutionMode=".Length), out ExecutionMode m))
                    mode = m;
                else if (p.StartsWith("UseTransaction=", StringComparison.OrdinalIgnoreCase) && bool.TryParse(p.AsSpan("UseTransaction=".Length), out var t))
                    useTransaction = t;
                else if (int.TryParse(p, out int i))
                    legacyId = i;
            }

            if (guid.HasValue)
            {
                id = new CanonicalScriptId(guid.GetValueOrDefault(), legacyId, mode, useTransaction);
                return true;
            }
        }

        id = default;
        return false;
    }

    /// <summary>
    /// Returns script files in the specified path.
    /// </summary>
    /// <param name="scriptPath">The path to iterate.</param>
    /// <returns>List of script files in the specified path.</returns>
    public static List<Script> GetScriptFiles(string scriptPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(scriptPath);

        return Directory.EnumerateFiles(scriptPath, "*.sql", SearchOption.AllDirectories)
            .Select(s => new Script(s[scriptPath.Length..].TrimStart(separators), File.ReadAllText(s)))
            .ToList();
    }
    /// <summary>
    /// Returns script files in the specified zip.
    /// </summary>
    /// <param name="zip">The zip file to iterate.</param>
    /// <returns>List of script files in the specified zip.</returns>
    public static List<Script> GetScriptZipEntries(ZipArchive zip)
    {
        ArgumentNullException.ThrowIfNull(zip);

        var scripts = new List<Script>(zip.Entries.Count);

        foreach (var entry in zip.Entries.Where(e => e.FullName.EndsWith(".sql", StringComparison.OrdinalIgnoreCase)))
        {
            using var entryReader = new StreamReader(entry.Open(), Encoding.UTF8);
            scripts.Add(new Script(entry.FullName, entryReader.ReadToEnd()));
        }

        return scripts;
    }

    [GeneratedRegex(@"^--\s*AH:(?<1>.+)\n", RegexOptions.ExplicitCapture | RegexOptions.Compiled)]
    private static partial Regex ScriptIdRegex();
}
