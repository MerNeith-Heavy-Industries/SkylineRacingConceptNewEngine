using NFMWorld.PhysicsFS;

namespace NFMWorldLibrary.Util;

/// <summary>
/// <see cref="IModuleSource"/> backed by the virtual file system. Keys are slash-separated
/// VFS paths (e.g. <c>data/gamemodes/pvp/client.luau</c>).
///
/// Alias rules: <c>require('@/foo')</c> resolves to <c>{root}/foo</c> (default root
/// <c>data</c>, i.e. <c>./data/foo</c>). Non-relative names resolve at the root too.
/// </summary>
public sealed class VfsModuleSource : IModuleSource
{
    const string AliasMarker = "@/";

    public string SourceId { get; }
    public VirtualFS Vfs { get; }
    public string Root { get; }

    public VfsModuleSource(VirtualFS vfs, string sourceId, string root = "data")
    {
        Vfs = vfs ?? throw new ArgumentNullException(nameof(vfs));
        SourceId = sourceId ?? throw new ArgumentNullException(nameof(sourceId));
        Root = root ?? string.Empty;
    }

    public bool IsFile(string key) => Vfs.FileExists(key);

    public bool IsDirectory(string key) => Vfs.DirectoryExists(key);

    public bool TryRead(string key, out string source)
    {
        if (!Vfs.FileExists(key))
        {
            source = null!;
            return false;
        }
        source = Vfs.ReadAllText(key);
        return true;
    }

    public string ToChunkName(string key) => key;

    public bool TryGetKeyFromChunk(string chunkSource, out string key)
    {
        // Radpack chunks are namespaced with '@'; VFS paths are plain.
        if (chunkSource.Length > 0 && chunkSource[0] == '@')
        {
            key = null!;
            return false;
        }
        key = chunkSource;
        return true;
    }

    public bool TryResolveAlias(string name, out string key)
    {
        if (name.StartsWith(AliasMarker, StringComparison.Ordinal))
        {
            var rest = name[AliasMarker.Length..];
            if (rest.Length > 0)
            {
                key = Root.Length == 0 ? rest : Root + "/" + rest;
                return true;
            }
        }
        key = null!;
        return false;
    }

    public bool TryResolveRoot(string name, out string key)
    {
        key = Root.Length == 0 ? name : Root + "/" + name;
        return true;
    }

    public bool TryResolveRelative(string callerKey, string relative, out string key)
    {
        return PathResolver.CombineRelative(callerKey, relative, '/', out key);
    }

    public string Join(string baseKey, string subKey)
    {
        return baseKey.Length == 0 ? subKey : baseKey + "/" + subKey;
    }
}
