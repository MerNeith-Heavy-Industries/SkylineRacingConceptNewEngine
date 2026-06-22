using NFMWorld.Reactor.TestFixtures;
using WorldXaml.UI.Yoga;

namespace NFMWorld.Reactor.Test;

[TestClass]
public class ReconcilerComponentTests
{
    /// <summary>
    /// Verifies that a ComponentNode in a VNode tree is reconciled to a native node.
    /// </summary>
    [TestMethod]
    public void Reconcile_ComponentNode_RendersIntoContainer()
    {
        var container = new FlexPanel();
        var reconciler = new Reconciler();

        var vnode = ViewNodeFactories.View(children:
            EmptyComponentComponentFactories.EmptyComponent()
        );

        var root = reconciler.Reconcile(vnode, container, null);

        Assert.IsNotNull(root);
        Assert.AreEqual(1, container.Children.Count);
    }

    /// <summary>
    /// Direct: Component's Render output is a FlexPanelNode with name.
    /// </summary>
    [TestMethod]
    public void Reconcile_ComponentRendersNamedNode_NameApplied()
    {
        var container = new FlexPanel();
        var reconciler = new Reconciler();
        
        // Step 1: verify the factory-produced VNode has Name
        var compNode = TitleComponentComponentFactories.TitleComponent(title: "CompName");
        var comp = (TitleComponent)compNode.CreateComponent();
        var rendered = (VNode)comp.GetType().GetMethod("Render", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            !.Invoke(comp, null)!;
        Assert.AreEqual("CompName", ((BindableObjectVNode)rendered).Name, "VNode name should be set after Render");
        
        // Step 2: reconcile directly (should work)
        var directResult = reconciler.Reconcile(rendered, new FlexPanel(), null);
        Assert.AreEqual("CompName", directResult.Name, "Direct reconcile should preserve name");
        
        // Step 3: reconcile through View wrapper
        var vnode = ViewNodeFactories.View(children: compNode);
        var root = reconciler.Reconcile(vnode, container, null);
        Assert.IsNotNull(root);
        var flexChild = (container.Children[0] as FlexPanel)?.Children[0] as FlexPanel;
        Assert.IsNotNull(flexChild);
        Assert.AreEqual("CompName", flexChild.Name, "Wrapped reconcile should preserve name");
    }

    /// <summary>
    /// Direct: Component Node reconciled directly (no wrapping View).
    /// </summary>
    [TestMethod]
    public void Reconcile_ComponentNodeDirect_NameApplied()
    {
        var container = new FlexPanel();
        var reconciler = new Reconciler();
        var compNode = TitleComponentComponentFactories.TitleComponent(title: "DirectComp");
        var root = reconciler.Reconcile(compNode, container, null);

        Assert.IsNotNull(root);
        Assert.AreEqual("DirectComp", root.Name);
    }

    /// <summary>
    /// Direct test: FlexPanelNode(name:"x") → native node gets Name set.
    /// </summary>
    [TestMethod]
    public void Reconcile_DirectName_Applied()
    {
        var container = new FlexPanel();
        var reconciler = new Reconciler();
        var flexNode = FlexPanelNodeFactories.FlexPanel(name: "DirectName");
        var root = reconciler.Reconcile(flexNode, container, null);
        Assert.AreEqual("DirectName", root.Name);
    }

    /// <summary>
    /// Verifies that a component's Render() output becomes the native child.
    /// Since EmptyComponent renders a FlexPanel, the container should contain a FlexPanel.
    /// </summary>
    [TestMethod]
    public void Reconcile_EmptyComponent_RendersFlexPanel()
    {
        var container = new FlexPanel();
        var reconciler = new Reconciler();

        var vnode = ViewNodeFactories.View(children:
            EmptyComponentComponentFactories.EmptyComponent()
        );

        reconciler.Reconcile(vnode, container, null);

        // The component renders a FlexPanel, so the View's child should be a FlexPanel
        Assert.IsInstanceOfType(container.Children[0], typeof(FlexPanel));
    }

    /// <summary>
    /// Verifies that constructor args flow through to the rendered output.
    /// TitleComponent renders a FlexPanel with Name=Title.
    /// </summary>
    [TestMethod]
    public void Reconcile_TitleComponent_SetsName()
    {
        var container = new FlexPanel();
        var reconciler = new Reconciler();

        var vnode = ViewNodeFactories.View(children:
            TitleComponentComponentFactories.TitleComponent(title: "MyTitle")
        );

        reconciler.Reconcile(vnode, container, null);

        var child = container.Children[0] as FlexPanel;
        Assert.IsNotNull(child);
        Assert.AreEqual("MyTitle", child.Name);
    }

    /// <summary>
    /// Verifies that updating a component re-renders with new args.
    /// </summary>
    [TestMethod]
    public void Reconcile_UpdateComponent_ReflectsNewTitle()
    {
        var container = new FlexPanel();
        var reconciler = new Reconciler();

        // First render
        var vnode1 = ViewNodeFactories.View(children:
            TitleComponentComponentFactories.TitleComponent(title: "First")
        );
        var root = reconciler.Reconcile(vnode1, container, null);

        // Second render with different title
        var vnode2 = ViewNodeFactories.View(children:
            TitleComponentComponentFactories.TitleComponent(title: "Second")
        );
        reconciler.Reconcile(vnode2, container, root);

        var child = container.Children[0] as FlexPanel;
        Assert.IsNotNull(child);
        Assert.AreEqual("Second", child.Name);
        Assert.AreEqual(1, container.Children.Count);
    }

    /// <summary>
    /// Verifies that a component with a child VNode constructor param can be reconciled.
    /// </summary>
    [TestMethod]
    public void Reconcile_WrapperComponent_PassesChildThrough()
    {
        var container = new FlexPanel();
        var reconciler = new Reconciler();

        var inner = FlexPanelNodeFactories.FlexPanel(name: "inner");
        var wrapperNode = WrapperComponentComponentFactories.WrapperComponent(child: inner);

        var vnode = ViewNodeFactories.View(children: wrapperNode);
        reconciler.Reconcile(vnode, container, null);

        var wrapperOutput = container.Children[0] as FlexPanel;
        Assert.IsNotNull(wrapperOutput);
        Assert.AreEqual("inner", wrapperOutput.Name);
    }
}
