namespace NFMWorldLibrary.Util;

/// <summary>
/// A source of Lua modules, used to resolve <c>require</c> names (Luau require-by-string
/// semantics) and to read module source text. Implementations wrap a backing store:
/// the virtual file system (<see cref="VfsModuleSource"/>) or a Lua radpack
/// (<see cref="RadpackModuleSource"/>).
///
/// Keys are source-relative identifiers. For VFS these are slash-separated paths
/// (e.g. <c>data/gamemodes/pvp/client.luau</c>); for radpacks these are dotted keys
/// (e.g. <c>client</c>, <c>pvp.util</c>).
/// </summary>
public interface IModuleSource
{
    /// <summary>A stable identifier used to namespace cache keys (e.g. <c>"vfs"</c>, <c>"pvp"</c>).</summary>
    string SourceId { get; }

    /// <summary>Whether <paramref name="key"/> names an existing file/module in this source.</summary>
    bool IsFile(string key);

    /// <summary>
    /// Whether <paramref name="key"/> names a directory in this source (used for
    /// <c>init.luau</c>/<c>init.lua</c> resolution).
    /// </summary>
    bool IsDirectory(string key);

    /// <summary>Reads the UTF-8 source text for the module at <paramref name="key"/>.</summary>
    bool TryRead(string key, out string source);

    /// <summary>Builds the Lua chunk name (the "source" reported by <c>debug</c>) for a module key.</summary>
    string ToChunkName(string key);

    /// <summary>
    /// Given a chunk's source (its chunk name), returns the source-relative key if this
    /// source owns that chunk. Used to route a <c>require</c> call to the source that
    /// contains the requiring file.
    /// </summary>
    bool TryGetKeyFromChunk(string chunkSource, out string key);

    /// <summary>
    /// Expands an explicit alias name (e.g. <c>@/foo</c>) to a source-relative key
    /// (<c>data/foo</c> for VFS, <c>foo</c> for radpack). Returns false if the name is
    /// not an alias this source understands.
    /// </summary>
    bool TryResolveAlias(string name, out string key);

    /// <summary>Resolves a non-relative name (e.g. <c>require('pvp.util')</c>) at this source's root.</summary>
    bool TryResolveRoot(string name, out string key);

    /// <summary>
    /// Resolves a relative name (<c>./</c> or <c>../</c>) against the directory of
    /// <paramref name="callerKey"/> using this source's key separators.
    /// </summary>
    bool TryResolveRelative(string callerKey, string relative, out string key);

    /// <summary>Joins a directory key with a child name (e.g. <c>init.luau</c>) using this source's separator.</summary>
    string Join(string baseKey, string subKey);
}
