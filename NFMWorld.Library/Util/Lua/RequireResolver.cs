namespace NFMWorldLibrary.Util;

/// <summary>
/// Resolves Lua <c>require</c> names to module keys using Luau require-by-string semantics:
/// <list type="bullet">
/// <item><c>./</c>/<c>../</c> names resolve against the directory of the requiring file;</item>
/// <item><c>@/</c> aliases expand via <see cref="IModuleSource.TryResolveAlias"/>;</item>
/// <item>other names resolve at the source root.</item>
/// </list>
/// Candidate resolution tries the exact key, then <c>.luau</c>, then <c>.lua</c>, then
/// <c>init.luau</c>/<c>init.lua</c> for directories — and if more than one candidate matches,
/// the require is ambiguous (an error), matching modern Luau.
/// </summary>
public static class RequireResolver
{
    static readonly string[] Extensions = [".luau", ".lua"];
    static readonly string[] InitFiles = ["init.luau", "init.lua"];

    public static bool IsRelative(string name)
    {
        return name.StartsWith("./", StringComparison.Ordinal)
            || name.StartsWith("../", StringComparison.Ordinal);
    }

    /// <summary>
    /// Resolves <paramref name="name"/> to a source-relative module key, or null if no module
    /// matches. Throws <see cref="InvalidOperationException"/> for ambiguous requires.
    /// </summary>
    public static string? Resolve(IModuleSource source, string name, string? callerSource)
    {
        // Explicit alias: require('@/foo') -> source root (data for VFS, radpack root for radpack).
        if (name.Length > 1 && name[0] == '@')
        {
            return source.TryResolveAlias(name, out var aliasKey)
                ? ResolveCandidates(source, aliasKey)
                : null;
        }

        // Luau require-by-string: './'/'../' resolve against the requiring file.
        if (IsRelative(name))
        {
            if (callerSource is null || !source.TryGetKeyFromChunk(callerSource, out var callerKey))
            {
                return null;
            }
            return source.TryResolveRelative(callerKey, name, out var combined)
                ? ResolveCandidates(source, combined)
                : null;
        }

        // Non-relative: resolve within the source root (dotted key for radpack, data/ for VFS).
        return source.TryResolveRoot(name, out var rootKey)
            ? ResolveCandidates(source, rootKey)
            : null;
    }

    static string? ResolveCandidates(IModuleSource source, string key)
    {
        var candidates = new List<string>(5);

        if (source.IsFile(key))
        {
            candidates.Add(key);
        }

        foreach (var extension in Extensions)
        {
            var candidate = key + extension;
            if (source.IsFile(candidate))
            {
                candidates.Add(candidate);
            }
        }

        if (source.IsDirectory(key))
        {
            foreach (var initFile in InitFiles)
            {
                var candidate = source.Join(key, initFile);
                if (source.IsFile(candidate))
                {
                    candidates.Add(candidate);
                }
            }
        }

        return candidates.Count switch
        {
            0 => null,
            > 1 => throw new InvalidOperationException(
                $"ambiguous require: multiple files match ({string.Join(", ", candidates)})"),
            _ => candidates[0],
        };
    }
}
