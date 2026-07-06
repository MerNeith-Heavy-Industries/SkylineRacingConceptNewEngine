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

- **Factory methods** produce typed VNode instances: `FlexPanel(name: "x", opacity: 0.5f, children: [...])` (access with `using static` on the generated `Nodes` class in the same namespace).
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

### Troubleshooting

| Symptom | Cause / Fix |
|---|---|
| "Partial class with single part" warning | Expected; the source generator produces the other `partial` declaration |

### Common Nodes/Components

- FlexPanel — a Yoga-backed container with flex layout
- PaintedBox — like FlexPanel but with a background and border; supports `BackgroundColor`, `BorderColor`, `BorderWidth`, `BorderRadius`
- TextRun — a rich text layouting component with many knobs for tweaking
- ContentsPanel — renders its children without participating in layout. Equivalent to `display: contents`
- View — usually the top-level element of the tree. Required for root Visual detection.
- Modal — part of NFMWorld.UI. Renders a VNode centered horizontally and vertically on screen.

### Yoga Layout Engine

The layout system wraps Facebook's **Yoga** (flexbox for native UIs). The C# wrapper exposes:
- `Node` — base layout node; every UI element owns one.
- Yoga enums (`YgDisplay`, `YgFlexDirection`, `YgAlign`, `YgJustify`, etc.) wrapped as C# enums with extension methods (`ToYogaDisplay()`, `ToNfmDisplay()`, etc.) to convert to/from native Yoga enum types.

Both directions of conversion (`→ native` and `← native`) must be present. Missing one direction causes obscure type errors in XAML-generated code when adding new enum values.

The property system (in `WorldXaml.UI.Yoga` namespace, implemented in `NFMWorld.Reactor`) is the reactive backbone. UI element properties that should be settable from XAML must be declared with the `[Property]` attribute.

### Reactor Styling System

A CSS-in-C# styling system built on `StyleSheet`, `StyleSheetStyles`, and the `Styles()` factory. Styles cascade from stylesheets to native Visuals at reconcile time.

**Key types:**

| Type | Role |
|---|---|
| `StyleSheet` | Container with 4 `StyleSheetStyles` slots: `Default`, `Hover`, `Active`, `Focus` |
| `StyleSheetStyles` | Struct holding ~55 CSS-like properties (layout, paint, text) — all nullable |
| `StyleSheetState` | `[Flags]` enum: `Normal=0`, `Hover=1`, `Active=2`, `Focus=4` |

**Key files:**
- `NFMWorld.Reactor/StyleSheetStyles.cs` — `StyleSheet`, `StyleSheetStyles`, `StyleSheetState`, `Merge()`
- `NFMWorld.Reactor/StyleFactory.cs` — `Nodes.Styles()` factory method
- `NFMWorld.Reactor/Visual.cs` — `Style` property, `UpdateStyleSheet()`, `GetSheetState()`
- `NFMWorld.Reactor/Node.cs` — `UpdateStyles()` override (layout props)
- `NFMWorld.Reactor/VisualVNode.cs` — `_style` field + `WithStyle()`

**The `Styles()` API** (from `static Nodes`):

```csharp
// Single entry point with all CSS-like properties + pseudo-state sub-sheets
StyleSheet Styles(
    // ~55 layout/paint/text params (all nullable)
    FlexDirection? flexDirection = null,
    Align? alignItems = null,
    Color? backgroundColor = null,
    float? fontSize = null,
    // ...
    // Pseudo-state sub-sheets:
    StyleSheet? hover = null,
    StyleSheet? active = null,
    StyleSheet? focus = null
)
```

**Usage pattern** (static cached sheets preferred):

```csharp
public static class Styles
{
    public static readonly StyleSheet Button = Styles(
        flexDirection: FlexDirection.Row,
        alignItems: Align.Center,
        minWidth: 230, minHeight: 35,
        backgroundColor: Color.Transparent,
        borderColor: Color.Transparent,
        borderRadius: 5,
        hover: Styles(
            backgroundColor: Colors.Background,
            borderColor: Colors.Primary
        )
    );
}

// Apply via factory method:
PaintedBox(style: Styles.Button, children: [...])

// Or via fluent builder:
FlexPanel(...).WithStyle(Styles.Button)
```

**How styles flow through the pipeline:**

1. **Factory method** → `if (style is not null) n.WithStyle(style)` → stores in `VNode._style`
2. **`AssignProperties`** (generated) → sets `typedVisual.Style = _style` → triggers `Visual.Style` setter
3. **`Visual.Style` setter** → calls `UpdateStyleSheet()` which computes `GetSheetState()` (`Normal | Hover | Active | Focus`)
4. **`GetStylesForState(state)`** → merges `Default` + `Hover?` + `Active?` + `Focus?` via `StyleSheetStyles.Merge` (last non-null wins)
5. **`UpdateStyles(old, new)`** → each Visual subclass resets old values to defaults, applies new ones:
   - `Node.UpdateStyles()` → layout props (Flex, Margin, Padding, Width/Height, etc.)
   - `PaintedBox.UpdateStyles()` → + border/background colors, border radii
   - `TextRun.UpdateStyles()` → + font, foreground, stroke, text alignment

**Precedence: direct property > style > default.** The generated `AssignProperties` sets `Visual.Style` first (which triggers `UpdateStyles` → sets layout/paint/text props), then sets direct properties afterward. Direct values overwrite any style-derived values.

**Pseudo-state transitions** are automatic: when `Visual.IsHovered`, `IsActive`, or `IsFocused` changes, the setter triggers `UpdateStyleSheet()` which re-merges the appropriate pseudo-state sheets and calls `UpdateStyles()` to apply the diff.

**Composition / merging:**

```csharp
// Merge multiple sheets — later sheets win for non-null properties
StyleSheet combined = StyleSheet.Merge(baseSheet, overrideSheet);
// or via implicit operator:
StyleSheet combined = new[] { baseSheet, overrideSheet };
```

**Staleness and the snapshot system:** The generated `AssignProperties` saves `typedVisual.Style` into the property snapshot before overwriting. On the next reconcile pass, `prev.AssignProperties` restores the old style, then the current style is applied. This ensures that when a style is removed from a VNode, the native Visual is reset correctly.

**Lessons learned:**

**L1 — Styles are applied BEFORE direct properties in `AssignProperties`.** This ordering is intentional and gives direct props priority. Do not reorder without updating tests.

**L2 — Cache styles as `static readonly` fields.** Creating new `StyleSheet` instances every render allocates unnecessary objects. The `Styles.Button` pattern in `MainMenuView.cs` is the recommended approach.

**L3 — `UpdateStyles` resets old values to defaults before applying new ones.** This means omitting a property from a style sheet resets it. Use `StyleSheet.Merge` to layer partial sheets safely.

**L4 — Pseudo-state sub-sheets are just `StyleSheet.Default` lifted into the slot.** In `Styles()`, `hover?.Default ?? default` is used (not `hover` itself). This means nested pseudo-state styles are flattened into the parent sheet's Hover/Active/Focus slots.

**L5 — No dedicated style unit tests exist.** The styling system is tested implicitly through UI integration. If making significant changes, manually verify with a running app.

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

### Polygon Triangulation (HoleyDiver)

The `HoleyDiver` project (`HoleyDiver/Program.cs`) provides a robust polygon triangulator for non-planar 3D n-gons with holes defined by self-intersecting paths. It uses **Poly2Tri** (constrained Delaunay) as the primary triangulator with an ear-cut fallback.

**Pipeline overview:**

1. **Best-fit plane projection** — Compute centroid + covariance matrix → eigenvector for plane normal. Project 3D vertices to 2D via `GetProjectionBasis`. Falls back to axis-aligned projections (XY/XZ/YZ) if the best-fit plane collapses distinct 3D points.
2. **Vertex deduplication** — Merge vertices within epsilon (`1e-5`) using `Vector2.Distance`. Build an `indexMap` from original indices → unique indices.
3. **Region extraction** (`ExtractRegions`) — Detects holes in self-intersecting paths by finding mirrored vertex sequences. Ported from a Python reference algorithm.
4. **Outer boundary reconstruction** — After `ExtractRegions`, the outer polygon is reconstructed from the original path by **excluding all hole vertices** (in path order). This is critical: do NOT use convex hull as it strips concave features.
5. **Bridge vertex filtering** — Vertices shared between outer and holes (bridge points from self-intersection) are removed from hole definitions to avoid duplicate constraints in Poly2Tri.
6. **Poly2Tri triangulation** — Outer polygon + cleaned holes passed as `Polygon` constraints. Triangles mapped back through unique→original indices.
7. **Incomplete triangle filtering** — Only triangles with all 3 vertices successfully mapped to original indices are emitted. Poly2Tri may produce degenerate triangles when hole vertices share edges with the outer boundary.

**Key types / entry point:**

| Type | Role |
|---|---|
| `PolygonTriangulator.Triangulate(IReadOnlyList<Vector3>)` | Main entry — returns `TriangulationResult` with `Triangles`, `PlaneNormal`, `Centroid`, `RegionCount` |
| `ExtractRegions(List<int>, List<Vector2>)` | Mirrored-sequence hole detection; returns list of poly-lines with holes marked by `-1` prefix |
| `Poly2Tri.Polygon` / `DTSweepContext` | Constrained Delaunay triangulation |

**Lessons learned:**

**L1 — Convex hull destroys concave features. Never use it as an outer boundary replacement.**
The Graham scan convex hull was initially used to "fix" incomplete outer boundaries from `ExtractRegions`. This silently removed concave indentations (e.g., the bottom indentation of a car rear panel: vertices `(19,-43)`, `(0,-45)`, `(-19,-43)` were dropped). The correct outer boundary **must** be reconstructed from the original path by filtering out hole vertices while preserving path order.

**L2 — `ExtractRegions` may produce incomplete outer boundaries. Always validate and reconstruct.**
The mirrored-sequence detection algorithm can leave the outer polygon with only a subset of vertices. The workaround: collect all vertices belonging to non-outer regions (holes), then rebuild `polyLines[0]` as `initialPoly \ holeVertices` (in original path order, with deduplication).

**L3 — Poly2Tri requires clean hole definitions. Filter bridge vertices.**
Self-intersecting path holes share "bridge" vertices with the outer polygon (the points where the path crosses itself). These must be removed from hole vertex lists before passing to Poly2Tri, otherwise the triangulator sees duplicate constraints and may fail or produce degenerate output.

**L4 — Map triangles through unique→original indices carefully. Reject incomplete ones.**
Poly2Tri works with the deduplicated unique vertex set. Each triangle vertex must be mapped: `PolygonPoint` → `uniqueVertices` index → `indexMap` → original 3D vertex index. If any of the 3 vertices fails to map (e.g., Poly2Tri created a triangle using a point not in the original set), **reject the entire triangle**. Without this filter, the triangle count can be non-integer.

**L5 — The Python hole-finding algorithm was ported directly.**
The `ExtractRegions` method is a direct C# translation of a Python reference implementation. It finds mirrored sequences in a self-intersecting path by testing all `(i,j)` pairs, walking forward from `i` and backward from `j`, measuring the length of matching vertices. When `le == 1` (mirror length 1), it checks containment via `AllPointsInPolygon` to decide whether to swap outer/hole roles. The algorithm requires `polyLines[0].Count >= 6` to continue (minimum path for a hole).

**L6 — Polygon winding direction matters for Poly2Tri but is handled automatically.**
Poly2Tri's `Polygon` constructor and `AddHole` handle winding internally. Do NOT manually reverse hole winding before passing to Poly2Tri — the library expects holes in their natural winding and will reverse them if needed.

**L7 — Best-fit plane fallback is essential for near-planar or degenerate input.**
When the covariance matrix produces a near-zero normal (length < `1e-10`), fall back to Newell's method (sum of cross products of adjacent edges). If that also fails, default to `Vector3.UnitZ`. The projection validator also checks that no two 3D points collapse to the same 2D point under the chosen projection.

**L8 — Do NOT add "safety" guards to the mirrored-sequence walker.**
The Python algorithm walks `while k0 != k1 && poly[0][k0] == poly[0][k1]`. An earlier C# implementation added `maxMatchIterations` bounds and extra `break` conditions (nextK0 == k1, k0 == nextK1) that diverged from the reference, causing subtle mismatches. The walker naturally terminates because `k0` advances forward and `k1` advances backward — they either meet or mismatch. Trust the reference algorithm.

**L9 — The `le == 1` containment check differs from `le > 1`.**
When the mirrored sequence length is exactly 1, the Python algorithm checks if all points of `polyLines[0]` are inside the new region (`AllPointsInPolygon(points0, pointsNew)`). If so, it swaps them (the new region becomes the outer). The old C# code also required `!newInsidePoly0` (an "only one contains" check), which was wrong. For `le > 1`, no containment check is performed — the new region is always treated as a hole.

**L10 — The `CombineWithHoles` / ear-cut path is fallback-only.**
`CombineWithHoles` (bridge-based hole merging for ear-cut) and `EarCutTriangulateSimple` are the fallback path used only when Poly2Tri throws. They are NOT exercised during normal operation. Changes to the primary pipeline should focus on `ExtractRegions` + Poly2Tri.

**L11 — Test polygons are embedded in `Main()`.**
Two test cases exist in `Program.Main()`: (1) a windshield-shaped polygon with 1 rectangular hole (19 vertices, planar Z≈207.4), (2) a car rear panel with 2 holes and concave bottom indentation (19 vertices, near-planar Z≈-103). Swap between them by commenting/uncommenting vertex blocks. Validate with `dotnet run 2>$null | Select-String -Pattern 'Plane|Regions|Triangles:'`.

**Common gotchas:**

| Gotcha | Rule |
|---|---|
| Convex hull for outer boundary | **Never** — reconstruct from original path minus hole vertices |
| Duplicate vertices | Deduplicate with epsilon before region extraction |
| Bridge vertices in holes | Filter out vertices that also appear in outer polygon |
| Poly2Tri degenerate triangles | Check `triIndices.Count == 3` before emitting |
| Hole marker convention | Holes are prefixed with `-1` in the poly-lines list |
| Ear-cut fallback | Only used when Poly2Tri throws; filters triangles by centroid-in-hole test |
| Safety guards in walker | Do NOT add `maxMatchIterations` or extra `break` conditions — trust the reference algorithm |
| `le == 1` containment check | Check only `AllPointsInPolygon(points0, pointsNew)` — not both directions |
| Test polygon swapping | Comment/uncomment vertex blocks in `Main()`; validate with `dotnet run` |

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