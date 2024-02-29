using System;
using System.Collections.Generic;

namespace Inedo.DbUpdater;

internal static class EmbeddedScripts
{
    private static readonly Lazy<IReadOnlyCollection<Script>> scriptsIfAvailable = new(ReadScripts);

    public static IReadOnlyCollection<Script> All => scriptsIfAvailable.Value;
    public static bool Available => scriptsIfAvailable.Value != null;

    private static IReadOnlyCollection<Script> ReadScripts()
    {
        return null;
    }
}
