# NFMWorld.Reactor — Agent Session Export
## Date: 2026-06-26

---

## Session Goal
Fix all tests and game code to work with the new Reactor architecture changes:
1. `Component.Update` now triggers a state transition via `SynchronizationContext` (like React's async batching)
2. `Reconciler` is now `internal` — must use `ReactorDom` as the public API
3. Component cannot be mounted without a Reconciler; must be tree-hosted
4. Component no longer exposes its internals (`Mount`, `Update`, `Unmount`, `Reconciler` are all `internal`)

---

## Architecture Overview (Post-Changes)

### Public API Surface
- **`ReactorDom`** — the entry point. Wrap it around a `Reconciler`. Provides `Mount(Visual container, VNode vnode)`.
  - Constructor: `ReactorDom(SynchronizationContext synchronizationContext)`
  - Also subscribes to `HotReloadService.UpdateApplicationEvent` for hot reload.
  - `Mount()` calls `_reconciler.Reconcile(vnode, container, Root)` internally.
- **`ComponentNodeFactory`** — static factory for creating `ComponentNode` instances:
  - `Create<T>(params object?[]? args)` — typed, creates via `UntypedComponentNode(Type)`
  - `Create(Component instance)` — wraps a pre-existing instance via `UntypedComponentNode(Component)`

### Internal Types (accessible via `InternalsVisibleTo`)
- **`Reconciler(SynchronizationContext)`** — diffs VNode tree against native Yoga nodes
  - `Reconcile(VNode, Visual container, Visual? existingRoot)` — public
  - `ReconcileNode(VNode, Visual? existing)` — internal
  - `FinishPass()` — runs `UnmountStaleComponents()` + `SwapSnapshots()`
  - `MarkComponentVisited(Component)` — call before `FinishPass` in tree-hosted update path
  - `ScheduleOnNextTick(object key, Action)` — queues work via `SynchronizationContext.Post`
- **`Component.Update()`** — schedules re-render via `ScheduleOnNextTick`, calls `MarkComponentVisited` before `FinishPass`
- **`UntypedComponentNode`** — fallback for components without generated wrappers

### SynchronizationContext & Async Updates
- `Component.Update()` calls `Reconciler.ScheduleOnNextTick(this, callback)`
- `ScheduleOnNextTick` stores work in `_workOnNextTick` dict (keyed by component), calls `synchronizationContext.Post(Tick, this)`
- `Tick` swaps `_workOnNextTick ↔ _otherWorkOnNextTick` and processes all queued work
- For tests: `SynchronousSynchronizationContext` overrides `Post` to execute immediately

### Critical Bug Fix: `MarkComponentVisited`
When `Component.Update()` re-renders a tree-hosted component, the reconciler's `ReconcileComponentNode` is NOT called (the update bypasses it). But `FinishPass()` uses `_visitedComponents` to decide which components to unmount. Since the updating component was never added to `_visitedComponents`, `FinishPass` would unmount it.

**Fix**: `Reconciler.MarkComponentVisited(Component)` adds the component to `_visitedComponents`. Called in `Component.Update()`'s scheduled callback before `FinishPass()`.

---

## Files Changed

### Framework Files (NFMWorld.Reactor)

| File | Change |
|---|---|
| `NFMWorld.Reactor.csproj` | Added `<InternalsVisibleTo Include="NFMWorld.Reactor.Test" />` |
| `Reactor/Reconciler.cs` | Added `MarkComponentVisited(Component)` method |
| `Reactor/Component.cs` | Added `Reconciler.MarkComponentVisited(this)` call in `Update()`'s scheduled callback |
| `Reactor/ComponentNode.cs` | Added `Create(Component instance)` overload; `UntypedComponentNode` now supports both type-based and instance-based construction (lost the primary constructor, now has two explicit constructors) |

### Test Infrastructure (NFMWorld.Reactor.Test)

| File | Change |
|---|---|
| `TestHelpers.cs` | **NEW** — `SynchronousSynchronizationContext`, `TestHelpers.MountComponent<T>(args)`, `TestHelpers.MountVNode(vnode)`, `TestHelpers.CreateReconciler()` |

### Test Files (NFMWorld.Reactor.Test)

All 6 test files updated:

| File | Changes |
|---|---|
| `ReconcilerCoreTests.cs` | `new Reconciler()` → `TestHelpers.CreateReconciler()` (15+ occurrences) |
| `ReconcilerComponentTests.cs` | `new Reconciler()` → `TestHelpers.CreateReconciler()` (7 occurrences) |
| `ComponentLifecycleTests.cs` | `Mount(comp)` → `TestHelpers.MountComponent<T>(args)`; removed `Mount` helper; Reconciler fix |
| `HooksTests.cs` | `Mount(comp)` → `TestHelpers.MountComponent<T>(args)`; removed `Mount` helper |
| `MemoizationTests.cs` | `Mount(comp)` → `TestHelpers.MountComponent<T>(args)`; removed `Mount` helper; Reconciler fix |
| `ContextTests.cs` | `Mount(comp)` → `TestHelpers.MountComponent<ContextConsumerComponent>(ctx)`; removed `Mount` helper; Reconciler fix |
| `ComponentNodeFactoryTests.cs` | `ComponentNode_RenderCount_Increments` rewritten to use `TestHelpers.MountVNode` |

### Game Code (nfm-world)

| File | Change |
|---|---|
| `Mad/Gameplay/XamlTestPhase.cs` | Replaced `_testView.Mount(container)` + `_testView.Update()` with `ReactorDom` + `ComponentNodeFactory.Create(_testView)` |
| `Mad/Gameplay/MainMenuPhase.cs` | Passed event handlers as constructor args to `MainMenuView`; replaced `_mainMenuView.Mount()` + `.Update()` with `ReactorDom` |
| `Mad/Gameplay/GaragePhase.cs` | Added `ReactorDom` + `ComponentNode` fields; replaced `_garageUiView.Update()` with `_garageDom.Mount()` |
| `NFMWorld.Library/.../DefaultHudManager.cs` | Removed unused `Reconciler` field; passed `SynchronizationContext` to `ReactorDom` |

---

## Test Results
**All 87 tests pass** (0 failed, 0 skipped).

---

## Build Status
- `NFMWorld.Reactor.Test` — ✅ Builds and tests pass
- `nfm-world/NFMWorld.csproj` — ⚠️ `MainMenuView.cs` has `CS0103` errors (`vm` and `items` undefined in `Render()`) — this is the user's in-progress rewrite, NOT caused by these changes. All other build errors are resolved.

---

## Key Patterns for Future Work

### Mounting a component in game code:
```csharp
private ReactorDom _dom;
private ComponentNode _cnode;
private FlexPanel _container = new();
private MyComponent _comp = new();

// In constructor/Enter:
_dom = new ReactorDom(SynchronizationContext.Current ?? new SynchronizationContext());
_cnode = ComponentNodeFactory.Create(_comp);
_dom.Mount(_container, _cnode);

// To re-render (e.g., in GameTick):
_dom.Mount(_container, _cnode);
```

### Mounting a component in tests:
```csharp
// For parameterless components:
var (comp, dom) = TestHelpers.MountComponent<MyTestComponent>();

// For components with constructor args:
var (comp, dom) = TestHelpers.MountComponent<MyTestComponent>(arg1, arg2);
```

### Direct reconciler access in tests:
```csharp
var reconciler = TestHelpers.CreateReconciler();
var root = reconciler.Reconcile(vnode, container, null);
```

### Adding InternalsVisibleTo for new test projects:
```xml
<InternalsVisibleTo Include="NewTestProject" />
```
in `NFMWorld.Reactor.csproj`.

---

## Pending / Known Issues
1. `MainMenuView.cs` — in-progress user rewrite; references undefined `vm` and `items`
2. `HStack` — has CS1955 error (user said to ignore for now)
3. `SolidBox` factory not accessible from nfm-world (replaced with FlexPanel in XamlTestView)
4. No `InternalsVisibleTo` for `NFMWorld.Library` — this project should NOT access internal Reactor types; use `ReactorDom` only
