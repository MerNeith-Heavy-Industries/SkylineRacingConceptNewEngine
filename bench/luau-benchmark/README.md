# LuauBenchmark — Lua VM benchmark (lua-csharp vs NuLua/Luau)

A standalone console app that benchmarks the **same VM-agnostic Luau workloads** under two
Lua VMs so they can be compared apples-to-apples:

- **This branch**: the Lua-CSharp VM (`Lua-CSharp/src/Lua`), driven via the real game UI host.
- **The other (Luau) branch**: a fork that swaps the VM glue to `NuLua.Luau` and runs the
  **identical** `.luau` scripts.

It runs headlessly — no FNA/MonoGame rendering. The UI uses the real `LuaUiLibrary` C# host
and a `DummyBackend` so no graphics/input device is touched.

## Scenarios

| Scenario | What it measures | Bottleneck |
|---|---|---|
| `fixed64` | Heavy fixed64 arithmetic + `f64math` trig (distilled from `el_stupido.luau`): a min-distance scan over N nodes plus sin/cos/sqrt avoidance math per iteration. | fixed64 ops + f64math C# interop |
| `preact-small` | Mount + re-render a **16-node** preact-luau tree `10_000`×, with **fresh** prop/style tables each render so `diffProps` fires a host `setProperty`/`commitTextUpdate` per node. | C# interop (host crossings dominate) |
| `preact-large` | Mount + re-render a **1024-node** preact-luau tree `100`×, with **stable** (module-constant) prop/style refs so the reconciler makes ~zero host calls. | pure-Lua reconciler walk |

`preact_render.luau` selects the regime via the `freshProps` flag; `Program.cs` hard-codes the
workloads. Tune the constants in `Program.cs` (or the script defaults) to taste.

## Build & run

```bash
# Build
dotnet build bench/luau-benchmark/LuauBenchmark.csproj -c Release

# Run everything (best-of-3 by default)
dotnet run --project bench/luau-benchmark/LuauBenchmark.csproj -c Release

# Run a single scenario, N best-of runs
dotnet run --project bench/luau-benchmark/LuauBenchmark.csproj -c Release -- fixed64 5
dotnet run --project bench/luau-benchmark/LuauBenchmark.csproj -c Release -- preact-small 3
dotnet run --project bench/luau-benchmark/LuauBenchmark.csproj -c Release -- preact-large 3
```

### Output

Each scenario prints:

```
  preact-small  CPU    15421.875 ms | wall    16518.080 ms | wall/CPU   1.07x
```

- **CPU** — `os.clock()` delta measured entirely **inside Lua** (total process CPU time). This is
  the fair cross-VM metric.
- **Wall** — C# `Stopwatch` around the same run. On this noisy machine wall time is inflated by
  GC/console/system contention (see `lua-csharp.md`), so prefer CPU when comparing VMs.
- **wall/CPU** — should be ≥ 1; much larger than ~1.2 means the machine is noisy for that run.

`fixed64` also prints a **checksum** (a `tostring(fixed64)` sentinel) that must match across VMs —
it guards against arithmetic divergence between Lua-CSharp and Luau.

## Layout

```
bench/luau-benchmark/
├── LuauBenchmark.csproj      # net10.0 console; refs NFMWorld.Library + Lua-CSharp
├── Program.cs                # CLI, scenario dispatch, result printing
├── BenchmarkHost.cs          # ★ THE VM-SPECIFIC GLUE (fork this to NuLua)
├── scripts/                  # VM-agnostic .luau — SHARED unchanged across branches
│   ├── fixed64_kernel.luau
│   └── preact_render.luau
└── README.md
```

## How the host works

`BenchmarkHost` mirrors the real game wiring (`nfm-world/UI/UiRenderer.cs`), minus rendering:

1. Create a Lua-CSharp `LuaState` (`LuaPlatform` with `RequireByString=true`, `SystemOsEnvironment`
   for `os.clock`, `unpack` global, `LuaVisibleTypeRegistry.RegisterAll`).
2. `GameThreadContext.Install()` + `IBackend.Backend = new DummyBackend()` → headless.
3. `LuaUiLibrary.Register(state, setActiveRoot, call, onEvent)` — registers the **real** C# UI
   host (`createInstance`/`setProperty`/… → `View`/`Component`/Yoga).
4. Load the real `react.luau` (preact-luau). `preact-luau/src/ui.luau` captures `_G.UiLib` at module
   load, so the host is registered **before** `react.luau` is loaded.
5. `LibraryFileSystem` (an `ILuaFileSystem`) roots the `./`-relative preact-luau require graph at
   `NFMWorld.Library/data/library` on the real filesystem — no VFS mount needed.

Benchmark scripts are plain chunks that `return run(...)`; the host loads them, calls them with
args, and reads back the `os.clock()` delta.

## Forking to the real Luau VM (NuLua)

The `.luau` scripts are intentionally VM-agnostic (no table destructuring, no Luau-only syntax).
To compare against the real Luau VM in the other branch, rewrite **only** the VM glue:

1. **`BenchmarkHost.cs`** — swap `LuaState`/`LuaPlatform`/`LuaVisibleTypeRegistry` (Lua-CSharp) for
   the `NuLua.Luau` API: `LuauState.Create()` / `OpenLibraries()`, `state["X"] = …`, `DoString`,
   `LuaValue.FromPrimitive(id, v)` (id 0 = Fixed64) for the fixed64 type, `state.CreateUserData`.
   The `fixed64(...)` ctor and `f64math.*` globals must be registered in NuLua (the game's Lua
   runtime migration handles this). The `LuaUiLibrary` C# host also needs a NuLua counterpart.
2. **`Program.cs`** — scenario dispatch is VM-agnostic already; only fix the host type.
3. `scripts/` — leave **unchanged**.

`IBackend.Backend = new DummyBackend()`, `GameThreadContext.Install()`, and the `LuaUiLibrary`
stand-in delegates carry over unchanged (they're VM-agnostic C#).

## Notes

- **Quiet logging**: the game logs every `setProperty`/`commitTextUpdate` at `Debug` (floods stdout
  and skews timing). `Program.cs` sets `NFMW_LOG_MIN_LEVEL=Warning` before the host is created.
  This is backed by a small, backwards-compatible env-var override added to `NFMWorld.Library/Logging.cs`
  (defaults to the previous Trace-in-Debug / Debug-in-Release behaviour when unset).
- **No new `CefBrowser`/UI phase** is created; this is a plain console process.
- Only `os.clock`-compatible runtimes can share the timing contract — both Lua-CSharp and Luau
  expose `os.clock`, so CPU numbers are directly comparable.
