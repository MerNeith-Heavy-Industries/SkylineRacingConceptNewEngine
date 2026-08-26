using System.Runtime.CompilerServices;
using NuLua;
using NuLua.Luau;

namespace NFMWorldLibrary.Util;

/// <summary>
/// Per-state registry of module sources for the game's Lua <c>require</c>. Attached to a
/// <see cref="LuauState"/> via a <see cref="ConditionalWeakTable{TKey,TValue}"/> so no NuLua
/// types need to change. Holds no reference to the state, so it cannot keep a state alive.
/// </summary>
public sealed class RequireContext
{
    /// <summary>Name of the per-state Lua table that caches loaded modules by resolved key.</summary>
    public const string CacheGlobal = "_NFM_MODULES";

    static readonly ConditionalWeakTable<LuauState, RequireContext> _contexts = new();

    readonly List<IModuleSource> _sources = [];
    IModuleSource _defaultSource;

    RequireContext()
    {
        _defaultSource = new VfsModuleSource(TheVFS.VFS, "vfs");
        _sources.Add(_defaultSource);
    }

    public static RequireContext Get(LuauState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        // Threads are distinct LuauState instances sharing the main state's globals and registry.
        // Route the lookup to the main state so registered module sources are found regardless of
        // which thread a require runs on.
        var root = state;
        while (((ILuaState)root).From is { } parent)
        {
            root = (LuauState)parent;
        }

        return _contexts.GetValue(root, static _ => new RequireContext());
    }

    public IModuleSource DefaultSource => _defaultSource;

    /// <summary>Replaces all registered sources with a single default (used by <c>Install</c>).</summary>
    public void Reset(IModuleSource defaultSource)
    {
        _sources.Clear();
        _defaultSource = defaultSource;
        _sources.Add(defaultSource);
    }

    /// <summary>Registers an additional source (e.g. a gamemode's radpack), replacing any with the same id.</summary>
    public void RegisterSource(IModuleSource source)
    {
        _sources.RemoveAll(s => s.SourceId == source.SourceId);
        _sources.Add(source);
    }

    /// <summary>
    /// Returns the source that owns <paramref name="callerSource"/> (its chunk name), or null if
    /// no registered source claims it. Radpack chunks are namespaced <c>@radpack/{{id}}/...</c>;
    /// everything else belongs to the VFS source.
    /// </summary>
    public IModuleSource? SelectSource(string? callerSource)
    {
        if (callerSource is not null)
        {
            foreach (var source in _sources)
            {
                if (source.TryGetKeyFromChunk(callerSource, out _))
                {
                    return source;
                }
            }
        }
        return null;
    }
}
