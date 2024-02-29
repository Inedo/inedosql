using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Inedo.DbUpdater;

internal sealed partial class ArgList
{
    public ArgList(IEnumerable<string> args)
    {
        var unnamed = args.Where(a => !a.StartsWith('-')).ToList();
        this.Command = unnamed.FirstOrDefault()?.ToLowerInvariant();
        this.Positional = unnamed.Skip(1).ToList().AsReadOnly();

        var namedArgs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var arg in args.Where(a => a.StartsWith('-')))
        {
            var match = OptionRegex().Match(arg);
            if (!match.Success)
                throw new InedoSqlException("Invalid argument: " + arg);

            var name = match.Groups[1].Value;
            if (namedArgs.ContainsKey(name))
                throw new InedoSqlException($"Argument --{name} is specified more than once.");

            namedArgs.Add(name, match.Groups[2].Value ?? string.Empty);
        }

        this.Named = namedArgs;
    }

    public string Command { get; }
    public IReadOnlyList<string> Positional { get; }
    public IReadOnlyDictionary<string, string> Named { get; }

    public string TryGetPositional(int index) => index >= 0 && index < this.Positional.Count ? this.Positional[index] : null;

    [GeneratedRegex(@"^--?(?<1>[a-zA-Z0-9]+[a-zA-Z0-9\-]*)(=(?<2>.*))?$", RegexOptions.ExplicitCapture)]
    private static partial Regex OptionRegex();
}
