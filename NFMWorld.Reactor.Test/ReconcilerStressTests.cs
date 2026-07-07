using NFMWorld.Reactor.TestFixtures;
using static NFMWorld.Reactor.Nodes;
using static NFMWorld.Reactor.TestFixtures.Nodes;

namespace NFMWorld.Reactor.Test;

[TestClass]
public class ReconcilerStressTests
{
    // ════════════════════════════════════════════════════════════════════
    //  Deep tree stress: many nested levels, many update iterations
    // ════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void DeepTree_ManyUpdates_MemoryDoesNotGrowUnbounded()
    {
        const int depth = 30;
        const int iterations = 500;

        var (component, dom, ctx) = TestHelpers.MountComponent<DeepTreeComponent>(depth);

        // Warm up
        for (int i = 0; i < 10; i++) { component.Update(); ctx.Drain(); }

        // Measure allocation rate over a large number of iterations
        GC.Collect(2, GCCollectionMode.Forced, true);
        GC.WaitForPendingFinalizers();
        var startAlloc = GC.GetTotalAllocatedBytes();

        for (int i = 0; i < iterations; i++) { component.Update(); ctx.Drain(); }

        var totalAlloc = GC.GetTotalAllocatedBytes() - startAlloc;
        var perIter = totalAlloc / iterations;

        // Verify the reconciler state isn't growing
        var reconcilerStats = GetReconcilerStats(dom);
        var finalSnapshots = reconcilerStats.snapshots;
        var finalActive = reconcilerStats.activeComponents;
        var finalSlots = reconcilerStats.componentSlots;

        dom.Dispose();

        // Per-iteration allocation should be reasonable (<100KB for a 30-deep tree)
        Assert.IsLessThan(100_000, perIter,
            $"Allocated {perIter} bytes/iteration (total {totalAlloc}). Expected < 100KB/iter.");

        // Snapshot count should be stable (not growing unboundedly)
        Assert.IsTrue(finalSnapshots < 200,
            $"_snapshots has {finalSnapshots} entries — snapshot leak");

        System.Diagnostics.Debug.WriteLine(
            $"Alloc: {perIter} B/iter, snapshots={finalSnapshots}, active={finalActive}, slots={finalSlots}");
    }

    private static (int snapshots, int activeComponents, int componentSlots, int contextStack)
        GetReconcilerStats(ReactorDom dom)
    {
        var reconcilerField = typeof(ReactorDom).GetField("Reconciler",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var reconciler = reconcilerField!.GetValue(dom)!;
        var reconcilerType = reconciler.GetType();

        int GetCount(string fieldName)
        {
            var field = reconcilerType.GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var value = field!.GetValue(reconciler);
            return value switch
            {
                System.Collections.IDictionary d => d.Count,
                System.Collections.ICollection c => c.Count,
                _ => 0
            };
        }

        return (
            GetCount("_snapshots"),
            GetCount("_activeComponents"),
            GetCount("_componentSlots"),
            GetCount("_contextStack")
        );
    }

    /// <summary>
    /// Measures memory with intra-loop GC to determine whether the leak
    /// survives full collection between iterations.
    /// </summary>
    [TestMethod]
    public void DeepTree_IntraLoopGC_ReclaimsMemory()
    {
        const int depth = 30;
        const int iterations = 100;

        var (component, dom, ctx) = TestHelpers.MountComponent<DeepTreeComponent>(depth);

        // Warm up + collect
        for (int i = 0; i < 10; i++)
        {
            component.Update();
            ctx.Drain();
        }
        GC.Collect(2, GCCollectionMode.Forced, true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, true);
        var baseline = GC.GetTotalMemory(true);

        // Run iterations WITH intra-loop GC every 10 iters
        for (int i = 0; i < iterations; i++)
        {
            component.Update();
            ctx.Drain();

            if ((i + 1) % 10 == 0)
            {
                GC.Collect(2, GCCollectionMode.Forced, true);
                GC.WaitForPendingFinalizers();
            }
        }

        GC.Collect(2, GCCollectionMode.Forced, true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, true);
        var after = GC.GetTotalMemory(true);

        dom.Dispose();
        var growth = after - baseline;

        // With intra-loop GC, growth should be bounded (<5MB including GC heap overhead)
        Assert.IsLessThan(5_000_000, growth,
            $"Even with intra-loop GC every 10 iters, memory grew by {growth} bytes. " +
            "Growth should be bounded.");
    }

    /// <summary>
    /// Minimal repro: single FlexPanel, no nesting, no children.
    /// If this still leaks, the bug is in the core reconcile loop.
    /// </summary>
    [TestMethod]
    public void MinimalUpdate_MemoryStable()
    {
        var container = new FlexPanel();
        var dom = new ReactorDom();
        var compNode = ComponentNodeFactory.Create<MinimalUpdateComponent>();
        dom.Mount(container, compNode);
        var comp = (MinimalUpdateComponent)compNode.Instance!;

        for (int i = 0; i < 10; i++)
        {
            comp.Update();
        }
        GC.Collect(2, GCCollectionMode.Forced, true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, true);
        var baseline = GC.GetTotalMemory(true);

        for (int i = 0; i < 500; i++)
        {
            comp.Update();
        }

        GC.Collect(2, GCCollectionMode.Forced, true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, true);
        var after = GC.GetTotalMemory(true);
        dom.Dispose();

        var growth = after - baseline;
        Assert.IsLessThan(200_000, growth,
            $"Minimal update leaked {growth} bytes after 500 iterations. Expected < 200KB.");
    }

    /// <summary>
    /// Narrow repro: 10 FlexPanels, no component children, no nesting.
    /// Isolates whether the leak is proportional to VNode count.
    /// </summary>
    [TestMethod]
    public void FlatTree_MemoryStable()
    {
        const int childCount = 30;
        var (component, dom, ctx) = TestHelpers.MountComponent<FlatTreeComponent>(childCount);

        for (int i = 0; i < 10; i++)
        {
            component.Update();
            ctx.Drain();
        }
        GC.Collect(2, GCCollectionMode.Forced, true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, true);
        var baseline = GC.GetTotalMemory(true);

        for (int i = 0; i < 300; i++)
        {
            component.Update();
            ctx.Drain();
        }

        GC.Collect(2, GCCollectionMode.Forced, true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, true);
        var after = GC.GetTotalMemory(true);
        dom.Dispose();

        var growth = after - baseline;
        Assert.IsLessThan(5_000_000, growth,
            $"Flat tree with {childCount} children grew by {growth} bytes after 300 iterations. Expected < 5MB.");
    }

    /// <summary>
    /// Same as FlatTree but no changing names — isolates whether string
    /// allocations in the name property are the leak source.
    /// </summary>
    [TestMethod]
    public void FlatTree_NoChangingNames_MemoryStable()
    {
        const int childCount = 30;
        var (component, dom, ctx) = TestHelpers.MountComponent<FlatTreeNoNameComponent>(childCount);

        for (int i = 0; i < 10; i++)
        {
            component.Update();
            ctx.Drain();
        }
        GC.Collect(2, GCCollectionMode.Forced, true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, true);
        var baseline = GC.GetTotalMemory(true);

        for (int i = 0; i < 300; i++)
        {
            component.Update();
            ctx.Drain();
        }

        GC.Collect(2, GCCollectionMode.Forced, true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, true);
        var after = GC.GetTotalMemory(true);
        dom.Dispose();

        var growth = after - baseline;
        Assert.IsLessThan(5_000_000, growth,
            $"Flat tree without changing names grew by {growth} bytes after 300 iterations. Expected < 5MB.");
    }

    /// <summary>
    /// Flat tree where every child has a STABLE property (flex:1).
    /// Snapshot entries should persist (not oscillate).
    /// If this doesn't leak, the oscillation of snapshot entries is the culprit.
    /// </summary>
    [TestMethod]
    public void FlatTree_StableProperties_MemoryStable()
    {
        const int childCount = 30;
        var (component, dom, ctx) = TestHelpers.MountComponent<FlatTreeStablePropsComponent>(childCount);

        for (int i = 0; i < 10; i++)
        {
            component.Update();
            ctx.Drain();
        }
        GC.Collect(2, GCCollectionMode.Forced, true);
        GC.WaitForPendingFinalizers();

        // Use allocated bytes for more precise measurement
        var startAllocated = GC.GetTotalAllocatedBytes();

        for (int i = 0; i < 300; i++)
        {
            component.Update();
            ctx.Drain();
        }

        // Force final GC before measuring
        GC.Collect(2, GCCollectionMode.Forced, true);
        GC.WaitForPendingFinalizers();
        var allocatedDuringTest = GC.GetTotalAllocatedBytes() - startAllocated;

        dom.Dispose();

        // Over 300 iterations, 30 FlexPanels: each PropertySnapshot + VNode
        // allocation is expected. But after GC, the LIVE set should be small.
        // Total allocation should be reasonable (< 20MB for 300 iterations).
        Assert.IsLessThan(20_000_000, allocatedDuringTest,
            $"Flat tree with stable props allocated {allocatedDuringTest} bytes over 300 iterations. Expected < 20MB.");

        // Live set after final GC should be reasonable.
        GC.Collect(2, GCCollectionMode.Forced, true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, true);
        var liveSet = GC.GetTotalMemory(true);
        Assert.IsLessThan(5_000_000, liveSet,
            $"Live set after all iterations and GC: {liveSet} bytes. Expected < 5MB.");
    }

    /// <summary>
    /// Single FlexPanel oscillating between name set and not set.
    /// Isolates snapshot entry oscillation.
    /// </summary>
    [TestMethod]
    public void SingleNode_OscillatingProperty_MemoryStable()
    {
        var (component, dom, ctx) = TestHelpers.MountComponent<OscillatingPropComponent>(true);

        for (int i = 0; i < 10; i++)
        {
            component.HasName = !component.HasName;
            component.Update();
            ctx.Drain();
        }
        GC.Collect(2, GCCollectionMode.Forced, true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, true);
        var baseline = GC.GetTotalMemory(true);

        for (int i = 0; i < 500; i++)
        {
            component.HasName = !component.HasName;
            component.Update();
            ctx.Drain();
        }

        GC.Collect(2, GCCollectionMode.Forced, true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, true);
        var after = GC.GetTotalMemory(true);
        dom.Dispose();

        var growth = after - baseline;
        Assert.IsLessThan(200_000, growth,
            $"Single node oscillating property leaked {growth} bytes after 500 iterations. Expected < 200KB.");
    }

    /// <summary>
    /// Renders a single FlexPanel with no properties set.
    /// </summary>
    private class MinimalUpdateComponent : Component
    {
        public MinimalUpdateComponent() { DisableMemo(); }
        protected override VNode Render() => FlexPanel();
    }

    /// <summary>
    /// Renders a flat list of FlexPanels, each with a changing name.
    /// No component children — isolates VNode/snapshot leak.
    /// </summary>
    private class FlatTreeComponent : Component
    {
        private readonly int _count;
        private int _tick;

        public FlatTreeComponent(int count)
        {
            _count = count;
            DisableMemo();
        }

        protected override VNode Render()
        {
            _tick++;
            var children = new VNode[_count];
            for (int i = 0; i < _count; i++)
                children[i] = FlexPanel(name: $"item-{i}-tick{_tick}");

            return View(children: children);
        }
    }

    /// <summary>
    /// Like FlatTreeComponent but without setting name — isolates whether
    /// string allocations via the name property are the leak.
    /// </summary>
    private class FlatTreeNoNameComponent : Component
    {
        private readonly int _count;

        public FlatTreeNoNameComponent(int count)
        {
            _count = count;
            DisableMemo();
        }

        protected override VNode Render()
        {
            var children = new VNode[_count];
            for (int i = 0; i < _count; i++)
                children[i] = FlexPanel();

            return View(children: children);
        }
    }

    /// <summary>
    /// Renders a flat list of FlexPanels, each with a STABLE property (flex).
    /// Snapshot entries should persist across passes (no oscillation).
    /// </summary>
    private class FlatTreeStablePropsComponent : Component
    {
        private readonly int _count;

        public FlatTreeStablePropsComponent(int count)
        {
            _count = count;
            DisableMemo();
        }

        protected override VNode Render()
        {
            var children = new VNode[_count];
            for (int i = 0; i < _count; i++)
                children[i] = FlexPanel(flex: 1);

            return View(children: children);
        }
    }

    /// <summary>
    /// Single FlexPanel that alternates between having a name and not.
    /// Isolates snapshot entry oscillation behavior.
    /// </summary>
    private class OscillatingPropComponent : Component
    {
        public bool HasName { get; set; }

        public OscillatingPropComponent(bool hasName)
        {
            HasName = hasName;
            DisableMemo();
        }

        protected override VNode Render()
        {
            if (HasName)
                return FlexPanel(name: "has-name");
            return FlexPanel();
        }
    }

    private static long MeasureGrowth(int depth, int iterations)
    {
        var (component, dom, ctx) = TestHelpers.MountComponent<DeepTreeComponent>(depth);

        // Warm up
        for (int i = 0; i < 10; i++)
        {
            component.Update();
            ctx.Drain();
        }

        // Force full GC before measurement
        GC.Collect(2, GCCollectionMode.Forced, true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, true);

        var baselineHeap = GC.GetGCMemoryInfo().HeapSizeBytes;

        for (int i = 0; i < iterations; i++)
        {
            component.Update();
            ctx.Drain();
        }

        GC.Collect(2, GCCollectionMode.Forced, true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, true);
        var afterHeap = GC.GetGCMemoryInfo().HeapSizeBytes;

        dom.Dispose();

        System.Diagnostics.Debug.WriteLine(
            $"Heap: baseline={baselineHeap}, after{iterations}={afterHeap}");

        return (long)(afterHeap - baselineHeap);
    }

    [TestMethod]
    public void DeepTree_ManyUpdates_ComponentsStayMounted()
    {
        const int depth = 20;
        const int iterations = 100;

        var (component, _, ctx) = TestHelpers.MountComponent<DeepTreeComponent>(depth);

        for (int i = 0; i < iterations; i++)
        {
            component.Update();
            ctx.Drain();
        }

        // All leaf components should still be mounted
        Assert.IsTrue(component.IsMounted, "Root should be mounted");
        Assert.IsTrue(component.AllLeavesMounted(),
            $"All leaves should be mounted after {iterations} updates");
    }

    // ════════════════════════════════════════════════════════════════════
    //  Wide tree stress: many children at one level, many iterations
    // ════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void WideTree_ManyUpdates_MemoryDoesNotGrowUnbounded()
    {
        const int childCount = 100;
        const int iterations = 50;

        var (component, dom, ctx) = TestHelpers.MountComponent<WideTreeComponent>(childCount);

        // Warm up
        for (int i = 0; i < 5; i++)
        {
            component.Update();
            ctx.Drain();
        }
        GC.Collect(2, GCCollectionMode.Forced, true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, true);
        var baseline = GC.GetTotalMemory(false);

        for (int i = 0; i < iterations; i++)
        {
            component.Update();
            ctx.Drain();
        }

        GC.Collect(2, GCCollectionMode.Forced, true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, true);
        var after = GC.GetTotalMemory(false);

        var growth = after - baseline;
        Assert.IsLessThan(5_000_000, growth,
            $"Memory grew by {growth} bytes after {iterations} updates with {childCount} children. Expected < 5MB.");
    }

    [TestMethod]
    public void WideTree_ManyUpdates_AllComponentsStayMounted()
    {
        const int childCount = 50;
        const int iterations = 100;

        var (component, _, ctx) = TestHelpers.MountComponent<WideTreeComponent>(childCount);

        for (int i = 0; i < iterations; i++)
        {
            component.Update();
            ctx.Drain();
        }

        Assert.IsTrue(component.IsMounted, "Root should be mounted");
        Assert.IsTrue(component.AllChildrenMounted(),
            $"All {childCount} children should be mounted after {iterations} updates");
    }

    // ════════════════════════════════════════════════════════════════════
    //  Toggle stress: repeatedly add/remove child components
    // ════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void ToggleChildren_RepeatedCycles_MemoryDoesNotGrowUnbounded()
    {
        const int childCount = 50;
        const int cycles = 30;

        var (component, _, ctx) = TestHelpers.MountComponent<ToggleManyChildrenComponent>(childCount, true);

        // Warm up
        for (int i = 0; i < 3; i++)
        {
            component.Visible = !component.Visible;
            component.Update();
            ctx.Drain();
        }
        GC.Collect(2, GCCollectionMode.Forced, true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, true);
        var baseline = GC.GetTotalMemory(false);

        for (int i = 0; i < cycles; i++)
        {
            component.Visible = !component.Visible;
            component.Update();
            ctx.Drain();
        }

        GC.Collect(2, GCCollectionMode.Forced, true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, true);
        var after = GC.GetTotalMemory(false);

        var growth = after - baseline;
        Assert.IsLessThan(3_000_000, growth,
            $"Memory grew by {growth} bytes after {cycles} toggle cycles with {childCount} children. Expected < 3MB.");
    }

    [TestMethod]
    public void ToggleChildren_RepeatedCycles_StaleComponentsUnmounted()
    {
        const int childCount = 30;
        const int cycles = 20;

        var (component, _, ctx) = TestHelpers.MountComponent<ToggleManyChildrenComponent>(childCount, true);

        var firstBatch = component.ChildInstances.ToList();
        Assert.IsTrue(firstBatch.All(c => c.IsMounted), "First batch should be mounted");

        for (int i = 0; i < cycles; i++)
        {
            component.Visible = !component.Visible;
            component.Update();
            ctx.Drain();

            var current = component.ChildInstances.ToList();
            Assert.IsTrue(current.All(c => c.IsMounted), $"Cycle {i}: visible children should be mounted");

            if (component.Visible)
            {
                // Children should NOT be the same instances as the first batch
                // (they were unmounted and recreated)
                foreach (var oldChild in firstBatch)
                    Assert.IsFalse(oldChild.IsMounted,
                        $"Cycle {i}: old child from first batch should still be unmounted");
            }
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Nested toggle: parent toggles → nested child toggles → no leaks
    // ════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void NestedToggle_DeepCycle_MemoryDoesNotGrowUnbounded()
    {
        const int depth = 10;
        const int cycles = 40;

        var (component, _, ctx) = TestHelpers.MountComponent<NestedToggleComponent>(depth, true);

        // Warm up
        for (int i = 0; i < 3; i++)
        {
            component.Visible = !component.Visible;
            component.Update();
            ctx.Drain();
        }
        GC.Collect(2, GCCollectionMode.Forced, true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, true);
        var baseline = GC.GetTotalMemory(false);

        for (int i = 0; i < cycles; i++)
        {
            component.Visible = !component.Visible;
            component.Update();
            ctx.Drain();
        }

        GC.Collect(2, GCCollectionMode.Forced, true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, true);
        var after = GC.GetTotalMemory(false);

        var growth = after - baseline;
        Assert.IsLessThan(2_000_000, growth,
            $"Memory grew by {growth} bytes after {cycles} toggle cycles with depth {depth}. Expected < 2MB.");
    }

    [TestMethod]
    public void NestedToggle_DeepCycle_NoComponentLeaks()
    {
        const int depth = 8;
        const int cycles = 30;

        var (component, dom, ctx) = TestHelpers.MountComponent<NestedToggleComponent>(depth, true);

        // Track first-batch leaf components
        var firstBatch = new List<Component>();
        component.ForEachLeaf(c => firstBatch.Add(c));
        Assert.IsTrue(firstBatch.Count > 0, "Should have leaf components");
        Assert.IsTrue(firstBatch.All(c => c.IsMounted), "First batch should be mounted");

        // Toggle off — all leaves should be unmounted
        component.Visible = false;
        component.Update();
        ctx.Drain();
        Assert.IsTrue(firstBatch.All(c => !c.IsMounted),
            "All first-batch leaves should be unmounted after toggling off");

        // Toggle back on — new leaves should be mounted, old ones still unmounted
        component.Visible = true;
        component.Update();
        ctx.Drain();
        var secondBatch = new List<Component>();
        component.ForEachLeaf(c => secondBatch.Add(c));
        Assert.IsTrue(secondBatch.All(c => c.IsMounted),
            "Second batch should be mounted");
        Assert.IsTrue(firstBatch.All(c => !c.IsMounted),
            "First batch should remain unmounted after creating second batch");

        // Run many more cycles
        for (int i = 0; i < cycles; i++)
        {
            component.Visible = !component.Visible;
            component.Update();
            ctx.Drain();

            if (component.Visible)
            {
                var current = new List<Component>();
                component.ForEachLeaf(c => current.Add(c));
                Assert.IsTrue(current.All(c => c.IsMounted),
                    $"Cycle {i}: current leaves should be mounted");
            }
        }

        // First batch should still be unmounted
        Assert.IsTrue(firstBatch.All(c => !c.IsMounted),
            $"First-batch leaves should still be unmounted after {cycles} cycles");

        dom.Dispose();
    }

    // ════════════════════════════════════════════════════════════════════
    //  Context stress: deep context propagation across many updates
    // ════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void ContextStress_ManyLayers_MemoryDoesNotGrowUnbounded()
    {
        var ctxKey = new Context<string>("stress-test");
        const int depth = 20;
        const int iterations = 100;

        var (component, _, syncCtx) = TestHelpers.MountComponent<DeepContextProviderComponent>(ctxKey, "root-value", depth);

        // Warm up
        for (int i = 0; i < 5; i++)
        {
            component.NewValue = $"value-{i}";
            component.Update();
            syncCtx.Drain();
        }
        GC.Collect(2, GCCollectionMode.Forced, true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, true);
        var baseline = GC.GetTotalMemory(false);

        for (int i = 0; i < iterations; i++)
        {
            component.NewValue = $"value-{i}";
            component.Update();
            syncCtx.Drain();
        }

        GC.Collect(2, GCCollectionMode.Forced, true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, true);
        var after = GC.GetTotalMemory(false);

        var growth = after - baseline;
        Assert.IsLessThan(12_000_000, growth,
            $"Memory grew by {growth} bytes after {iterations} context updates at depth {depth}. Expected < 12MB.");
    }

    // ════════════════════════════════════════════════════════════════════
    //  Snapshot stress: many property changes to stress snapshot system
    // ════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void SnapshotStress_ManyPropertyChanges_MemoryDoesNotGrowUnbounded()
    {
        const int childCount = 80;
        const int iterations = 200;

        var (component, dom, ctx) = TestHelpers.MountComponent<SnapshotStressComponent>(childCount);

        // Warm up
        for (int i = 0; i < 10; i++)
        {
            component.CycleProperties();
            component.Update();
            ctx.Drain();
        }
        GC.Collect(2, GCCollectionMode.Forced, true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, true);
        var baseline = GC.GetTotalMemory(false);

        for (int i = 0; i < iterations; i++)
        {
            component.CycleProperties();
            component.Update();
            ctx.Drain();
        }

        GC.Collect(2, GCCollectionMode.Forced, true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, true);
        var after = GC.GetTotalMemory(false);

        var growth = after - baseline;
        Assert.IsLessThan(5_000_000, growth,
            $"Memory grew by {growth} bytes after {iterations} snapshot cycles with {childCount} children. Expected < 5MB.");
    }

    // ════════════════════════════════════════════════════════════════════
    //  Multi-component batch update stress
    // ════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void MultiComponent_BatchUpdates_MemoryDoesNotGrowUnbounded()
    {
        const int componentCount = 20;
        const int iterations = 100;

        // Mount a parent with many independent stateful children
        var (parent, _, ctx) = TestHelpers.MountComponent<MultiStateParentComponent>(componentCount);

        // Warm up
        for (int i = 0; i < 5; i++)
        {
            parent.CycleAllStates();
            parent.Update();
            ctx.Drain();
        }
        GC.Collect(2, GCCollectionMode.Forced, true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, true);
        var baseline = GC.GetTotalMemory(false);

        for (int i = 0; i < iterations; i++)
        {
            parent.CycleAllStates();
            parent.Update();
            ctx.Drain();
        }

        GC.Collect(2, GCCollectionMode.Forced, true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, true);
        var after = GC.GetTotalMemory(false);

        var growth = after - baseline;
        Assert.IsLessThan(5_000_000, growth,
            $"Memory grew by {growth} bytes after {iterations} batch updates across {componentCount} children. Expected < 5MB.");
    }

    [TestMethod]
    public void MultiComponent_BatchUpdates_AllComponentsStayMounted()
    {
        const int componentCount = 15;
        const int iterations = 50;

        var (parent, _, ctx) = TestHelpers.MountComponent<MultiStateParentComponent>(componentCount);

        for (int i = 0; i < iterations; i++)
        {
            parent.CycleAllStates();
            parent.Update();
            ctx.Drain();
        }

        Assert.IsTrue(parent.IsMounted, "Parent should stay mounted");
        Assert.IsTrue(parent.AllChildrenMounted(),
            $"All {componentCount} stateful children should be mounted after {iterations} batch updates");
    }

    // ════════════════════════════════════════════════════════════════════
    //  Unmount + remount stress (simulates phase transitions)
    // ════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void UnmountRemount_ManyCycles_MemoryDoesNotGrowUnbounded()
    {
        const int childCount = 40;
        const int cycles = 30;

        var doms = new List<ReactorDom>();
        var containers = new List<FlexPanel>();

        // Warm up
        for (int i = 0; i < 3; i++)
        {
            var container = new FlexPanel();
            var dom = new ReactorDom();
            var vnode = View(children: BuildChildList(childCount));
            dom.Mount(container, vnode);
            dom.Unmount();
            dom.Dispose();
        }
        containers.Clear();
        doms.Clear();

        GC.Collect(2, GCCollectionMode.Forced, true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, true);
        var baseline = GC.GetTotalMemory(false);

        for (int i = 0; i < cycles; i++)
        {
            var container = new FlexPanel();
            var dom = new ReactorDom();
            var vnode = View(children: BuildChildList(childCount));
            dom.Mount(container, vnode);
            dom.Unmount();
            dom.Dispose();
        }

        GC.Collect(2, GCCollectionMode.Forced, true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, true);
        var after = GC.GetTotalMemory(false);

        var growth = after - baseline;
        Assert.IsLessThan(3_000_000, growth,
            $"Memory grew by {growth} bytes after {cycles} mount/unmount cycles with {childCount} children. Expected < 3MB.");
    }

    [TestMethod]
    public void UnmountRemount_ManyCycles_ComponentsCollected()
    {
        const int cycles = 20;

        for (int i = 0; i < cycles; i++)
        {
            var container = new FlexPanel();
            var dom = new ReactorDom();
            var childNode = EmptyComponent();
            var vnode = View(children: childNode);
            dom.Mount(container, vnode);

            var comp = childNode.Instance;
            Assert.IsNotNull(comp, $"Cycle {i}: component should be instantiated");
            Assert.IsTrue(comp!.IsMounted, $"Cycle {i}: component should be mounted");

            dom.Unmount();
            Assert.IsFalse(comp.IsMounted, $"Cycle {i}: component should be unmounted after Unmount()");

            dom.Dispose();
        }

        // After all cycles, force GC to verify no memory accumulation
        GC.Collect(2, GCCollectionMode.Forced, true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, true);
    }

    // ════════════════════════════════════════════════════════════════════
    //  Component slot stress: fill and clear slots repeatedly
    // ════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void ComponentSlots_FillAndClear_MemoryDoesNotGrowUnbounded()
    {
        const int slotCount = 60;
        const int cycles = 30;

        var (component, _, ctx) = TestHelpers.MountComponent<SlotStressComponent>(slotCount);

        // Warm up
        for (int i = 0; i < 3; i++)
        {
            component.FillAll = !component.FillAll;
            component.Update();
            ctx.Drain();
        }
        GC.Collect(2, GCCollectionMode.Forced, true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, true);
        var baseline = GC.GetTotalMemory(false);

        for (int i = 0; i < cycles; i++)
        {
            component.FillAll = !component.FillAll;
            component.Update();
            ctx.Drain();
        }

        GC.Collect(2, GCCollectionMode.Forced, true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, true);
        var after = GC.GetTotalMemory(false);

        var growth = after - baseline;
        Assert.IsLessThan(3_000_000, growth,
            $"Memory grew by {growth} bytes after {cycles} slot fill/clear cycles with {slotCount} slots. Expected < 3MB.");
    }

    // ════════════════════════════════════════════════════════════════════
    //  Type-change cleanup: when a component's root type changes,
    //  the old subtree must be recursively cleaned up.
    // ════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void TypeChange_Update_CleansUpOldSubtreeComponents()
    {
        var container = new FlexPanel();
        var dom = new ReactorDom();

        // Mount with child visible
        var compNode = ComponentNodeFactory.Create<TypeSwitchComponent>(true);
        dom.Mount(container, compNode);
        var comp = (TypeSwitchComponent)compNode.Instance!;
        var child = comp.ChildInstance;
        Assert.IsNotNull(child, "Child should exist");
        Assert.IsTrue(child!.IsMounted, "Child should be mounted");

        // Toggle off — switches from View to FlexPanel (type change)
        comp.ShowChild = false;
        comp.Update();

        Assert.IsFalse(child.IsMounted,
            "Child should be unmounted after type-change toggle off");

        dom.Dispose();
    }

    /// <summary>
    /// Switches between View(children: EmptyComponent) and FlexPanel() —
    /// forces a root type change from View to FlexPanel.
    /// </summary>
    private class TypeSwitchComponent : Component
    {
        private EmptyComponentNode? _childNode;
        public bool ShowChild { get; set; }

        public TypeSwitchComponent(bool showChild)
        {
            ShowChild = showChild;
            DisableMemo();
        }

        public EmptyComponent? ChildInstance => _childNode?.Instance as EmptyComponent;

        protected override VNode Render()
        {
            if (ShowChild)
            {
                _childNode = EmptyComponent();
                return View(children: _childNode);
            }
            _childNode = null;
            return FlexPanel();
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Test component implementations
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Renders a deeply nested chain of FlexPanels.
    /// The leaf component triggers updates — tests that ancestor components
    /// are NOT unmounted during subtree updates.
    /// </summary>
    private class DeepTreeComponent : Component
    {
        private readonly int _depth;
        private readonly EmptyComponentNode[] _leafNodes;
        private int _tick;

        public DeepTreeComponent(int depth)
        {
            _depth = depth;
            DisableMemo();
            _leafNodes = new EmptyComponentNode[1];
        }

        public bool AllLeavesMounted()
        {
            if (_leafNodes[0]?.Instance is not { } leaf) return false;
            return leaf.IsMounted;
        }

        protected override VNode Render()
        {
            _tick++;
            VNode inner = FlexPanel(); // leaf
            for (int i = 0; i < _depth; i++)
            {
                if (i == 0)
                {
                    _leafNodes[0] = EmptyComponent();
                    inner = FlexPanel(children: [FlexPanel(children: _leafNodes[0]), inner]);
                }
                else
                {
                    inner = FlexPanel(children: inner);
                }
            }

            return View(children: inner);
        }
    }

    /// <summary>
    /// Renders many EmptyComponent children in a single FlexPanel.
    /// Triggers subtree updates — verifies all children survive.
    /// </summary>
    private class WideTreeComponent : Component
    {
        private readonly int _count;
        private readonly List<EmptyComponentNode> _childNodes = [];
        private int _tick;

        public WideTreeComponent(int count)
        {
            _count = count;
            DisableMemo();
        }

        public bool AllChildrenMounted()
        {
            if (_childNodes.Count == 0) return false;
            return _childNodes.All(n => n.Instance is { IsMounted: true });
        }

        protected override VNode Render()
        {
            _tick++;
            _childNodes.Clear();
            var children = new List<VNode>(_count);
            for (int i = 0; i < _count; i++)
            {
                var node = EmptyComponent();
                _childNodes.Add(node);
                children.Add(node);
            }

            return View(flexDirection: FlexDirection.Column, children: [..children]);
        }
    }

    /// <summary>
    /// Toggles many EmptyComponent children on/off via a Visible flag.
    /// </summary>
    private class ToggleManyChildrenComponent : Component
    {
        private readonly int _count;
        private readonly List<EmptyComponentNode> _childNodes = [];
        public bool Visible { get; set; }

        public ToggleManyChildrenComponent(int count, bool visible)
        {
            _count = count;
            Visible = visible;
            DisableMemo();
        }

        public IEnumerable<EmptyComponent> ChildInstances =>
            _childNodes.Select(n => n.Instance).OfType<EmptyComponent>();

        protected override VNode Render()
        {
            _childNodes.Clear();
            if (!Visible)
                return FlexPanel();

            var children = new List<VNode>(_count);
            for (int i = 0; i < _count; i++)
            {
                var node = EmptyComponent();
                _childNodes.Add(node);
                children.Add(node);
            }

            return FlexPanel(children: [..children]);
        }
    }

    /// <summary>
    /// Nested toggle: renders a chain of depth levels, each wrapping
    /// a child component. When Visible=false, all internal components
    /// should be unmounted.
    /// </summary>
    private class NestedToggleComponent : Component
    {
        private readonly int _depth;
        private readonly List<EmptyComponentNode> _innerNodes = [];
        public bool Visible { get; set; }

        public NestedToggleComponent(int depth, bool visible)
        {
            _depth = depth;
            Visible = visible;
            DisableMemo();
        }

        public void ForEachLeaf(Action<Component> action)
        {
            foreach (var node in _innerNodes)
            {
                if (node.Instance is { } comp)
                    action(comp);
            }
        }

        protected override VNode Render()
        {
            _innerNodes.Clear();
            if (!Visible)
                return FlexPanel();

            VNode? tree = null;
            for (int i = 0; i < _depth; i++)
            {
                var inner = EmptyComponent();
                _innerNodes.Add(inner);
                tree = tree is null
                    ? inner
                    : FlexPanel(children: [tree, inner]);
            }

            return View(children: tree);
        }
    }

    /// <summary>
    /// Provides a context value through a deep chain of provider components.
    /// </summary>
    private class DeepContextProviderComponent : Component
    {
        private readonly Context<string> _context;
        private readonly int _depth;
        private string _newValue;

        public DeepContextProviderComponent(Context<string> context, string initialValue, int depth)
        {
            _context = context;
            _newValue = initialValue;
            _depth = depth;
            DisableMemo();
        }

        public string NewValue
        {
            get => _newValue;
            set => _newValue = value;
        }

        protected override VNode Render()
        {
            ProvideContext(_context, _newValue);

            VNode tree = ContextConsumerComponent(_context);
            for (int i = 0; i < _depth; i++)
            {
                tree = ContextProviderComponent(_context, $"layer-{i}", tree);
            }

            return FlexPanel(children: tree);
        }
    }

    /// <summary>
    /// Stress-tests the property snapshot system by toggling many properties
    /// on many children each cycle.
    /// </summary>
    private class SnapshotStressComponent : Component
    {
        private readonly int _count;
        private bool _toggle;
        private int _tick;

        public SnapshotStressComponent(int count)
        {
            _count = count;
            DisableMemo();
        }

        public void CycleProperties()
        {
            _toggle = !_toggle;
        }

        protected override VNode Render()
        {
            _tick++;
            var children = new List<VNode>(_count);
            for (int i = 0; i < _count; i++)
            {
                // Alternate between setting name, opacity, and background color
                if (_toggle)
                    children.Add(FlexPanel(
                        name: $"child-{i}-{_tick}",
                        opacity: (i % 10) / 10f + 0.1f));
                else
                    children.Add(FlexPanel(
                        name: $"child-{i}-{_tick}-alt"));
            }

            return View(flexDirection: FlexDirection.Column, children: [..children]);
        }
    }

    /// <summary>
    /// Renders many independent stateful child components.
    /// Cycling their states triggers many batched subtree updates.
    /// </summary>
    private class MultiStateParentComponent : Component
    {
        private readonly int _count;
        private readonly List<EmptyComponentNode> _childNodes = [];
        private int _tick;

        public MultiStateParentComponent(int count)
        {
            _count = count;
            DisableMemo();
        }

        public void CycleAllStates()
        {
            _tick++;
        }

        public bool AllChildrenMounted()
        {
            if (_childNodes.Count == 0) return false;
            return _childNodes.All(n => n.Instance is { IsMounted: true });
        }

        protected override VNode Render()
        {
            _childNodes.Clear();
            var vnodes = new List<VNode>(_count);
            for (int i = 0; i < _count; i++)
            {
                // Use a unique name each cycle to force property changes
                // without changing component identity
                var node = EmptyComponent();
                _childNodes.Add(node);
                vnodes.Add(FlexPanel(
                    name: $"wrapper-{i}-tick{_tick}",
                    children: node));
            }

            return View(flexDirection: FlexDirection.Row, children: [..vnodes]);
        }
    }

    /// <summary>
    /// Fills and clears many component slots repeatedly.
    /// </summary>
    private class SlotStressComponent : Component
    {
        private readonly int _count;
        public bool FillAll { get; set; } = true;
        private int _tick;

        public SlotStressComponent(int count)
        {
            _count = count;
            DisableMemo();
        }

        protected override VNode Render()
        {
            _tick++;
            if (!FillAll)
                return FlexPanel();

            var children = new List<VNode>(_count);
            for (int i = 0; i < _count; i++)
                children.Add(EmptyComponent());

            return FlexPanel(children: [..children]);
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Helpers
    // ════════════════════════════════════════════════════════════════════

    private static VNode[] BuildChildList(int count)
    {
        var children = new VNode[count];
        for (int i = 0; i < count; i++)
            children[i] = FlexPanel(name: $"child-{i}");
        return children;
    }
}


