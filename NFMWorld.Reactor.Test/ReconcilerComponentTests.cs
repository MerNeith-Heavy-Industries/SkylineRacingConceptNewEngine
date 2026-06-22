using NFMWorld.Reactor.TestFixtures;
using static NFMWorld.Reactor.TestFixtures.Nodes;
using static WorldXaml.UI.Yoga.Nodes;
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

        var vnode = View(name: "vnode", children:
            EmptyComponent()
        );

        var root = reconciler.Reconcile(vnode, container, null);

        Assert.IsNotNull(root);
        Assert.HasCount(1, container.Children);
    }

    /// <summary>
    /// Direct: Component's Render output is a FlexPanelNode with name.
    /// </summary>
    [TestMethod]
    public void Reconcile_ComponentRendersNamedNode_NameApplied()
    {
        var container = new FlexPanel { Name = "container" };
        var reconciler = new Reconciler();
        
        // Step 1: verify the factory-produced VNode has Name
        var compNode = TitleComponent(title: "CompName");
        var comp = (TitleComponent)compNode.CreateComponent();
        var rendered = (VNode)comp.GetType().GetMethod("Render", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            !.Invoke(comp, null)!;
        Assert.AreEqual("CompName", ((VisualVNode)rendered).Name, "VNode name should be set after Render");
        
        // Step 2: reconcile directly (should work)
        var directResult = reconciler.Reconcile(rendered, new FlexPanel(), null);
        Assert.AreEqual("CompName", directResult.Name, "Direct reconcile should preserve name");
        
        // Step 3: reconcile through View wrapper
        var vnode = View(name: "vnode", children: compNode);
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
        var container = new FlexPanel { Name = "container" };
        var reconciler = new Reconciler();
        var compNode = TitleComponent(title: "DirectComp");
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
        var container = new FlexPanel { Name = "container" };
        var reconciler = new Reconciler();
        var flexNode = FlexPanel(name: "DirectName");
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
        var container = new FlexPanel { Name = "container" };
        var reconciler = new Reconciler();

        var vnode = View(name: "vnode", children:
            EmptyComponent()
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
        var container = new FlexPanel { Name = "container" };
        var reconciler = new Reconciler();

        var vnode = View(name: "vnode", children:
            TitleComponent(title: "MyTitle")
        );

        reconciler.Reconcile(vnode, container, null);

        var child = (container.Children[0] as FlexPanel)?.Children[0] as FlexPanel;
        Assert.IsNotNull(child);
        Assert.AreEqual("MyTitle", child.Name);
    }

    /// <summary>
    /// Verifies that updating a component re-renders with new args.
    /// </summary>
    [TestMethod]
    public void Reconcile_UpdateComponent_ReflectsNewTitle()
    {
        var container = new FlexPanel { Name = "container" };
        var reconciler = new Reconciler();

        // First render
        var vnode1 = View(name: "vnode1", children:
            TitleComponent(title: "First")
        );
        var root = reconciler.Reconcile(vnode1, container, null);

        // Second render with different title
        var vnode2 = View(name: "vnode2", children:
            TitleComponent(title: "Second")
        );
        reconciler.Reconcile(vnode2, container, root);

        var child = (container.Children[0] as FlexPanel)?.Children[0] as FlexPanel;
        Assert.IsNotNull(child);
        Assert.AreEqual("Second", child.Name);
        Assert.HasCount(1, container.Children);
    }

    /// <summary>
    /// Verifies that a component with a child VNode constructor param can be reconciled.
    /// </summary>
    [TestMethod]
    public void Reconcile_WrapperComponent_PassesChildThrough()
    {
        var container = new FlexPanel { Name = "container" };
        var reconciler = new Reconciler();

        var inner = FlexPanel(name: "inner");
        var wrapperNode = WrapperComponent(child: inner);

        var vnode = View(children: wrapperNode, name: "view");
        reconciler.Reconcile(vnode, container, null);

        var wrapperOutput = (container.Children[0] as FlexPanel)?.Children[0] as FlexPanel;
        Assert.IsNotNull(wrapperOutput);
        Assert.AreEqual("inner", wrapperOutput.Name);
    }
}
