using NFMWorldLibrary.Radpack;

namespace NFMWorldLibrary.Util;

/// <summary>
/// <see cref="IModuleSource"/> backed by a <see cref="RadpackLua"/>'s <c>Files</c> dictionary.
/// Keys use '.' as the path separator (e.g. <c>client</c>, <c>pvp.util</c>); entry files are
/// keyed without an extension, but the loader tolerates keys that include one.
///
/// Chunk names are namespaced as <c>@radpack/{SourceId}/{key}</c> so relative requires from
/// radpack modules can be routed back to this source.
/// </summary>
public sealed class RadpackModuleSource : IModuleSource
{
    const string ChunkPrefix = "@radpack/";
    const string AliasMarker = "@/";

    public string SourceId { get; }
    public RadpackLua Radpack { get; }

    public RadpackModuleSource(RadpackLua radpack, string sourceId)
    {
        Radpack = radpack ?? throw new ArgumentNullException(nameof(radpack));
        SourceId = sourceId ?? throw new ArgumentNullException(nameof(sourceId));
    }

    string Prefix => ChunkPrefix + SourceId + "/";

    public bool IsFile(string key) => Radpack.Files.ContainsKey(key);

    public bool IsDirectory(string key)
    {
        var prefix = key + ".";
        foreach (var existingKey in Radpack.Files.Keys)
        {
            if (existingKey.StartsWith(prefix, StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    public bool TryRead(string key, out string source)
    {
        return Radpack.Files.TryGetValue(key, out source!);
    }

    public string ToChunkName(string key) => Prefix + key;

    public bool TryGetKeyFromChunk(string chunkSource, out string key)
    {
        if (chunkSource.StartsWith(Prefix, StringComparison.Ordinal))
        {
            key = chunkSource[Prefix.Length..];
            return true;
        }
        key = null!;
        return false;
    }

    public bool TryResolveAlias(string name, out string key)
    {
        // '@/' resolves to the radpack root: require('@/util') -> key 'util'.
        if (name.StartsWith(AliasMarker, StringComparison.Ordinal))
        {
            var rest = name[AliasMarker.Length..];
            if (rest.Length > 0)
            {
                key = rest;
                return true;
            }
        }
        key = null!;
        return false;
    }

    public bool TryResolveRoot(string name, out string key)
    {
        key = name;
        return true;
    }

    public bool TryResolveRelative(string callerKey, string relative, out string key)
    {
        return PathResolver.CombineRelative(callerKey, relative, '.', out key);
    }

    public string Join(string baseKey, string subKey)
    {
        return baseKey.Length == 0 ? subKey : baseKey + "." + subKey;
    }
}
