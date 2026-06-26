# Agent instructions — NFM-World

Keep guidance short and actionable. Reference files and patterns below when making changes.

DO NOT write PowerShell or shell scripts for code-editing tasks. ALWAYS use the code-editing tools available to you.

NFM World is a custom game engine and game written primarily in **C#**, targeting `net10.0`. The playable app lives in `nfm-world/` (`NFMWorld.csproj`) and depends on many sibling projects — notably `NFMWorld.Library`, `FNA.Core` (via NvgSharp), and `MonoGame.ImGuiNet`. Treat `nfm-world/` as the app entry point; engine/framework code is in `FNA/`; rendering and GUI glue is under `NvgSharp/`, `FontStashSharp/`, and `MonoGame.ImGuiNet/` (FontStashSharp is in the solution but not a direct ProjectReference of the app).

- **Big picture:** The playable app lives in `nfm-world/` (`NFMWorld.csproj`) and depends on many sibling projects (notably `NFMWorld.Library`, `NvgSharp.FNA.Core`, `NvgSharp.Text.FNA.Core`, `MonoGame.ImGuiNet`). Treat `nfm-world` as the app entry; engine/framework code is in `FNA/` and rendering/GUI glue under `NvgSharp/`, `FontStashSharp/`, and `MonoGame.ImGuiNet/`.

Key characteristics:
- **NFMWorld.Reactor** — a React-like virtual DOM framework with hooks, memo, context, and source-generated factory methods. Built on top of Yoga layout. This is the new UI system replacing XAML.
- A custom **shader pipeline**: shaders in `data/shaders/*.fx` are compiled to `.fxb` via `fxc.exe` during build.
- **Fixed-point math** (`FixedMathSharp`) for deterministic physics and gameplay logic.
- A **virtual file system** (`Maxine.VFS`) with path abstraction over real and in-memory backends.
- A Blender-based asset pipeline using the proprietary **RAD 3D** car format.

- **Build / run:** Use the .NET SDK (this repo targets `net10.0`). Typical commands:
  - Build entire workspace: `dotnet build nfm-world.slnx -c Debug`
  - Build single project: `dotnet build nfm-world/NFMWorld.csproj`
  - Run: `dotnet run --project nfm-world/NFMWorld.csproj`
  - Run tests: `dotnet test --no-build` from solution root or test project folder.

- **Shaders & tools:** Shaders in `data/shaders/*.fx` are compiled to `.fxb` via `fxc.exe` during build (`BuildShaders` target). On non-Windows builds the project expects `wine` + a Windows DirectX SDK `fxc.exe` (winetricks `dxsdk_jun2010`) or a `tools/fxc.exe` helper. If altering shader handling, preserve the MSBuild targets in `nfm-world/NFMWorld.csproj` that produce and include `.fxb` files.

- **Platform nuances:**
  - The project sets `AllowUnsafeBlocks` and several compile symbols (e.g. `USE_BASS`). Keep those when editing compilation logic.

- **Project patterns / conventions:**
  - Most subprojects are referenced with `ProjectReference` from `NFMWorld.csproj`; prefer keeping cross-project ref changes small and use `dotnet sln` only when adding/removing whole projects.
  - Game logic vs UI: `NFMWorld.Library` contains backend/game systems; UI, rendering and native interops live in `nfm-world/`, `NvgSharp/`, and `FNA/`.
  - **NFMWorld.Reactor** is the new VDOM UI framework; `NFMWorld.Reactor.Generator` provides Roslyn source generators for factory methods and typed VNode subclasses. Tests in `NFMWorld.Reactor.Test` and test fixtures in `NFMWorld.Reactor.TestFixtures`.
  - Data and assets: NFMWorld and NFMWorld.Library include `None Include="data\**\*" CopyToOutputDirectory=...` — follow existing CopyToOutputDirectory semantics rather than inventing new asset pipelines.

- **Dependencies & runtime notes:**
  - NuGet packages used by the app include `ImGui.NET`, `ManagedBass` (and related). When adding packages, prefer matching versions already in the csproj.
  - For local developer builds on Linux/macOS, ensure native dependencies (OpenGL drivers, libSDL, wine for shader compilation) are present.

- **Tests and CI hints:**
  - Run `dotnet test` at repo root; test projects are co-located with their libraries (e.g. `HoleyDiver.UnitTest`).
  - CI should `dotnet restore` then `dotnet build` then `dotnet test`. If CI runs on Linux/macOS, ensure native copy targets won't fail due to missing platform files — add conditional guards or include stub files as needed.

- **When editing MSBuild targets:** Inspect `nfm-world/NFMWorld.csproj` for patterns: shader compilation targets, copy-to-output items, and platform-specific Publish hooks. Changes here affect runtime asset layout; run a local `dotnet publish` to validate.

- **Where to look for behavior:**
  - Initialization / main loop: `nfm-world/NFMWorld.csproj` → `WorldGame.cs`, `NFMWorld.csproj` references `WorldGame.cs` as a logical entry point.
  - Game backend: [NFMWorld.Library](../NFMWorld.Library/NFMWorld.Library.csproj)
  - Rendering and fonts: `NvgSharp/`, `FontStashSharp/` and `FNA/`.

- **Examples to follow:**
  - Adding native files: put in the right location under NFMWorld.NativeLibs to load via the DllImport resolver.
  - Adding compiled assets (shaders): add `.fx` to `CompileShader` ItemGroup so builders include shader compilation automatically.

- **Do NOT:**
  - Remove or flatten the MSBuild platform conditionals without testing on all OSes.
  - Change shader/tool expectations without keeping a non-Windows fallback path (`tools/fxc.exe` or documented wine steps).

If anything above is unclear or you want examples inserted for a specific task (adding a native plugin, publishing for Linux, or modifying shader flow), tell me which area to expand and I will update this file.

---

## Build, Run & CI

```bash
# Build entire solution
dotnet build nfm-world.slnx -c Debug

# Build single project
dotnet build nfm-world/NFMWorld.csproj

# Run
dotnet run --project nfm-world/NFMWorld.csproj

# Run all tests
dotnet test --no-build          # from solution root
dotnet test                     # from individual test project folder
```

**CI pipeline:** `dotnet restore` → `dotnet build` → `dotnet test`. On Linux/macOS, add conditional guards to ensure platform-specific MSBuild copy targets don't fail for missing Windows-only native files.

### Shaders

Shaders in `data/shaders/*.fx` are compiled to `.fxb` by the `BuildShaders` MSBuild target via `fxc.exe`. On non-Windows:
- Use `wine` + a Windows DirectX SDK `fxc.exe` (via `winetricks dxsdk_jun2010`), **or**
- Provide a `tools/fxc.exe` helper shim.

To add a new shader, add the `.fx` source to the `<CompileShader>` ItemGroup in `NFMWorld.csproj`. Do not manually copy `.fxb` files.

### Source generator output

```bash
# Force regeneration of all source-generated files
Remove-Item -Recurse nfm-world/Generated
dotnet build nfm-world/NFMWorld.csproj
```

Generated files appear in `nfm-world/Generated/NFMWorld.Reactor.Generator/.../*.g.cs`:
- `Nodes.g.cs` — factory methods for VNode types (`FlexPanel(...)`, `Node(...)`, `View(...)`)
- `Nodes.Types.g.cs` — typed VNode subclasses with `With*` methods
- `Components.g.cs` — factory methods for Component types
- `Components.Types.g.cs` — typed `ComponentNode` subclasses

The csproj must have `<Compile Remove="Generated/**" />` to prevent the implicit glob from double-compiling them alongside the in-memory source gen output.

---

## NFMWorld.Reactor VDOM Framework (New UI System)

A React-like virtual DOM framework built on top of the Yoga layout engine. Replaces the legacy XAML-based UI system.

### Important notes

- Components ARE immutable! They are reused indefinitely when mounted but this is only an implementation detail and you
  should never rely on internal state. Pass inputs as props and use state for internal data. Use `UseState<T>` for
  stateful values and `UseEffect` for side effects.

### Key projects

| Project | Role |
|---|---|
| `NFMWorld.Reactor` | Core VDOM runtime: `VNode`, `VisualVNode`, `ComponentNode`, `Component`, hooks, `Reconciler`, `Context<T>`, `BasePropertySnapshot` |
| `NFMWorld.Reactor.Generator` | Roslyn incremental source generators: `ReactorNodeFactoryGenerator` (Yoga VNodes + factory methods), `ReactorComponentFactoryGenerator` (Component wrappers), `PropertyGenerator` |
| `NFMWorld.Reactor.Test` | MSTest tests (87 tests as of 2026-06) |
| `NFMWorld.Reactor.TestFixtures` | Shared test components, rebuilt to generate ComponentNode subclasses |

### VNode hierarchy

- **`VNode`** — base node with `Key` and `WithKey` fluent method
- **`VisualVNode`** — base for Yoga-backed nodes: `NodeType`, `Children`, `Classes`, `Name`, `CreateNative()`, `AssignProperties()`
- **`ComponentNode`** — hosts a `Component`; has `CreateComponent()`, `GetInputs()` (for memo)
- **Generated subclasses**: `NodeNode`, `FlexPanelNode`, `ViewNode` — each has typed `With*` methods, a nested `PropertySnapshot`, and generated `AssignProperties()`

### Component system

- **`Component`** — base class. Override `Render()` to return a `VNode` tree.
- **Hooks**: `UseState<T>`, `UseEffect`, `UseMemo<T>`, `UseRef<T>`, `UseCallback`, `UseContext<T>`, `ProvideContext<T>`, `UseObservable<T>`, `UseObservableProperty<T,TProp>`, `UseCollection<T>`
- **Memo**: enabled by default. `DisableMemo()` to opt out. Components skip `Render()` when inputs (constructor args + context versions) are unchanged.
- **Lifecycle**: `RenderViaReconciler(Reconciler, Visual?, ComponentNode)`, `Update()`, `Unmount()`; `OnMounted()`/`OnUnmounted()` virtuals; hooks re-run on each `Render()`.

### Reconciler

- **`Reconciler.Reconcile(VNode, Visual container, Visual? existingRoot)`** — diffs VNode tree against native Yoga tree.
- **Property system**: `AssignProperties(Visual, ref BasePropertySnapshot?)` on each VNode saves old values into a `PropertySnapshot`, then applies new values. `ReconcileVisualNode` restores stale properties from the previous pass's snapshot before applying current values (per-property staleness).
- **Children**: keyed reconciliation (`oldKeyMap`), positional matching for non-keyed children, type-change detection.
- **Component slots**: `_componentSlots` dictionary keyed by `(parent, childIndex)` persists component instances across reconciles.
- **Context stack**: `PushContextFrame`/`PopContextFrame` around each `ReconcileComponentNode`; `SetContext`/`GetContext` walk the stack.

### Context system

- **`Context<T>`** — typed context key with `DefaultValue` and `Version` (monotonically increments on `ProvideContext`).
- **`ProvideContext<T>(ctx, value)`** — bumps `ctx.Version` and sets value in the Reconciler's context stack frame.
- **`UseContext<T>(ctx)`** — reads from the stack (walks frames top→bottom), records version for memo comparison.
- Deep context propagation works through memo-skipped intermediates: the cached VNode tree still reaches the consumer ComponentNode.

### Source generators

- **`ReactorNodeFactoryGenerator`** — produces `Nodes.g.cs` (unified factory class with `FlexPanel(...)`, `Node(...)`, `View(...)` methods) and `Nodes.Types.g.cs` (typed VNode subclasses with `With*` methods, nested `PropertySnapshot`, `AssignProperties`).
  - Only emits types for the current project (assembly-based filtering via `SymbolEqualityComparer`).
  - Collects properties with `[Property]` attribute across the full Yoga hierarchy. `IsDeclared` tracks whether the property is first declared on the current type.
  - Factory parameters use `T?` (nullable) — not `Optional<T>` — to support implicit conversions through the built-in nullable conversion.
- **`ReactorComponentFactoryGenerator`** — produces `Components.g.cs` (factory methods) and `Components.Types.g.cs` (typed `ComponentNode` subclasses with `With*` methods, `CreateComponent()`, `GetInputs()`).

### Creating a new Component

1. Subclass `Component` in `NFMWorld.Reactor.TestFixtures` (or your project).
2. Add a public constructor with parameters (these become factory arguments).
3. Override `Render()` — use factory methods like `FlexPanel(...)` from `static WorldXaml.UI.Yoga.Nodes`.
   Every Component and Node will have a generated Nodes type within its namespace, so you can `using static XYZ.Nodes`
   to get the factory methods in scope.
4. Call `DisableMemo()` in the constructor if the component must always re-render.
5. Rebuild so the source generator produces the `ComponentNode` subclass and factory method.

### Key patterns

- **Factory methods** produce typed VNode instances: `FlexPanel(name: "x", opacity: 0.5f, children: [...])`
- **Fluent builders** on VNodes: `.WithName("x")`, `.WithKey("k")`, `.WithOpacity(0.5f)`
- **Shadowed methods**: generated subclasses shadow base `With*` methods with `new` to return the correct type (e.g., `FlexPanelNode.WithName` returns `FlexPanelNode`)
- **Stale properties**: if a property is set in pass N but omitted in pass N+1, it resets to its default value via the snapshot system

### Test commands

```bash
# Run all Reactor tests
dotnet test NFMWorld.Reactor.Test/NFMWorld.Reactor.Test.csproj

# Run specific test
dotnet test NFMWorld.Reactor.Test/NFMWorld.Reactor.Test.csproj --filter "Memo_SkipsRender"
```

---

### Code-Behind Patterns

**Accessing named elements after InitializeComponent:**
```csharp
public partial class MyView : Node
{
    public MyView()
    {
        InitializeComponent();
        // Named elements are now available
        TitleText.Text = "Updated";
    }
}
```

**Post-initialization setup** (see `PowerDamageBars.cs`):
```csharp
public PowerDamageBars()
{
    InitializeComponent();
    // Configure elements that need runtime data (or, preferably, use bindings!)
    PowerBar.BarColor = GetPowerBarColor(1f);
    PowerBar.Width = IBackend.Backend.LoadCachedImage("data/images/power.gif").Width;
}
```

### Current Limitations

- **No styles/templates** — All styling is inline or in code-behind
- **Limited type converters** — Only Font, Color, Measurement types have converters
- **Build task required** — XAML files must be in `<AvaloniaXaml>` ItemGroup to be compiled

### Troubleshooting

| Symptom | Cause / Fix |
|---|---|
| "Partial class with single part" warning | Expected; the source generator produces the other `partial` declaration |
| Missing `InitializeComponent` | Ensure XAML is in `<AvaloniaXaml>` ItemGroup and `x:Class` matches code-behind |
| "Type not found" at build | Check `xmlns` namespace matches actual C# namespace |
| "Property not found" at build | Ensure property has a public setter decorated with `[Property]` attribute |

### Yoga Layout Engine

The layout system wraps Facebook's **Yoga** (flexbox for native UIs). The C# wrapper exposes:
- `Node` — base layout node; every UI element owns one.
- Yoga enums (`YgDisplay`, `YgFlexDirection`, `YgAlign`, `YgJustify`, etc.) wrapped as C# enums with extension methods (`ToYogaDisplay()`, `ToNfmDisplay()`, etc.) to convert to/from native Yoga enum types.

Both directions of conversion (`→ native` and `← native`) must be present. Missing one direction causes obscure type errors in XAML-generated code when adding new enum values.

The property system (in `WorldXaml.UI.Yoga` namespace, implemented in `NFMWorld.Reactor`) is the reactive backbone. UI element properties that should be settable from XAML must be declared with the `[Property]` attribute.

---

## Shader Pipeline (HLSL / SPIR-V)

Shaders live in `data/shaders/*.fx` and are compiled to `.fxb` by the `BuildShaders` MSBuild target via `fxc.exe`. The `ShaderSourceGen` Roslyn source generator additionally wraps compiled shaders and emits C# binding code.

**`ShaderSourceGen` naming:** generated C# shader wrapper files use a deterministic naming convention based on shader entry point and target profile. Do not rename shaders without updating all downstream C# references.

---

## Virtual File System (Maxine.VFS)

Provides a path-abstraction layer decoupling game code from the real filesystem.

**Key types:**

| Type | Role |
|---|---|
| `IPath` | Abstract path interface |
| `MemoryPath` | In-memory path implementation |
| `IoPath` | Wraps real filesystem paths (internal) |
| `FallbackFileSystem` | Chains multiple `ReadOnlyFileSystem` implementations, trying each in order |

**Tested behaviours (MSTest):**
- `GetFullPath` resolves `..` segments correctly.
- `Combine` handles absolute path override on both Windows (`C:\...`) and Unix (`/...`).
- Path normalization converts `\` to `/`.
- `FallbackFileSystem` falls through on `FileNotFoundException` (in `OpenRead`/`GetAttributes`) and `DirectoryNotFoundException` (in `EnumerateFiles`/`EnumerateDirectories`). Other IO exceptions propagate immediately.

---

## FixedMath / Fixed-Point Math

`FixedMathSharp` provides fixed-point arithmetic for deterministic simulation:
- `Fixed4x4` — 4×4 transformation matrix
- Various `Fixed*` scalar and vector types

Fixed → float conversions are **lossy** by design. Never use `==` between fixed-point and float values; use epsilon tolerance in tests.

---

## Testing Infrastructure

- **Test framework:** MSTest (`[TestClass]`, `[TestMethod]`, `Assert.AreEqual`, `Assert.ThrowsException<T>`, `Assert.IsNotNull`). The project was converted from NUnit. **Never use NUnit APIs** (`[Test]`, `[TestFixture]`, `Assert.That`, `Assert.Throws`, etc.).
- **Test runner:** `dotnet test` from the solution root or individual test project folder.
- **Test projects:** `NFMWorld.Reactor.Test`, `NFMWorld.Library.Test`, `Maxine.VFS.Test`, `Maxine.Extensions.Test`.

**MSTest pattern:**

```csharp
[TestClass]
public class SomeTests {
    [TestMethod]
    public void MethodName_Scenario_ExpectedBehavior() {
        // Arrange
        var sut = new SystemUnderTest();

        // Act
        var result = sut.DoThing();

        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Method_InvalidInput_Throws() { ... }
}
```

**Coverage priorities:**
- VFS path operations (`GetFullPath`, `Combine`, normalization, `FallbackFileSystem`)
- Lua generator output correctness (method table structure, metamethods, constructor presence/absence, operator overloads, InlineArray indexers)
- XAML source generator round-trip (XAML in → expected C# out, both literal and `{Binding}` properties)
- FixedMath conversion accuracy

---

## Lessons Learned by Subsystem

### XAML Source Generator / XamlX Emitter

**L1 — C# emitter and IL emitter are separate code paths.** Code that works in the IL path often does not work in the C# path. Always inspect the generated `.g.cs` file — do not just check that the build succeeded.

**L2 — Binding-backed property setters silently emit nothing when the C# emitter doesn't handle `MakeGenericMethod`.** The symptom is a chain of `__tmpN = __tmpM;` assignments with no setter call between them in the generated code. Fix: ensure `EmitMethodCall` correctly handles the `ConstructedCSharpType` returned by `MakeGenericMethod`.

**L3 — `{Binding}` and literal properties use different setter classes.** Literals go through `ValueSetter`; bound properties go through `BindingSetter`. Test both when modifying the XAML code generation pipeline.

**L4 — `Dup` temp variables must be pre-declared at method scope.** They must survive `goto` labels that are emitted outside the block that created the `Dup`. Using `var` inside a conditional block will cause a compile error in generated code.

**L5 — `TypeExtension` nodes (`{x:Type}`) require semantic model resolution.** They are not string attributes. The AST transformer must look up the referenced type via the Roslyn `ITypeSymbol` API.

**L6 — Keep the XamlX fork minimal.** Do not touch it if possible; if you must, keep it isolated to a single file and avoid complex logic. This minimizes merge conflicts with upstream.

### Yoga / UI System

**L1 — Yoga enum wrappers need bidirectional conversions.** Add both `→ native` and `← native` directions (via extension methods like `ToYogaDisplay()` / `ToNfmDisplay()`). Missing one causes obscure type errors in XAML-generated code.

**L2 — Properties must use the `[Property]` attribute to be settable from XAML.** Plain auto-properties are not recognized by the XAML transformer.

### Shader Pipeline

**L1 — Do not rename shaders without updating downstream C# references.** `ShaderSourceGen` generates wrapper classes with deterministic names based on shader entry points.

### VFS / Path Handling

**L1 — Replicate `Path.Combine` semantics exactly.** An absolute path on the right-hand side must discard the left-hand side. Test with both Windows and Unix absolute paths.

**L2 — Always normalize `\` to `/`.** Code consuming VFS paths must not assume OS-native separators.

**L3 — `FallbackFileSystem` falls through on `FileNotFoundException` (in `OpenRead`/`GetAttributes`) and `DirectoryNotFoundException` (in enumerate methods).** Other IO exceptions propagate immediately. Test this boundary explicitly.

### FixedMath

**L1 — Fixed → float is lossy. Never use `==`.** Use epsilon-based comparison in all tests.

**L2 — `FixedMathSharp` updates break dependent projects.** Run all downstream test suites after any bump.

### NFMWorld.Reactor / VDOM

**L1 — `AssignProperties` only handles properties with `[Property]` attribute.** Properties without it (like `Classes`) must be applied directly by the Reconciler.

**L2 — Generated VNode subclasses only declare fields for `IsDeclared` properties.** Fields for inherited properties come from the base class (they're `protected`). The `AssignProperties` method iterates all hierarchy properties and accesses fields via inheritance.

**L3 — The `PropertySnapshot` includes ALL hierarchy properties, not just declared ones.** Its `AssignProperties` method restores ALL properties that have `HasValue = true`.

**L4 — The Reconciler's `ReconcileVisualNode` restores stale properties from the previous snapshot BEFORE calling `AssignProperties`.** This is the key to per-property staleness: `prev.AssignProperties` restores old values, then `AssignProperties` overwrites with current values. `SwapSnapshots` just rotates `prev = current; current = null`.

**L5 — Don't call `current.AssignProperties` after `prev.AssignProperties` in `SwapSnapshots`.** The snapshot stores OLD values (pre-assignment), so re-applying it would restore the wrong state. Stale restoration must happen before `AssignProperties`, not after.

**L6 — Component instances are reused via `_componentSlots` keyed by `(parent Visual, childIndex)`.** `TryReuseComponent` checks `HasSameInputs` to decide whether to reuse; if inputs differ, a new instance is created (constructor runs with new values).

**L7 — Memo `ShouldSkipRender` compares constructor inputs AND context versions.** `HasSameInputs` only compares inputs (for instance reuse). `SaveMemoState` always saves inputs even when memo is disabled (needed for instance reuse decisions).

**L8 — Deep context propagation works through memo-skipped intermediates.** The cached VNode tree from a memo-skipped parent still contains child ComponentNodes. The Reconciler walks into them and `ShouldSkipRender` detects context version changes.

**L9 — Non-keyed children match by position and type compatibility.** Keyed children match by key first. Positional matching only applies to non-keyed existing children at the same index.

**L10 — Source generators can't see each other's output.** The `ReactorNodeFactoryGenerator` uses `[Property]` attribute detection (not `*Property` static fields) because `PropertyGenerator`'s output isn't visible at analysis time.

**L11 — Factory parameters use `T?` not `Optional<T>`.** C# allows chaining one user-defined implicit conversion + the built-in nullable conversion. `Optional<T>` required two user-defined conversions, which C# rejects.

**L12 — `ClearChildren` handles `Children = null`.** The Reconciler calls `ClearChildren` when a VNode has no children but the native node has existing children, ensuring old children are removed.

**L13 — `ClearChildren` is not a separate method.** The Reconciler handles null children inline in `ReconcileVisualNode` by passing an empty array to `ReconcileChildren`, which then removes stale children.

---

## Agent Working Guidelines

### Before starting any task

- Identify which subsystem(s) are involved and re-read the relevant section of this document.

### While working

4. **Set up a todo list for multi-step tasks.** The codebase is complex enough that losing track mid-task causes compounding errors.
5. **Verify generated output, not just build success.** After any source generator change, read the corresponding `.g.cs` file and confirm the emitted C# is structurally correct.
6. **Test both literal and `{Binding}` properties** when touching the XAML emitter. They go through different code paths.
7. **Run the full test suite for the affected project** — many edge cases have dedicated tests (e.g., `NFMWorld.Reactor.Test`, `NFMWorld.Library.Test`).
8. **Never delete a test.** If an interface changed, update the test to match the new contract.

### After completing a task

9. Update `.github/copilot-instructions.md` — record any architectural decisions, new patterns, or newly discovered caveats.
10. Ensure all tests pass in affected projects.
11. If you introduced or significantly changed a subsystem, update the relevant section of this document.

### Do NOT

- Remove or flatten the MSBuild platform conditionals without testing on all OSes.
- Change shader/tool expectations without keeping a non-Windows fallback (`tools/fxc.exe` or documented wine steps).
- Use NUnit APIs — the project uses MSTest.
- Use `{Binding}` in XAML and assume it works without verification — always check the generated `.g.cs` output.
- Assume MAUI or upstream Avalonia documentation applies verbatim to the custom XAML implementation used here.
- Rely on OS-native path separators anywhere in game or test code — use VFS normalization.

### Common gotchas at a glance

| Gotcha | Rule |
|---|---|
| Test framework | MSTest only — no `Assert.That`, `[Test]`, `[TestFixture]` |
| Source gen output | Check `nfm-world/Generated/` — do not trust a clean build alone |
| XAML binding properties | Missing or incorrect setter calls in generated output = check `.g.cs` file for correct property assignment code |

---