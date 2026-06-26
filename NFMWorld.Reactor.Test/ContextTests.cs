using NFMWorld.Reactor.TestFixtures;
using static WorldXaml.UI.Yoga.Nodes;
using static NFMWorld.Reactor.TestFixtures.Nodes;
using WorldXaml.UI.Yoga;

namespace NFMWorld.Reactor.Test;

[TestClass]
public class ContextTests
{
    // ════════════════════════════════════════════════════════════════════
    //  UseContext — default value
    // ════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void UseContext_ReturnsDefaultValue_WhenNoProvider()
    {
        var ctx = new Context<string>("default-value");
        var (comp, _, _) = TestHelpers.MountComponent<ContextConsumerComponent>(ctx);

        Assert.AreEqual("default-value", comp.LastReadValue);
    }

    /// <summary>
    /// Sanity check that SetContext/GetContext work directly on the Reconciler
    /// without going through components at all.
    /// </summary>
    [TestMethod]
    public void Reconciler_SetAndGetContext_Directly()
    {
        var ctx = new Context<string>("default");
        var container = new FlexPanel();
        var (dom, syncCtx) = TestHelpers.CreateDom();

        var vnode = View(children:
            ContextProviderComponent(context: ctx, value: "direct", child:
                ContextConsumerComponent(context: ctx)
            )
        );
        dom.Mount(container, vnode);
        syncCtx.Drain();

        var viewNative = container.Children[0] as FlexPanel;
        var providerNative = viewNative!.Children[0] as FlexPanel;
        var consumerOutput = providerNative!.Children[0] as FlexPanel;
        Assert.AreEqual("direct", consumerOutput!.Name);
    }

    // ════════════════════════════════════════════════════════════════════
    //  ProvideContext → UseContext through reconciler
    // ════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void ProvideContext_MakesValueAvailableToDescendant()
    {
        var ctx = new Context<string>("default");
        var container = new FlexPanel();
        var (dom, syncCtx) = TestHelpers.CreateDom();

        var vnode = View(children:
            ContextProviderComponent(context: ctx, value: "provided", child:
                ContextConsumerComponent(context: ctx)
            )
        );

        dom.Mount(container, vnode);
        syncCtx.Drain();

        // container → View's FlexPanel → Provider's FlexPanel → Consumer's FlexPanel
        var viewNative = container.Children[0] as FlexPanel;
        Assert.IsNotNull(viewNative, "View should render a FlexPanel");
        var providerNative = viewNative.Children[0] as FlexPanel;
        Assert.IsNotNull(providerNative, "Provider should render a FlexPanel");
        var consumerOutput = providerNative.Children[0] as FlexPanel;
        Assert.IsNotNull(consumerOutput, "Consumer should render a FlexPanel");
        Assert.AreEqual("provided", consumerOutput.Name, "Consumer should use provided context value");
    }

    [TestMethod]
    public void ProvideContext_OverridesParentContext()
    {
        var ctx = new Context<string>("default");
        var container = new FlexPanel();
        var (dom, syncCtx) = TestHelpers.CreateDom();

        var vnode = View(children:
            ContextProviderComponent(context: ctx, value: "outer", child:
                ContextProviderComponent(context: ctx, value: "inner", child:
                    ContextConsumerComponent(context: ctx)
                )
            )
        );

        dom.Mount(container, vnode);
        syncCtx.Drain();

        // container → View's FP → outer FP → inner FP → Consumer's FP
        var viewNative = container.Children[0] as FlexPanel;
        Assert.IsNotNull(viewNative, "View should render a FlexPanel");
        var outerNative = viewNative.Children[0] as FlexPanel;
        Assert.IsNotNull(outerNative, "Outer provider should render a FlexPanel");
        var innerNative = outerNative.Children[0] as FlexPanel;
        Assert.IsNotNull(innerNative, "Inner provider should render a FlexPanel");
        var consumerOutput = innerNative.Children[0] as FlexPanel;
        Assert.IsNotNull(consumerOutput, "Consumer should render a FlexPanel");
        Assert.AreEqual("inner", consumerOutput.Name, "Innermost provider should win");
    }

    // ════════════════════════════════════════════════════════════════════
    //  Context.Version
    // ════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void ContextVersion_IncrementsOnProvide()
    {
        var ctx = new Context<string>("default");
        var initialVersion = ctx.Version;

        var container = new FlexPanel();
        var (dom, syncCtx) = TestHelpers.CreateDom();

        // First reconcile — ProvideContext bumps version
        var vnode1 = View(children:
            ContextProviderComponent(context: ctx, value: "v1", child:
                ContextConsumerComponent(context: ctx)
            )
        );
        dom.Mount(container, vnode1);
        syncCtx.Drain();
        Assert.IsGreaterThan(initialVersion, ctx.Version, "ProvideContext should increment version");

        // Second reconcile with same provider — version increments again
        var versionAfterFirst = ctx.Version;
        var vnode2 = View(children:
            ContextProviderComponent(context: ctx, value: "v2", child:
                ContextConsumerComponent(context: ctx)
            )
        );
        dom.Mount(container, vnode2);
        syncCtx.Drain();
        Assert.IsGreaterThan(versionAfterFirst, ctx.Version, "Version should increment on each ProvideContext");
    }

    [TestMethod]
    public void ContextVersion_DoesNotIncrementWhenNotProvided()
    {
        var ctx = new Context<string>("default");
        var initialVersion = ctx.Version;

        var container = new FlexPanel();
        var (dom, syncCtx) = TestHelpers.CreateDom();

        // Reconcile a tree WITHOUT a provider for this context
        var vnode = View(children:
            ContextConsumerComponent(context: ctx)
        );
        dom.Mount(container, vnode);
        syncCtx.Drain();

        Assert.AreEqual(initialVersion, ctx.Version, "Version should not change when context is not provided");
    }
}