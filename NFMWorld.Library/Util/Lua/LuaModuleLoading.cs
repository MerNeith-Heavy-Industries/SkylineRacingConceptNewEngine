using System.Text;
using NFMWorldLibrary.Radpack;
using NuLua;
using NuLua.Luau;

namespace NFMWorldLibrary.Util;

/// <summary>
/// Game-layer Lua module loading: installs a Luau-style <c>require</c> global and provides
/// chunk-named entry-point helpers (VFS-backed <see cref="DoFile"/> and a chunk-named
/// <see cref="DoString"/>) that are prerequisites for relative requires.
///
/// Resolution semantics (Luau require-by-string):
/// <list type="bullet">
/// <item><c>require('./x')</c> / <c>require('../x')</c> — resolved against the directory of the
/// requiring file (its chunk name), with <c>.luau</c>/<c>.lua</c> and <c>init.*</c> fallbacks and
/// ambiguity errors.</item>
/// <item><c>require('@/x')</c> — resolves to <c>./data/x</c> on the VFS source (the data root),
/// and to the radpack root on a <c>RadpackModuleSource</c>.</item>
/// <item>Other names resolve at the caller's source root.</item>
/// </list>
/// Loaded modules are cached per state in the <c>_NFM_MODULES</c> global table.
/// </summary>
public static class LuaModuleLoading
{
    /// <summary>
    /// Installs the game <c>require</c> global on <paramref name="state"/> and registers
    /// <paramref name="defaultSource"/> (or a default VFS source rooted at <c>data</c>).
    /// Called from <see cref="LuaHelpers.OpenState"/>.
    /// </summary>
    public static void Install(LuauState state, IModuleSource? defaultSource = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        defaultSource ??= new VfsModuleSource(TheVFS.VFS, "vfs");
        RequireContext.Get(state).Reset(defaultSource);
        state["require"] = LuaValue.FromFunction(state.CreateFunction(RequireFunc, 0));
    }

    /// <summary>Registers a gamemode radpack so <c>require</c> calls from its scripts resolve within it.</summary>
    public static void RegisterRadpackSource(LuauState state, RadpackLua radpack, string sourceId)
    {
        ArgumentNullException.ThrowIfNull(state);
        RequireContext.Get(state).RegisterSource(new RadpackModuleSource(radpack, sourceId));
    }

    /// <summary>
    /// Loads and runs a script from the virtual file system, using <paramref name="path"/> as the
    /// chunk name so relative <c>require</c> calls resolve against its directory.
    /// </summary>
    public static LuaValue[] DoFile(this LuauState state, string path)
    {
        ArgumentNullException.ThrowIfNull(state);
        var bytes = TheVFS.VFS.ReadAllBytes(path);
        return ExecuteChunk(state, bytes, path);
    }

    /// <summary>
    /// Loads and runs <paramref name="source"/> with an explicit <paramref name="chunkName"/>
    /// (the module's identity for relative <c>require</c> resolution).
    /// </summary>
    public static LuaValue[] DoString(this LuauState state, string source, string chunkName)
    {
        ArgumentNullException.ThrowIfNull(state);
        return ExecuteChunk(state, Encoding.UTF8.GetBytes(source), chunkName);
    }

    static LuaValue[] ExecuteChunk(LuauState state, byte[] bytes, string chunkName)
    {
        var baseTop = state.GetTop();
        state.LoadString(bytes, Encoding.UTF8.GetBytes(chunkName));
        state.Call(0, -1);

        var count = state.GetTop() - baseTop;
        var results = new LuaValue[count];
        for (var i = 0; i < count; i++)
        {
            results[i] = state.ToLuaValue(baseTop + 1 + i);
        }
        state.SetTop(baseTop);
        return results;
    }

    static int RequireFunc(LuauState state, LuaFuncArguments args)
    {
        var name = args[0].Read<string>();
        var context = RequireContext.Get(state);
        var callerSource = GetCallerSource(state);

        var source = context.SelectSource(callerSource) ?? context.DefaultSource;

        // Relative requires need a file-backed caller chunk to resolve against.
        if (RequireResolver.IsRelative(name)
            && (callerSource is null || !source.TryGetKeyFromChunk(callerSource, out _)))
        {
            state.RaiseError($"cannot use relative require '{name}' from a chunk without a file");
            return 1;
        }

        string? key;
        string? error = null;
        try
        {
            key = RequireResolver.Resolve(source, name, callerSource);
        }
        catch (Exception ex)
        {
            // Defer the raise until after the catch: RaiseError (luau_error) longjmps via an
            // SEH exception, which re-enters the catch on .NET 10 inside the
            // [UnmanagedCallersOnly] bridge (dotnet/runtime#123579).
            key = null;
            error = ex.Message;
        }

        if (error is not null)
        {
            state.RaiseError(error);
            return 1;
        }

        if (key is null)
        {
            state.RaiseError($"module '{name}' not found");
            return 1;
        }

        var cacheKey = source.SourceId + ":" + key;

        var cacheValue = state[RequireContext.CacheGlobal];
        LuaTable cacheTable;
        if (cacheValue.IsNil)
        {
            cacheTable = state.CreateTable();
            state[RequireContext.CacheGlobal] = cacheTable;
        }
        else
        {
            cacheTable = cacheValue.Read<LuaTable>();
        }

        try
        {
            var cached = cacheTable[cacheKey];
            if (!cached.IsNil)
            {
                state.Push(cached);
                cached.Dispose();
                return 1;
            }

            if (!LoadModule(state, source, key))
            {
                state.RaiseError($"module '{name}' not found");
                return 1;
            }

            var moduleValue = state.ToLuaValue(-1);
            cacheTable[cacheKey] = moduleValue;
            state.Push(moduleValue);
            // The module is now cached in `cacheTable` and on the stack, so the temporary
            // registry reference taken by ToLuaValue can be released.
            moduleValue.Dispose();
            return 1;
        }
        finally
        {
            // `cacheValue` is a per-call temporary wrapper around the persistent `_NFM_MODULES`
            // global; release its registry reference. No-op when the cache table was freshly created.
            cacheValue.Dispose();
        }
    }

    /// <summary>
    /// The calling Lua function's chunk name (level 0 is this <c>require</c> closure, level 1 is
    /// the function that called it). This is the module identity used for relative resolution.
    /// </summary>
    static string? GetCallerSource(LuauState state)
    {
        if (state.Debug.TryGetStackInfo(1, LuaDebugInfoFields.Source, out var info))
        {
            return info.Source;
        }
        return null;
    }

    /// <summary>
    /// Loads a module directly on <paramref name="state"/> and calls it, leaving its single
    /// return value on the stack. Loading on the current state (rather than a separate coroutine)
    /// keeps error propagation a plain Lua call — a module error surfaces as a normal Lua error
    /// through the nearest <c>pcall</c>, without bouncing across a thread boundary.
    /// </summary>
    static bool LoadModule(LuauState state, IModuleSource source, string key)
    {
        if (!source.TryRead(key, out var sourceText))
        {
            return false;
        }

        state.LoadString(sourceText, source.ToChunkName(key));
        state.Call(0, 1);
        return true;
    }
}
