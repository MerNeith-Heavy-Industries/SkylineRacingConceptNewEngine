using System.Diagnostics;
using NFMWorld.DriverInterface.DriverInterface;
using NFMWorld.Reactor;
using NFMWorldLibrary.Util;
using NuLua;
using NuLua.Luau;

namespace LuauBenchmark;

/// <summary>
/// The single VM-specific entry point for the benchmark — NuLua (real Luau) edition.
///
/// Mirrors the real game on this branch (nfm-world/UI/UiRenderer.cs + LuaHelpers.OpenState):
///   1. Create a NuLua <see cref="LuauState"/> via <see cref="LuaHelpers.OpenState()"/> — opens
///      libs, registers the LuaVisibleTypeRegistry, the fixed64/f64math lib, and the game `require`.
///   2. Swap the default module source for a real-filesystem one rooted at the repo's
///      NFMWorld.Library (keys are VFS-style "data/..." paths, matching the game).
///   3. Install a GameThreadContext + a no-op DummyBackend so no FNA/graphics is touched.
///   4. Register the REAL LuaUiLibrary host (createInstance/setProperty/etc. -> View/Yoga); this
///      sets the `UiLib` global, which preact-luau/src/ui.luau captures at module load.
///   5. Load the real react.luau (preact-luau), then run benchmark scripts that return a `run` fn.
///
/// The scripts under scripts/ are VM-agnostic and shared unchanged with the Lua-CSharp branch;
/// only this file (and Program.cs's arg construction) differ between the two VMs.
/// </summary>
public sealed class BenchmarkHost
{
    readonly LuauState _state;
    readonly string _libraryRoot; // repo/NFMWorld.Library (parent of data/)

    public View? ActiveRoot { get; private set; }

    readonly Dictionary<int, Action<LuaValue>> _handlers = new();
    readonly object _gate = new();
    int _eventId;

    public BenchmarkHost(string libraryRoot)
    {
        _libraryRoot = Path.GetFullPath(libraryRoot);

        // Game-faithful state: libs + LuaVisibleTypeRegistry + fixed64/f64math + `require`.
        _state = LuaHelpers.OpenState();
        // Point the game `require` at a real-filesystem source so the `./`-relative preact-luau
        // graph resolves from disk (keys are "data/library/..." VFS-style paths). No VFS mount.
        RequireContext.Get(_state).Reset(new RepoModuleSource(_libraryRoot));

        // Headless: no real graphics/input backend, and a SynchronizationContext so
        // LuaUiLibrary's `defer` (which would otherwise throw without one) is satisfied.
        GameThreadContext.Install();
        IBackend.Backend = new DummyBackend();

        // Register the real host BEFORE loading react.luau: preact-luau/src/ui.luau
        // captures the `UiLib` global at module-load time.
        LuaUiLibrary.Register(_state, SetActiveRoot, Call, OnEvent);

        var react = LoadReact();
        _state["React"] = react;
    }

    // ---- LuaUiLibrary stand-in delegates ------------------------------------

    void SetActiveRoot(View view) => ActiveRoot = view;

    void Call(string method, LuaValue payload) { }

    Action OnEvent(string @event, Action<LuaValue> callback)
    {
        int id;
        lock (_gate) id = _eventId++;
        _handlers[id] = callback;
        return () => { lock (_gate) _handlers.Remove(id); };
    }

    // ---- Script loading ------------------------------------------------------

    LuaValue LoadReact()
    {
        var reactPath = Path.Combine(_libraryRoot, "data", "library", "react.luau");
        var source = File.ReadAllText(reactPath);
        // Chunk name "data/library/react.luau" so relative requires resolve to data/library/...
        var results = _state.DoString(source, "data/library/react.luau");
        return results.Length > 0 ? results[0] : LuaValue.Nil;
    }

    /// <summary>
    /// Load a benchmark script (scripts/*.luau) and invoke its returned `run(...)` function.
    /// The script chunk must return a single `run` function.
    /// Returns (cpuSeconds from os.clock, wallSeconds from a C# Stopwatch, all returns).
    /// </summary>
    public (double CpuSeconds, double WallSeconds, LuaValue[] Returns) RunScript(
        string scriptPath,
        params LuaValue[] args)
    {
        var source = File.ReadAllText(scriptPath);
        var module = _state.DoString(source, "bench_" + Path.GetFileName(scriptPath));
        var runFn = module.Length > 0 ? module[0] : LuaValue.Nil;
        var runFunction = runFn.ConvertLuaValue<LuaFunction>();

        var sw = Stopwatch.StartNew();
        var results = CallFunction(_state, runFunction, args);
        sw.Stop();

        double cpu = double.NaN;
        if (results.Length > 0 && results[0].TryConvertLuaValue<double>(out var c))
        {
            cpu = c;
        }
        return (cpu, sw.Elapsed.TotalSeconds, results);
    }

    /// <summary>Calls a Lua function, reads all its returns, and restores the stack.</summary>
    static LuaValue[] CallFunction(LuauState state, LuaFunction function, params LuaValue[] args)
    {
        var baseTop = state.GetTop();
        var count = state.Call(function, args);
        var results = new LuaValue[count];
        for (var i = 0; i < count; i++)
        {
            results[i] = state.ToLuaValue(baseTop + 1 + i);
        }
        state.SetTop(baseTop);
        return results;
    }

    // ---- Real-filesystem IModuleSource rooted at NFMWorld.Library -----------------

    /// <summary>
    /// Read-only real-filesystem <see cref="IModuleSource"/>. Keys are VFS-style slash paths
    /// rooted at "data" (e.g. "data/library/preact-luau/src/index.luau"), mapped onto the repo's
    /// NFMWorld.Library directory. Mirrors VfsModuleSource semantics without a VFS mount.
    /// </summary>
    sealed class RepoModuleSource : IModuleSource
    {
        const string AliasMarker = "@/";
        readonly string _root;

        public RepoModuleSource(string root) => _root = Path.GetFullPath(root);

        public string SourceId => "repo";

        string Map(string key) => Path.GetFullPath(Path.Combine(_root, key.Replace('/', Path.DirectorySeparatorChar)));

        public bool IsFile(string key) => File.Exists(Map(key));

        public bool IsDirectory(string key) => Directory.Exists(Map(key));

        public bool TryRead(string key, out string source)
        {
            var path = Map(key);
            if (!File.Exists(path))
            {
                source = null!;
                return false;
            }
            source = File.ReadAllText(path);
            return true;
        }

        public string ToChunkName(string key) => key;

        public bool TryGetKeyFromChunk(string chunkSource, out string key)
        {
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
                    key = "data/" + rest;
                    return true;
                }
            }
            key = null!;
            return false;
        }

        public bool TryResolveRoot(string name, out string key)
        {
            key = "data/" + name;
            return true;
        }

        public bool TryResolveRelative(string callerKey, string relative, out string key)
            => CombineRelative(callerKey, relative, '/', out key);

        public string Join(string baseKey, string subKey)
            => baseKey.Length == 0 ? subKey : baseKey + "/" + subKey;

        // Port of PathResolver.CombineRelative (internal in NFMWorldLibrary.Util).
        static bool CombineRelative(string callerKey, string relative, char separator, out string key)
        {
            var lastSeparator = callerKey.LastIndexOf(separator);
            var baseDir = lastSeparator < 0 ? string.Empty : callerKey[..lastSeparator];

            var segments = new List<string>();
            if (baseDir.Length > 0)
            {
                segments.AddRange(baseDir.Split(separator));
            }

            foreach (var part in relative.Split('/'))
            {
                switch (part)
                {
                    case "":
                    case ".":
                        continue;
                    case "..":
                        if (segments.Count > 0)
                        {
                            segments.RemoveAt(segments.Count - 1);
                        }
                        break;
                    default:
                        segments.Add(part);
                        break;
                }
            }

            key = segments.Count == 0 ? string.Empty : string.Join(separator, segments);
            return true;
        }
    }
}

