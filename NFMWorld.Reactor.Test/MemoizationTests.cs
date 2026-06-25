using NFMWorld.Reactor.TestFixtures;
using static WorldXaml.UI.Yoga.Nodes;
using static NFMWorld.Reactor.TestFixtures.Nodes;
using WorldXaml.UI.Yoga;

namespace NFMWorld.Reactor.Test;

[TestClass]
public class MemoizationTests
{
    /// <summary>
    /// Verifies that a memoized component reuses the previous VNode
    /// when inputs haven't changed, skipping <see cref="Component.Render"/>.
    /// </summary>
    [TestMethod]
    public void Memo_SkipsRender_WhenInputsUnchanged()
    {
        var container = new FlexPanel();
        var reconciler = new Reconciler();

        var vnode1 = View(children: MemoIdComponent(id: 42));
        var root = reconciler.Reconcile(vnode1, container, null);
        var compNode1 = (MemoIdComponentNode)vnode1.Children![0];
        var comp = (MemoIdComponent)compNode1.Instance!;
        Assert.AreEqual(1, comp.RenderCount);

        var vnode2 = View(children: MemoIdComponent(id: 42));
        reconciler.Reconcile(vnode2, container, root);
        Assert.AreEqual(1, comp.RenderCount, "Should skip Render when inputs unchanged");
    }

    [TestMethod]
    public void Memo_Rerenders_WhenInputsChanged()
    {
        var container = new FlexPanel();
        var reconciler = new Reconciler();

        var vnode1 = View(children: MemoIdComponent(id: 42));
        var root = reconciler.Reconcile(vnode1, container, null);
        var compNode1 = (MemoIdComponentNode)vnode1.Children![0];
        var comp = (MemoIdComponent)compNode1.Instance!;
        Assert.AreEqual(1, comp.RenderCount);

        var vnode2 = View(children: MemoIdComponent(id: 99));
        reconciler.Reconcile(vnode2, container, root);
        // Different inputs → new instance created with new constructor args
        var comp2 = (MemoIdComponent)((MemoIdComponentNode)vnode2.Children![0]).Instance!;
        Assert.AreEqual(1, comp2.RenderCount, "New instance renders once");
        Assert.AreEqual(99, comp2.Id, "New instance should have new input value");
    }

    [TestMethod]
    public void Memo_ValueTypeInput_SameValueSkipsRender()
    {
        var container = new FlexPanel();
        var reconciler = new Reconciler();

        var vnode1 = View(children: MemoIdComponent(id: 7));
        var root = reconciler.Reconcile(vnode1, container, null);
        var compNode1 = (MemoIdComponentNode)vnode1.Children![0];
        var comp = (MemoIdComponent)compNode1.Instance!;
        Assert.AreEqual(1, comp.RenderCount);

        var vnode2 = View(children: MemoIdComponent(id: 7));
        reconciler.Reconcile(vnode2, container, root);
        // Same value (boxed int 7 == 7) → instance reused → memo skips
        Assert.AreEqual(1, comp.RenderCount, "Same value type input should skip render");
    }

    // (Memo_ValueTypeInput test removed — same pattern as Memo_SkipsRender)

    [TestMethod]
    public void DisableMemo_AlwaysRerenders()
    {
        var container = new FlexPanel();
        var reconciler = new Reconciler();

        var vnode1 = View(children: NoMemoIdComponent(id: 1));
        var root = reconciler.Reconcile(vnode1, container, null);
        var compNode1 = (NoMemoIdComponentNode)vnode1.Children![0];
        var comp = (NoMemoIdComponent)compNode1.Instance!;
        Assert.AreEqual(1, comp.RenderCount);

        var vnode2 = View(children: NoMemoIdComponent(id: 1));
        reconciler.Reconcile(vnode2, container, root);

        // Same inputs → instance reused → but memo is disabled, so it re-renders
        Assert.AreEqual(2, comp.RenderCount, "DisableMemo should re-render every time");
    }

    // ════════════════════════════════════════════════════════════════════
    //  Context-driven memo
    // ════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Memo_Rerenders_WhenContextVersionChanges()
    {
        var ctx = new Context<string>("default");
        var container = new FlexPanel();
        var reconciler = new Reconciler();

        var vnode1 = View(children:
            AlwaysRenderProviderComponent(context: ctx, value: "alpha", child:
                MemoContextConsumerComponent(context: ctx)
            )
        );
        var root = reconciler.Reconcile(vnode1, container, null);

        var consumerNode1 = ExtractInnerConsumer(vnode1);
        var consumer = (MemoContextConsumerComponent)consumerNode1.Instance!;
        Assert.AreEqual(1, consumer.RenderCount);
        Assert.AreEqual("alpha", consumer.LastReadValue);

        var vnode2 = View(children:
            AlwaysRenderProviderComponent(context: ctx, value: "alpha", child:
                MemoContextConsumerComponent(context: ctx)
            )
        );
        reconciler.Reconcile(vnode2, container, root);

        Assert.AreEqual(2, consumer.RenderCount,
            "Should re-render when context version changes (even with same value)");
    }

    [TestMethod]
    public void Memo_SkipsRender_WhenContextNotProvided()
    {
        var ctx = new Context<string>("default");
        var container = new FlexPanel();
        var reconciler = new Reconciler();

        var vnode1 = View(children: MemoContextConsumerComponent(context: ctx));
        var root = reconciler.Reconcile(vnode1, container, null);

        var compNode1 = (MemoContextConsumerComponentNode)vnode1.Children![0];
        var comp = (MemoContextConsumerComponent)compNode1.Instance!;
        Assert.AreEqual(1, comp.RenderCount);
        Assert.AreEqual("default", comp.LastReadValue);

        var vnode2 = View(children: MemoContextConsumerComponent(context: ctx));
        reconciler.Reconcile(vnode2, container, root);

        Assert.AreEqual(1, comp.RenderCount,
            "Should skip render when context is not provided and version is unchanged");
    }

    // ════════════════════════════════════════════════════════════════════
    //  Deep memo + deep context
    // ════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void DeepMemo_ContextChangePropagatesThroughMemoSkippedIntermediates()
    {
        var ctx = new Context<string>("default");
        var container = new FlexPanel();
        var reconciler = new Reconciler();

        // Structure: AlwaysRenderProvider → MemoPassthrough (×2) → MemoContextConsumer
        var vnode1 = View(children:
            AlwaysRenderProviderComponent(context: ctx, value: "old", child:
                MemoPassthroughComponent(child:
                    MemoPassthroughComponent(child:
                        MemoContextConsumerComponent(context: ctx)
                    )
                )
            )
        );
        var root = reconciler.Reconcile(vnode1, container, null);

        // Extract the leaf consumer
        var consumer = ExtractDeepConsumer(vnode1);
        Assert.AreEqual(1, consumer.RenderCount, "Consumer should render on mount");
        Assert.AreEqual("old", consumer.LastReadValue);

        // Extract the intermediate passthroughs
        var passthroughs = ExtractPassthroughs(vnode1);
        Assert.AreEqual(1, passthroughs.outer.RenderCount);
        Assert.AreEqual(1, passthroughs.inner.RenderCount);

        // Second render — provider changes context value
        var vnode2 = View(children:
            AlwaysRenderProviderComponent(context: ctx, value: "new", child:
                MemoPassthroughComponent(child:
                    MemoPassthroughComponent(child:
                        MemoContextConsumerComponent(context: ctx)
                    )
                )
            )
        );
        reconciler.Reconcile(vnode2, container, root);

        // Both passthroughs have same inputs → should memo-skip
        Assert.AreEqual(1, passthroughs.outer.RenderCount,
            "Outer passthrough should skip (same inputs)");
        Assert.AreEqual(1, passthroughs.inner.RenderCount,
            "Inner passthrough should skip (same inputs)");

        // Consumer has same inputs BUT context version changed → must re-render
        Assert.AreEqual(2, consumer.RenderCount,
            "Deep consumer should re-render when context changes");
        Assert.AreEqual("new", consumer.LastReadValue,
            "Deep consumer should read the new context value");
    }

    [TestMethod]
    public void DeepMemo_ContextUnchanged_SkipsRender()
    {
        var ctx = new Context<string>("default");
        var container = new FlexPanel();
        var reconciler = new Reconciler();

        var vnode1 = View(children:
            AlwaysRenderProviderComponent(context: ctx, value: "same", child:
                MemoPassthroughComponent(child:
                    MemoContextConsumerComponent(context: ctx)
                )
            )
        );
        var root = reconciler.Reconcile(vnode1, container, null);

        var consumer = ExtractShallowConsumer(vnode1);
        var passthrough = (MemoPassthroughComponent)
            ((MemoPassthroughComponentNode)((AlwaysRenderProviderComponentNode)vnode1.Children![0]).GetInputs()[2]!).Instance!;
        Assert.AreEqual(1, consumer.RenderCount);
        Assert.AreEqual(1, passthrough.RenderCount);
        Assert.AreEqual("same", consumer.LastReadValue);

        // Second render — same provider value (but version still increments)
        var vnode2 = View(children:
            AlwaysRenderProviderComponent(context: ctx, value: "same", child:
                MemoPassthroughComponent(child:
                    MemoContextConsumerComponent(context: ctx)
                )
            )
        );
        reconciler.Reconcile(vnode2, container, root);

        // Passthrough has same inputs → skips
        Assert.AreEqual(1, passthrough.RenderCount,
            "Passthrough should memo-skip");

        // Consumer: same inputs, but context version changed → must re-render.
        // Read from vnode2 since the old VNode tree's Instance reference is still valid
        // (component instances are reused), but verifying via the new tree is cleaner.
        // The consumer instance is reused across renders, so reading from vnode1 also works.
        Assert.AreEqual(2, consumer.RenderCount,
            "Consumer should re-render even when context value is same (version bumped)");
        Assert.AreEqual("same", consumer.LastReadValue,
            "Consumer should still read the correct (same) value");
    }

    // ════════════════════════════════════════════════════════════════════
    //  State bypasses memo
    // ════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Memo_StateUpdateAlwaysRerenders()
    {
        var comp = new MemoStateComponent();
        Mount(comp);

        Assert.AreEqual(1, comp.RenderCount, "Should render on mount");

        comp.ExposedSetValue(99);
        Assert.AreEqual(2, comp.RenderCount, "UseState setter should always re-render, bypassing memo");
    }

    // ════════════════════════════════════════════════════════════════════
    //  Helpers
    // ════════════════════════════════════════════════════════════════════

    private static void Mount(Component comp)
    {
        comp.Mount(new FlexPanel());
    }

    /// <summary>
    /// Walks a provider→child VNode chain to extract the innermost consumer ComponentNode.
    /// </summary>
    private static ComponentNode ExtractInnerConsumer(VNode root)
    {
        // root is a View VNode. Its Children[0] is the Provider ComponentNode.
        var providerNode = (AlwaysRenderProviderComponentNode)((VisualVNode)root).Children![0];
        // The provider's GetInputs()[2] is the child VNode (MemoContextConsumerComponentNode)
        return (MemoContextConsumerComponentNode)providerNode.GetInputs()[2]!;
    }

    /// <summary>
    /// Extracts the leaf MemoContextConsumer from: Provider → Passthrough → Passthrough → Consumer
    /// </summary>
    private static MemoContextConsumerComponent ExtractDeepConsumer(VNode root)
    {
        var providerNode = (AlwaysRenderProviderComponentNode)((VisualVNode)root).Children![0];
        var outerPassNode = (MemoPassthroughComponentNode)providerNode.GetInputs()[2]!;
        var innerPassNode = (MemoPassthroughComponentNode)outerPassNode.GetInputs()[0]!;
        var consumerNode = (MemoContextConsumerComponentNode)innerPassNode.GetInputs()[0]!;
        return (MemoContextConsumerComponent)consumerNode.Instance!;
    }

    /// <summary>
    /// Extracts the MemoContextConsumer from: Provider → Passthrough → Consumer (single passthrough).
    /// </summary>
    private static MemoContextConsumerComponent ExtractShallowConsumer(VNode root)
    {
        var providerNode = (AlwaysRenderProviderComponentNode)((VisualVNode)root).Children![0];
        var passNode = (MemoPassthroughComponentNode)providerNode.GetInputs()[2]!;
        var consumerNode = (MemoContextConsumerComponentNode)passNode.GetInputs()[0]!;
        return (MemoContextConsumerComponent)consumerNode.Instance!;
    }

    /// <summary>
    /// Extracts the two intermediate MemoPassthroughComponents from a deep tree.
    /// </summary>
    private static (MemoPassthroughComponent outer, MemoPassthroughComponent inner)
        ExtractPassthroughs(VNode root)
    {
        var providerNode = (AlwaysRenderProviderComponentNode)((VisualVNode)root).Children![0];
        var outerPassNode = (MemoPassthroughComponentNode)providerNode.GetInputs()[2]!;
        var innerPassNode = (MemoPassthroughComponentNode)outerPassNode.GetInputs()[0]!;
        return ((MemoPassthroughComponent)outerPassNode.Instance!,
                (MemoPassthroughComponent)innerPassNode.Instance!);
    }

    // ════════════════════════════════════════════════════════════════════
    //  Test Components
    // ════════════════════════════════════════════════════════════════════

    private class MemoStateComponent : Component
    {
        private (int value, Action<int> setValue) _state;
        public int RenderCount { get; private set; }
        public int ExposedValue => _state.value;

        public void ExposedSetValue(int v) => _state.setValue(v);

        protected override VNode Render()
        {
            RenderCount++;
            _state = UseState(0);
            return FlexPanel();
        }
    }
}
