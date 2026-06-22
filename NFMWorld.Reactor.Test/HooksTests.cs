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
        var comp = new UseStateComponent();
        Mount(comp);
        Assert.AreEqual(42, comp.ExposedValue);
    }

    [TestMethod]
    public void UseState_SetterTriggersRerender()
    {
        var comp = new UseStateComponent();
        Mount(comp);
        var initial = comp.RenderCount;
        comp.ExposedSetValue(99);
        Assert.AreEqual(99, comp.ExposedValue);
        Assert.IsGreaterThan(initial, comp.RenderCount);
    }

    [TestMethod]
    public void UseState_SameValueSkipsRerender()
    {
        var comp = new UseStateComponent();
        Mount(comp);
        var initial = comp.RenderCount;
        comp.ExposedSetValue(42); // same as initial
        Assert.AreEqual(initial, comp.RenderCount);
    }

    // ── UseEffect ───────────────────────────────────────────────────────

    [TestMethod]
    public void UseEffect_RunsOnMount()
    {
        var comp = new UseEffectComponent();
        Mount(comp);
        Assert.AreEqual(1, comp.EffectRunCount);
    }

    [TestMethod]
    public void UseEffect_CleanupRunsBeforeNextEffect()
    {
        var comp = new UseEffectComponent();
        Mount(comp);
        comp.ExposedSetCount(1); // change dep → triggers update → cleanup + new effect
        Assert.AreEqual(1, comp.CleanupRunCount, "Cleanup should run before new effect");
        Assert.AreEqual(2, comp.EffectRunCount, "New effect should run after deps change");
    }

    [TestMethod]
    public void UseEffect_SkipsWhenDepsUnchanged()
    {
        var comp = new UseEffectComponent();
        Mount(comp);
        var after = comp.EffectRunCount;
        comp.Update(); // deps unchanged
        Assert.AreEqual(after, comp.EffectRunCount);
    }

    [TestMethod]
    public void UseEffect_EmptyDepsRunsOnce()
    {
        var comp = new UseEffectOnceComponent();
        Mount(comp);
        Assert.AreEqual(1, comp.EffectRunCount);
        comp.Update();
        Assert.AreEqual(1, comp.EffectRunCount, "Empty deps should run only on mount");
    }

    // ── UseMemo ─────────────────────────────────────────────────────────

    [TestMethod]
    public void UseMemo_ComputesValue()
    {
        var comp = new UseMemoComponent();
        Mount(comp);
        Assert.AreEqual(84, comp.MemoizedValue); // 42 * 2
    }

    [TestMethod]
    public void UseMemo_RecomputesOnDepChange()
    {
        var comp = new UseMemoComponent();
        Mount(comp);
        Assert.AreEqual(84, comp.MemoizedValue);
        comp.ExposedSetBase(10);
        Assert.AreEqual(20, comp.MemoizedValue);
    }

    [TestMethod]
    public void UseMemo_SkipsWhenDepsUnchanged()
    {
        var comp = new UseMemoComponent();
        Mount(comp);
        var computeCount = comp.MemoComputeCount;
        comp.Update(); // deps unchanged
        Assert.AreEqual(computeCount, comp.MemoComputeCount);
    }

    // ── UseRef ──────────────────────────────────────────────────────────

    [TestMethod]
    public void UseRef_PersistsAcrossRenders()
    {
        var comp = new UseRefComponent();
        Mount(comp);
        var ref1 = comp.ExposedRef;
        comp.Update();
        var ref2 = comp.ExposedRef;
        Assert.AreSame(ref1, ref2, "Ref should be the same object across renders");
    }

    [TestMethod]
    public void UseRef_MutationDoesNotRerender()
    {
        var comp = new UseRefComponent();
        Mount(comp);
        var before = comp.RenderCount;
        comp.ExposedRef.Current = "changed";
        Assert.AreEqual(before, comp.RenderCount, "Ref mutation should not trigger re-render");
    }

    // ── UseCallback ─────────────────────────────────────────────────────

    [TestMethod]
    public void UseCallback_ReturnsStableReference()
    {
        var comp = new UseCallbackComponent();
        Mount(comp);
        var cb1 = comp.ExposedCallback;
        comp.Update(); // deps unchanged
        var cb2 = comp.ExposedCallback;
        Assert.AreSame(cb1, cb2, "Callback should be stable when deps unchanged");
    }

    // ── UseObservable ───────────────────────────────────────────────────

    [TestMethod]
    public void UseObservable_RerendersOnPropertyChange()
    {
        var vm = new TestViewModel { Name = "Initial" };
        var comp = new UseObservableComponent(vm);
        Mount(comp);

        var before = comp.RenderCount;
        vm.Name = "Changed";
        Assert.IsGreaterThan(before, comp.RenderCount, "Should re-render on property change");
    }

    // ── UseObservableProperty ───────────────────────────────────────────

    [TestMethod]
    public void UseObservableProperty_RerendersOnlyOnMatchingProperty()
    {
        var vm = new TestViewModel { Name = "A", Age = 1 };
        var comp = new UseObservablePropertyComponent(vm);
        Mount(comp);

        var before = comp.RenderCount;
        vm.Age = 2; // different property
        Assert.AreEqual(before, comp.RenderCount, "Should NOT re-render on Age change");

        vm.Name = "B"; // watched property
        Assert.IsGreaterThan(before, comp.RenderCount, "Should re-render on Name change");
    }

    // ── UseCollection ───────────────────────────────────────────────────

    [TestMethod]
    public void UseCollection_RerendersOnAdd()
    {
        var collection = new ObservableCollection<string>();
        var comp = new UseCollectionComponent(collection);
        Mount(comp);

        var before = comp.RenderCount;
        collection.Add("item");
        Assert.IsGreaterThan(before, comp.RenderCount, "Should re-render on Add");
    }

    [TestMethod]
    public void UseCollection_RerendersOnRemove()
    {
        var collection = new ObservableCollection<string> { "item" };
        var comp = new UseCollectionComponent(collection);
        Mount(comp);

        var before = comp.RenderCount;
        collection.Remove("item");
        Assert.IsGreaterThan(before, comp.RenderCount, "Should re-render on Remove");
    }

    // ════════════════════════════════════════════════════════════════════
    //  Test Components
    // ════════════════════════════════════════════════════════════════════

    // ── Hook Order Enforcement ──────────────────────────────────────────

    [TestMethod]
    public void HookOrder_SameOrderOnEachRender_Ok()
    {
        var comp = new UseStateComponent();
        Mount(comp);
        comp.Update(); // same hooks in same order, should not throw
    }

    [TestMethod]
    public void HookOrder_ConditionalHook_Throws()
    {
        var comp = new ConditionalHookComponent { ShowExtra = false };
        Mount(comp);
        comp.ShowExtra = true;
        try
        {
            comp.Update();
            Assert.Fail("Expected HookOrderException was not thrown");
        }
        catch (HookOrderException) { /* expected */ }
    }

    [TestMethod]
    public void HookOrder_MissingHook_Throws()
    {
        var comp = new SkippingHookComponent { Skip = false };
        Mount(comp);
        comp.Skip = true;
        try
        {
            comp.Update();
            Assert.Fail("Expected HookOrderException was not thrown");
        }
        catch (HookOrderException) { /* expected */ }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Helpers
    // ════════════════════════════════════════════════════════════════════

    private static void Mount(Component comp)
    {
        comp.Mount(new FlexPanel());
    }

    // ════════════════════════════════════════════════════════════════════
    //  Test Components
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
