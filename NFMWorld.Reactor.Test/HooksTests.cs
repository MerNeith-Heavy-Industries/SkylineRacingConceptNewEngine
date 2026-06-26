using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using static WorldXaml.UI.Yoga.Nodes;
using WorldXaml.UI.Yoga;

namespace NFMWorld.Reactor.Test;

[TestClass]
public class HooksTests
{
    // ── UseState ────────────────────────────────────────────────────────

    [TestMethod]
    public void UseState_InitialValue()
    {
        var (comp, _, _) = TestHelpers.MountComponent<UseStateComponent>();
        Assert.AreEqual(42, comp.ExposedValue);
    }

    [TestMethod]
    public void UseState_SetterTriggersRerender()
    {
        var (comp, _, ctx) = TestHelpers.MountComponent<UseStateComponent>();
        var initial = comp.RenderCount;
        comp.ExposedSetValue(99);
        ctx.Drain();
        Assert.AreEqual(99, comp.ExposedValue);
        Assert.IsGreaterThan(initial, comp.RenderCount);
    }

    [TestMethod]
    public void UseState_SameValueSkipsRerender()
    {
        var (comp, _, ctx) = TestHelpers.MountComponent<UseStateComponent>();
        var initial = comp.RenderCount;
        comp.ExposedSetValue(42); // same as initial
        ctx.Drain();
        Assert.AreEqual(initial, comp.RenderCount);
    }

    // ── UseEffect ───────────────────────────────────────────────────────

    [TestMethod]
    public void UseEffect_RunsOnMount()
    {
        var (comp, _, _) = TestHelpers.MountComponent<UseEffectComponent>();
        Assert.AreEqual(1, comp.EffectRunCount);
    }

    [TestMethod]
    public void UseEffect_CleanupRunsBeforeNextEffect()
    {
        var (comp, _, ctx) = TestHelpers.MountComponent<UseEffectComponent>();
        comp.ExposedSetCount(1); // change dep → triggers update → cleanup + new effect
        ctx.Drain();
        Assert.AreEqual(1, comp.CleanupRunCount, "Cleanup should run before new effect");
        Assert.AreEqual(2, comp.EffectRunCount, "New effect should run after deps change");
    }

    [TestMethod]
    public void UseEffect_SkipsWhenDepsUnchanged()
    {
        var (comp, _, ctx) = TestHelpers.MountComponent<UseEffectComponent>();
        var after = comp.EffectRunCount;
        comp.Update(); // deps unchanged
        ctx.Drain();
        Assert.AreEqual(after, comp.EffectRunCount);
    }

    [TestMethod]
    public void UseEffect_EmptyDepsRunsOnce()
    {
        var (comp, _, ctx) = TestHelpers.MountComponent<UseEffectOnceComponent>();
        Assert.AreEqual(1, comp.EffectRunCount);
        comp.Update();
        ctx.Drain();
        Assert.AreEqual(1, comp.EffectRunCount, "Empty deps should run only on mount");
    }

    // ── UseMemo ─────────────────────────────────────────────────────────

    [TestMethod]
    public void UseMemo_ComputesValue()
    {
        var (comp, _, _) = TestHelpers.MountComponent<UseMemoComponent>();
        Assert.AreEqual(84, comp.MemoizedValue); // 42 * 2
    }

    [TestMethod]
    public void UseMemo_RecomputesOnDepChange()
    {
        var (comp, _, ctx) = TestHelpers.MountComponent<UseMemoComponent>();
        Assert.AreEqual(84, comp.MemoizedValue);
        comp.ExposedSetBase(10);
        ctx.Drain();
        Assert.AreEqual(20, comp.MemoizedValue);
    }

    [TestMethod]
    public void UseMemo_SkipsWhenDepsUnchanged()
    {
        var (comp, _, ctx) = TestHelpers.MountComponent<UseMemoComponent>();
        var computeCount = comp.MemoComputeCount;
        comp.Update(); // deps unchanged
        ctx.Drain();
        Assert.AreEqual(computeCount, comp.MemoComputeCount);
    }

    // ── UseRef ──────────────────────────────────────────────────────────

    [TestMethod]
    public void UseRef_PersistsAcrossRenders()
    {
        var (comp, _, ctx) = TestHelpers.MountComponent<UseRefComponent>();
        var ref1 = comp.ExposedRef;
        comp.Update();
        ctx.Drain();
        var ref2 = comp.ExposedRef;
        Assert.AreSame(ref1, ref2, "Ref should be the same object across renders");
    }

    [TestMethod]
    public void UseRef_MutationDoesNotRerender()
    {
        var (comp, _, ctx) = TestHelpers.MountComponent<UseRefComponent>();
        var before = comp.RenderCount;
        comp.ExposedRef.Current = "changed";
        ctx.Drain();
        Assert.AreEqual(before, comp.RenderCount, "Ref mutation should not trigger re-render");
    }

    // ── UseCallback ─────────────────────────────────────────────────────

    [TestMethod]
    public void UseCallback_ReturnsStableReference()
    {
        var (comp, _, ctx) = TestHelpers.MountComponent<UseCallbackComponent>();
        var cb1 = comp.ExposedCallback;
        comp.Update(); // deps unchanged
        ctx.Drain();
        var cb2 = comp.ExposedCallback;
        Assert.AreSame(cb1, cb2, "Callback should be stable when deps unchanged");
    }

    // ── UseObservable ───────────────────────────────────────────────────

    [TestMethod]
    public void UseObservable_RerendersOnPropertyChange()
    {
        var vm = new TestViewModel { Name = "Initial" };
        var (comp, _, ctx) = TestHelpers.MountComponent<UseObservableComponent>(vm);

        var before = comp.RenderCount;
        vm.Name = "Changed";
        ctx.Drain();
        Assert.IsGreaterThan(before, comp.RenderCount, "Should re-render on property change");
    }

    // ── UseObservableProperty ───────────────────────────────────────────

    [TestMethod]
    public void UseObservableProperty_RerendersOnlyOnMatchingProperty()
    {
        var vm = new TestViewModel { Name = "A", Age = 1 };
        var (comp, _, ctx) = TestHelpers.MountComponent<UseObservablePropertyComponent>(vm);

        var before = comp.RenderCount;
        vm.Age = 2; // different property
        ctx.Drain();
        Assert.AreEqual(before, comp.RenderCount, "Should NOT re-render on Age change");

        vm.Name = "B"; // watched property
        ctx.Drain();
        Assert.IsGreaterThan(before, comp.RenderCount, "Should re-render on Name change");
    }

    // ── UseCollection ───────────────────────────────────────────────────

    [TestMethod]
    public void UseCollection_RerendersOnAdd()
    {
        var collection = new ObservableCollection<string>();
        var (comp, _, ctx) = TestHelpers.MountComponent<UseCollectionComponent>(collection);

        var before = comp.RenderCount;
        collection.Add("item");
        ctx.Drain();
        Assert.IsGreaterThan(before, comp.RenderCount, "Should re-render on Add");
    }

    [TestMethod]
    public void UseCollection_RerendersOnRemove()
    {
        var collection = new ObservableCollection<string> { "item" };
        var (comp, _, ctx) = TestHelpers.MountComponent<UseCollectionComponent>(collection);

        var before = comp.RenderCount;
        collection.Remove("item");
        ctx.Drain();
        Assert.IsGreaterThan(before, comp.RenderCount, "Should re-render on Remove");
    }

    // ════════════════════════════════════════════════════════════════════
    //  UseEffect — cleanup on unmount
    // ════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void UseEffect_CleanupRunsOnUnmount_ReconcileNull()
    {
        var (comp, dom, ctx) = TestHelpers.MountComponent<UseEffectWithCleanupComponent>();
        Assert.AreEqual(1, comp.EffectRunCount, "Effect should run on mount");
        Assert.AreEqual(0, comp.CleanupRunCount, "Cleanup should not run on mount");

        // Unmount via ReactorDom.Unmount → Reconciler.Reconcile(null, ...)
        dom.Unmount();
        ctx.Drain();
        Assert.AreEqual(1, comp.CleanupRunCount, "Cleanup should run on unmount");
        Assert.IsFalse(comp.IsMounted, "Component should no longer be mounted");
    }

    [TestMethod]
    public void UseEffect_CleanupRunsOnUnmount_ChildRemoved()
    {
        var container = new FlexPanel();
        var (dom, ctx) = TestHelpers.CreateDom();

        // Mount parent with a child that has an effect+cleanup
        var childCNode = ComponentNodeFactory.Create<UseEffectWithCleanupComponent>();
        var parentVNode = FlexPanel(children: childCNode);
        dom.Mount(container, parentVNode);
        ctx.Drain();
        var childNode = parentVNode.Children![0];
        var child = ((ComponentNode)childNode).Instance! as UseEffectWithCleanupComponent;
        Assert.IsNotNull(child);
        Assert.AreEqual(1, child.EffectRunCount);
        Assert.AreEqual(0, child.CleanupRunCount);

        // Re-reconcile parent without the child
        var parentVNode2 = FlexPanel();
        dom.Mount(container, parentVNode2);
        ctx.Drain();
        Assert.AreEqual(1, child.CleanupRunCount, "Cleanup should run when child is removed");
        Assert.IsFalse(child.IsMounted);
    }

    [TestMethod]
    public void UseEffect_CleanupRunsOnUnmount_UpdatePath()
    {
        var (parent, _, ctx) = TestHelpers.MountComponent<ToggleCleanupChildComponent>(true);
        var child = parent.ChildInstance!;
        Assert.IsTrue(child.IsMounted);
        Assert.AreEqual(1, child.EffectRunCount);
        Assert.AreEqual(0, child.CleanupRunCount);

        // Toggle off → child removed → cleanup should run
        parent.ShowChild = false;
        parent.Update();
        ctx.Drain();
        Assert.AreEqual(1, child.CleanupRunCount, "Cleanup should run when child removed via Update()");
        Assert.IsFalse(child.IsMounted);
    }

    [TestMethod]
    public void UseEffect_MultipleCleanupsRunOnUnmount()
    {
        var (comp, dom, ctx) = TestHelpers.MountComponent<MultiEffectComponent>();
        Assert.AreEqual(3, comp.EffectRunCount, "3 effects should run on mount");
        Assert.AreEqual(0, comp.CleanupRunCount);

        dom.Unmount();
        ctx.Drain();
        Assert.AreEqual(3, comp.CleanupRunCount, "All 3 cleanups should run on unmount");
        Assert.IsFalse(comp.IsMounted);
    }

    [TestMethod]
    public void UseEffect_EmptyDepsCleanupRunsOnUnmount()
    {
        var (comp, dom, ctx) = TestHelpers.MountComponent<UseEffectOnceWithCleanupComponent>();
        Assert.AreEqual(1, comp.EffectRunCount);
        Assert.AreEqual(0, comp.CleanupRunCount);

        // Re-render — effect should not re-run (empty deps)
        comp.Update();
        ctx.Drain();
        Assert.AreEqual(1, comp.EffectRunCount, "Empty deps effect should not re-run");
        Assert.AreEqual(0, comp.CleanupRunCount, "Cleanup should not run yet");

        // Unmount — cleanup should run
        dom.Unmount();
        ctx.Drain();
        Assert.AreEqual(1, comp.CleanupRunCount, "Cleanup should run on unmount");
    }

    [TestMethod]
    public void UseEffect_CleanupBetweenRerenders_ThenFinalCleanupOnUnmount()
    {
        var (comp, dom, ctx) = TestHelpers.MountComponent<UseEffectWithCleanupComponent>();
        Assert.AreEqual(1, comp.EffectRunCount);
        Assert.AreEqual(0, comp.CleanupRunCount);

        // Trigger re-render by changing dep → cleanup from first effect runs, then new effect
        comp.ExposedSetCount(1);
        ctx.Drain();
        Assert.AreEqual(1, comp.CleanupRunCount, "Cleanup from first effect should run before second effect");
        Assert.AreEqual(2, comp.EffectRunCount, "Second effect should run");

        // Another re-render
        comp.ExposedSetCount(2);
        ctx.Drain();
        Assert.AreEqual(2, comp.CleanupRunCount, "Cleanup from second effect should run");
        Assert.AreEqual(3, comp.EffectRunCount);

        // Now unmount — cleanup from the last effect should run
        dom.Unmount();
        ctx.Drain();
        Assert.AreEqual(3, comp.CleanupRunCount, "Final cleanup should run on unmount");
        Assert.IsFalse(comp.IsMounted);
    }

    [TestMethod]
    public void UseEffect_PartialDepChange_OnlyRunsCleanupForChangedEffect()
    {
        var (comp, _, ctx) = TestHelpers.MountComponent<DualEffectComponent>();
        Assert.AreEqual(1, comp.EffectARunCount, "Effect A should run on mount");
        Assert.AreEqual(0, comp.CleanupARunCount, "Cleanup A should not run on mount");
        Assert.AreEqual(1, comp.EffectBRunCount, "Effect B should run on mount");
        Assert.AreEqual(0, comp.CleanupBRunCount, "Cleanup B should not run on mount");

        // Change only Effect A's dependency
        comp.ExposedSetCountA(1);
        ctx.Drain();
        Assert.AreEqual(1, comp.CleanupARunCount, "Cleanup A should run before re-running effect A");
        Assert.AreEqual(2, comp.EffectARunCount, "Effect A should re-run");
        Assert.AreEqual(0, comp.CleanupBRunCount, "Cleanup B should NOT run — its deps didn't change");
        Assert.AreEqual(1, comp.EffectBRunCount, "Effect B should NOT re-run — its deps didn't change");
    }

    // ════════════════════════════════════════════════════════════════════
    //  Hook Order Enforcement
    // ════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void HookOrder_SameOrderOnEachRender_Ok()
    {
        var (comp, _, _) = TestHelpers.MountComponent<UseStateComponent>();
        comp.Update(); // same hooks in same order, should not throw
    }

    [TestMethod]
    public void HookOrder_ConditionalHook_Throws()
    {
        var (comp, _, ctx) = TestHelpers.MountComponent<ConditionalHookComponent>(); comp.ShowExtra = false;
        comp.ShowExtra = true;
        try
        {
            comp.Update();
            ctx.Drain();
            Assert.Fail("Expected HookOrderException was not thrown");
        }
        catch (HookOrderException) { /* expected */ }
    }

    [TestMethod]
    public void HookOrder_MissingHook_Throws()
    {
        var (comp, _, ctx) = TestHelpers.MountComponent<SkippingHookComponent>(); comp.Skip = false;
        comp.Skip = true;
        try
        {
            comp.Update();
            ctx.Drain();
            Assert.Fail("Expected HookOrderException was not thrown");
        }
        catch (HookOrderException) { /* expected */ }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Test Components — Hooks
    // ════════════════════════════════════════════════════════════════════

    private class UseStateComponent : Component
    {
        public int RenderCount { get; private set; }
        public int ExposedValue { get; private set; }
        private Action<int>? _setValue;

        public void ExposedSetValue(int v) => _setValue?.Invoke(v);

        protected override VNode Render()
        {
            RenderCount++;
            var (value, setValue) = UseState(42);
            ExposedValue = value;
            _setValue = setValue;
            return FlexPanel();
        }
    }

    private class UseEffectComponent : Component
    {
        public int RenderCount { get; private set; }
        public int EffectRunCount { get; private set; }
        public int CleanupRunCount { get; private set; }
        private (int count, Action<int> setCount) _state;

        public void ExposedSetCount(int c) => _state.setCount(c);

        protected override VNode Render()
        {
            RenderCount++;
            _state = UseState(0);
            UseEffect(() =>
            {
                EffectRunCount++;
                return () => CleanupRunCount++;
            }, [_state.count]);
            return FlexPanel();
        }
    }

    private class UseEffectOnceComponent : Component
    {
        public int EffectRunCount { get; private set; }

        protected override VNode Render()
        {
            UseEffect(() =>
            {
                EffectRunCount++;
                return null;
            }, []); // empty deps → run once
            return FlexPanel();
        }
    }

    private class UseMemoComponent : Component
    {
        private (int value, Action<int> setValue) _base;
        public int MemoComputeCount { get; private set; }
        public int MemoizedValue { get; private set; }

        public void ExposedSetBase(int v) => _base.setValue(v);

        protected override VNode Render()
        {
            _base = UseState(42);
            MemoizedValue = UseMemo(() =>
            {
                MemoComputeCount++;
                return _base.value * 2;
            }, [_base.value]);
            return FlexPanel();
        }
    }

    private class UseRefComponent : Component
    {
        public int RenderCount { get; private set; }
        public Ref<string> ExposedRef { get; private set; } = null!;

        protected override VNode Render()
        {
            RenderCount++;
            ExposedRef = UseRef("initial");
            return FlexPanel();
        }
    }

    private class UseCallbackComponent : Component
    {
        private (int value, Action<int> setValue) _state;
        public Action ExposedCallback { get; private set; } = null!;

        protected override VNode Render()
        {
            _state = UseState(0);
            ExposedCallback = UseCallback(() => Console.WriteLine(_state.value), [_state.value]);
            return FlexPanel();
        }
    }

    private class UseObservableComponent : Component
    {
        private readonly TestViewModel _vm;
        public int RenderCount { get; private set; }

        public UseObservableComponent(TestViewModel vm) => _vm = vm;

        protected override VNode Render()
        {
            RenderCount++;
            UseObservable(_vm);
            return FlexPanel();
        }
    }

    private class UseObservablePropertyComponent : Component
    {
        private readonly TestViewModel _vm;
        public int RenderCount { get; private set; }

        public UseObservablePropertyComponent(TestViewModel vm) => _vm = vm;

        protected override VNode Render()
        {
            RenderCount++;
            UseObservableProperty(_vm, static vm => vm.Name, nameof(TestViewModel.Name));
            return FlexPanel();
        }
    }

    private class UseCollectionComponent : Component
    {
        private readonly ObservableCollection<string> _collection;
        public int RenderCount { get; private set; }

        public UseCollectionComponent(ObservableCollection<string> collection) => _collection = collection;

        protected override VNode Render()
        {
            RenderCount++;
            UseCollection(_collection);
            return FlexPanel();
        }
    }

    private class TestViewModel : INotifyPropertyChanged
    {
        private string _name = "";
        private int _age;

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public int Age
        {
            get => _age;
            set { _age = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // ════════════════════════════════════════════════════════════════════
    //  Test Components — Unmount Cleanup
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Component with a UseEffect that tracks both effect invocations and cleanup invocations.
    /// </summary>
    private class UseEffectWithCleanupComponent : Component
    {
        public int EffectRunCount { get; private set; }
        public int CleanupRunCount { get; private set; }
        private (int count, Action<int> setCount) _state;

        public void ExposedSetCount(int c) => _state.setCount(c);

        protected override VNode Render()
        {
            _state = UseState(0);
            UseEffect(() =>
            {
                EffectRunCount++;
                return () => CleanupRunCount++;
            }, [_state.count]);
            return FlexPanel();
        }
    }

    /// <summary>
    /// Component with an empty-deps UseEffect that returns a cleanup action.
    /// Verifies that the cleanup runs on unmount even for "run once" effects.
    /// </summary>
    private class UseEffectOnceWithCleanupComponent : Component
    {
        public int EffectRunCount { get; private set; }
        public int CleanupRunCount { get; private set; }

        protected override VNode Render()
        {
            UseEffect(() =>
            {
                EffectRunCount++;
                return () => CleanupRunCount++;
            }, []); // empty deps → run once, cleanup on unmount
            return FlexPanel();
        }
    }

    /// <summary>
    /// Component with multiple UseEffect calls, each with a cleanup.
    /// Verifies all cleanups run on unmount.
    /// </summary>
    private class MultiEffectComponent : Component
    {
        public int EffectRunCount { get; private set; }
        public int CleanupRunCount { get; private set; }

        protected override VNode Render()
        {
            UseEffect(() =>
            {
                EffectRunCount++;
                return () => CleanupRunCount++;
            });
            UseEffect(() =>
            {
                EffectRunCount++;
                return () => CleanupRunCount++;
            });
            UseEffect(() =>
            {
                EffectRunCount++;
                return () => CleanupRunCount++;
            });
            return FlexPanel();
        }
    }

    /// <summary>
    /// Component with two independent UseEffect calls with separate deps and cleanup counters.
    /// Verifies that changing one effect's deps doesn't run the other effect's cleanup.
    /// </summary>
    private class DualEffectComponent : Component
    {
        public int EffectARunCount { get; private set; }
        public int CleanupARunCount { get; private set; }
        public int EffectBRunCount { get; private set; }
        public int CleanupBRunCount { get; private set; }
        private (int count, Action<int> setCount) _stateA;
        private (int count, Action<int> setCount) _stateB;

        public void ExposedSetCountA(int c) => _stateA.setCount(c);
        public void ExposedSetCountB(int c) => _stateB.setCount(c);

        protected override VNode Render()
        {
            _stateA = UseState(0);
            _stateB = UseState(0);
            UseEffect(() =>
            {
                EffectARunCount++;
                return () => CleanupARunCount++;
            }, [_stateA.count]);
            UseEffect(() =>
            {
                EffectBRunCount++;
                return () => CleanupBRunCount++;
            }, [_stateB.count]);
            return FlexPanel();
        }
    }

    /// <summary>
    /// Parent component that conditionally renders a UseEffectWithCleanupComponent child.
    /// Uses DisableMemo for deterministic re-renders.
    /// </summary>
    private class ToggleCleanupChildComponent : Component
    {
        public bool ShowChild { get; set; }
        private ComponentNode? _childNode;
        public UseEffectWithCleanupComponent? ChildInstance => _childNode?.Instance as UseEffectWithCleanupComponent;

        public ToggleCleanupChildComponent(bool showChild)
        {
            ShowChild = showChild;
            DisableMemo();
        }

        protected override VNode Render()
        {
            if (ShowChild)
            {
                _childNode = ComponentNodeFactory.Create<UseEffectWithCleanupComponent>();
                return FlexPanel(children: _childNode);
            }

            _childNode = null;
            return FlexPanel();
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Hook Order Violation Test Components
    // ════════════════════════════════════════════════════════════════════

    private class ConditionalHookComponent : Component
    {
        public bool ShowExtra { get; set; }

        protected override VNode Render()
        {
            UseState(1);
            if (ShowExtra)
                UseState("extra"); // conditional — should throw on second render
            UseState(3);
            return FlexPanel();
        }
    }

    private class SkippingHookComponent : Component
    {
        public bool Skip { get; set; }

        protected override VNode Render()
        {
            UseState(1);
            if (!Skip)
                UseState(2);
            // When Skip=true, hook #2 is missing → throws
            UseState(3);
            return FlexPanel();
        }
    }
}