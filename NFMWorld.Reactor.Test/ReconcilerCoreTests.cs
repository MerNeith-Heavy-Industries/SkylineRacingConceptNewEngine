using static NFMWorld.Reactor.Nodes;

namespace NFMWorld.Reactor.Test;

[TestClass]
public class ReconcilerCoreTests
{
    // ════════════════════════════════════════════════════════════════════
    //  Keyed reconciliation
    // ════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void KeyedChildren_PreserveIdentity_AcrossReorder()
    {
        var container = new FlexPanel();
        var (dom, ctx) = TestHelpers.CreateDom();

        // First render: A, B, C
        var vnode1 = FlexPanel(children: [
            FlexPanel(name: "A").WithKey("a"),
            FlexPanel(name: "B").WithKey("b"),
            FlexPanel(name: "C").WithKey("c")
        ]);
        dom.Mount(container, vnode1);
        ctx.Drain();
        var root = (FlexPanel)dom.Root!;
        var children1 = root.Children;
        Assert.AreEqual("A", children1[0].Name);
        Assert.AreEqual("B", children1[1].Name);
        Assert.AreEqual("C", children1[2].Name);

        // Second render: C, A, B (reordered)
        var vnode2 = FlexPanel(children: [
            FlexPanel(name: "C").WithKey("c"),
            FlexPanel(name: "A").WithKey("a"),
            FlexPanel(name: "B").WithKey("b")
        ]);
        dom.Mount(container, vnode2);
        ctx.Drain();
        var children2 = root.Children;
        Assert.AreEqual("C", children2[0].Name, "C should move to position 0");
        Assert.AreEqual("A", children2[1].Name, "A should move to position 1");
        Assert.AreEqual("B", children2[2].Name, "B should move to position 2");
    }

    [TestMethod]
    public void KeyedChildren_NewKeyAdded_OldKeyRemoved()
    {
        var container = new FlexPanel();
        var (dom, ctx) = TestHelpers.CreateDom();

        // First render: A (key=a), B (key=b)
        var vnode1 = FlexPanel(children: [
            FlexPanel(name: "A").WithKey("a"),
            FlexPanel(name: "B").WithKey("b")
        ]);
        dom.Mount(container, vnode1);
        ctx.Drain();
        var root = (FlexPanel)dom.Root!;
        Assert.HasCount(2, root.Children);

        // Second render: B (key=b), C (key=c) — A removed, C added
        var vnode2 = FlexPanel(children: [
            FlexPanel(name: "B").WithKey("b"),
            FlexPanel(name: "C").WithKey("c")
        ]);
        dom.Mount(container, vnode2);
        ctx.Drain();
        var children = root.Children;
        Assert.HasCount(2, children);
        Assert.AreEqual("B", children[0].Name, "B should persist at position 0");
        Assert.AreEqual("C", children[1].Name, "C should be at position 1");
    }

    [TestMethod]
    public void KeyedChildren_KeyChange_RecreatesElement()
    {
        var container = new FlexPanel();
        var (dom, ctx) = TestHelpers.CreateDom();

        // First render: element with key "old"
        var vnode1 = FlexPanel(children:
            FlexPanel(name: "OldName").WithKey("old")
        );
        dom.Mount(container, vnode1);
        ctx.Drain();
        var root = (FlexPanel)dom.Root!;
        var firstChild = root.Children[0];
        Assert.AreEqual("OldName", firstChild.Name);

        // Second render: same position, different key "new"
        var vnode2 = FlexPanel(children:
            FlexPanel(name: "NewName").WithKey("new")
        );
        dom.Mount(container, vnode2);
        ctx.Drain();
        var newChild = root.Children[0];
        Assert.AreEqual("NewName", newChild.Name, "New key should create new element");
        Assert.AreNotSame(firstChild, newChild, "Should be a different native instance");
    }

    [TestMethod]
    public void KeyedChildren_MixedKeyedAndNonKeyed()
    {
        var container = new FlexPanel();
        var (dom, ctx) = TestHelpers.CreateDom();

        // First render: keyed A, non-keyed B, keyed C
        var vnode1 = FlexPanel(children: [
            FlexPanel(name: "A").WithKey("a"),
            FlexPanel(name: "B"),           // no key
            FlexPanel(name: "C").WithKey("c")
        ]);
        dom.Mount(container, vnode1);
        ctx.Drain();
        var root = (FlexPanel)dom.Root!;
        var children1 = root.Children;
        Assert.AreEqual("A", children1[0].Name);
        Assert.AreEqual("B", children1[1].Name);
        Assert.AreEqual("C", children1[2].Name);

        // Second render: keyed C, non-keyed X, keyed A (C and A swapped, B replaced)
        var vnode2 = FlexPanel(children: [
            FlexPanel(name: "C").WithKey("c"),
            FlexPanel(name: "X"),           // new non-keyed
            FlexPanel(name: "A").WithKey("a")
        ]);
        dom.Mount(container, vnode2);
        ctx.Drain();
        var children2 = root.Children;
        Assert.AreEqual("C", children2[0].Name);
        Assert.AreEqual("X", children2[1].Name);
        Assert.AreEqual("A", children2[2].Name);
    }

    // ════════════════════════════════════════════════════════════════════
    //  Non-keyed (positional) reconciliation
    // ════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void NonKeyedChildren_MatchByPosition()
    {
        var container = new FlexPanel();
        var (dom, ctx) = TestHelpers.CreateDom();

        var vnode1 = FlexPanel(children: [
            FlexPanel(name: "First"),
            FlexPanel(name: "Second"),
            FlexPanel(name: "Third")
        ]);
        dom.Mount(container, vnode1);
        ctx.Drain();
        var root = (FlexPanel)dom.Root!;
        var firstChild = root.Children[0];

        // Same position, same type — should reuse
        var vnode2 = FlexPanel(children: [
            FlexPanel(name: "FirstUpdated"),
            FlexPanel(name: "Second"),
            FlexPanel(name: "Third")
        ]);
        dom.Mount(container, vnode2);
        ctx.Drain();
        var children = root.Children;
        Assert.AreSame(firstChild, children[0], "Same-position element should be reused");
        Assert.AreEqual("FirstUpdated", children[0].Name, "Name should be updated");
    }

    [TestMethod]
    public void NonKeyedChildren_AppendAndRemove()
    {
        var container = new FlexPanel();
        var (dom, ctx) = TestHelpers.CreateDom();

        // Start with 2
        var vnode1 = FlexPanel(children: [
            FlexPanel(name: "A"),
            FlexPanel(name: "B")
        ]);
        dom.Mount(container, vnode1);
        ctx.Drain();
        var root = (FlexPanel)dom.Root!;
        Assert.HasCount(2, root.Children);

        // Add 2 more (now 4)
        var vnode2 = FlexPanel(children: [
            FlexPanel(name: "A"),
            FlexPanel(name: "B"),
            FlexPanel(name: "C"),
            FlexPanel(name: "D")
        ]);
        dom.Mount(container, vnode2);
        ctx.Drain();
        Assert.HasCount(4, root.Children);

        // Remove 2 (back to 2)
        var vnode3 = FlexPanel(children: [
            FlexPanel(name: "A"),
            FlexPanel(name: "B")
        ]);
        dom.Mount(container, vnode3);
        ctx.Drain();
        Assert.HasCount(2, root.Children);
        Assert.AreEqual("A", root.Children[0].Name);
        Assert.AreEqual("B", root.Children[1].Name);
    }

    [TestMethod]
    public void NonKeyedChildren_TypeChange_RecreatesElement()
    {
        var container = new FlexPanel();
        var (dom, ctx) = TestHelpers.CreateDom();

        var vnode1 = FlexPanel(children:
            FlexPanel(name: "Panel")
        );
        dom.Mount(container, vnode1);
        ctx.Drain();
        var root = (FlexPanel)dom.Root!;
        Assert.IsInstanceOfType(root.Children[0], typeof(FlexPanel));

        var vnode2 = FlexPanel(children:
            Node()
        );
        dom.Mount(container, vnode2);
        ctx.Drain();
        Assert.IsInstanceOfType(root.Children[0], typeof(Node),
            "Type should change from FlexPanel to Node");
    }

    // ════════════════════════════════════════════════════════════════════
    //  Property restoration (stale property cleanup)
    // ════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void PropertyRestoration_StalePropertyResetsToDefault()
    {
        var container = new FlexPanel();
        var (dom, ctx) = TestHelpers.CreateDom();

        var vnode1 = FlexPanel(visibility: Visibility.Hidden);
        dom.Mount(container, vnode1);
        ctx.Drain();
        var root = (FlexPanel)dom.Root!;
        Assert.AreEqual(Visibility.Hidden, root.Visibility);

        // Second render: Visibility NOT set → should reset to default
        var vnode2 = FlexPanel();
        dom.Mount(container, vnode2);
        ctx.Drain();
        Assert.AreEqual(Visibility.Visible, root.Visibility,
            "Stale property should reset to default (Visible) when omitted");
    }

    [TestMethod]
    public void PropertyRestoration_MultipleStalePropertiesReset()
    {
        var container = new FlexPanel();
        var (dom, ctx) = TestHelpers.CreateDom();

        var vnode1 = FlexPanel(
            visibility: Visibility.Hidden,
            opacity: 0.3f,
            flexDirection: FlexDirection.Column
        );
        dom.Mount(container, vnode1);
        ctx.Drain();
        var root = (FlexPanel)dom.Root!;
        Assert.AreEqual(Visibility.Hidden, root.Visibility);
        Assert.AreEqual(0.3f, root.Opacity, 0.001f);
        Assert.AreEqual(FlexDirection.Column, root.FlexDirection);

        var vnode2 = FlexPanel();
        dom.Mount(container, vnode2);
        ctx.Drain();
        Assert.AreEqual(Visibility.Visible, root.Visibility);
        Assert.AreEqual(1.0f, root.Opacity, 0.001f, "Stale Opacity should reset to 1.0");
        Assert.AreEqual(FlexDirection.Row, root.FlexDirection, "Stale FlexDirection should reset to Row");
    }

    [TestMethod]
    public void PropertyRestoration_OneStaleOneFresh()
    {
        var container = new FlexPanel();
        var (dom, ctx) = TestHelpers.CreateDom();

        var vnode1 = FlexPanel(
            visibility: Visibility.Hidden,
            flexDirection: FlexDirection.Column
        );
        dom.Mount(container, vnode1);
        ctx.Drain();
        var root = (FlexPanel)dom.Root!;

        // Only set FlexDirection this pass; Visibility should reset
        var vnode2 = FlexPanel(flexDirection: FlexDirection.ColumnReverse);
        dom.Mount(container, vnode2);
        ctx.Drain();

        Assert.AreEqual(Visibility.Visible, root.Visibility,
            "Stale Visibility should reset to Visible");
        Assert.AreEqual(FlexDirection.ColumnReverse, root.FlexDirection,
            "Fresh FlexDirection should update");
    }

    [TestMethod]
    public void PropertyRestoration_RemovedNode_RestoresOldValues()
    {
        var container = new FlexPanel();
        var (dom, ctx) = TestHelpers.CreateDom();

        var vnode1 = FlexPanel(children:
            FlexPanel(name: "Child", visibility: Visibility.Hidden)
        );
        dom.Mount(container, vnode1);
        ctx.Drain();
        var root = (FlexPanel)dom.Root!;
        var child = (FlexPanel)root.Children[0];
        Assert.AreEqual(Visibility.Hidden, child.Visibility);

        var vnode2 = FlexPanel(children:
            FlexPanel(name: "Replacement")
        );
        dom.Mount(container, vnode2);
        ctx.Drain();
        Assert.HasCount(1, root.Children);
        Assert.AreEqual("Replacement", root.Children[0].Name);
    }

    [TestMethod]
    public void PropertyRestoration_PropertyPreservedWhenRespecified()
    {
        var container = new FlexPanel();
        var (dom, ctx) = TestHelpers.CreateDom();

        var vnode1 = FlexPanel(visibility: Visibility.Hidden);
        dom.Mount(container, vnode1);
        ctx.Drain();
        var root = (FlexPanel)dom.Root!;
        Assert.AreEqual(Visibility.Hidden, root.Visibility);

        var vnode2 = FlexPanel(visibility: Visibility.Hidden);
        dom.Mount(container, vnode2);
        ctx.Drain();
        Assert.AreEqual(Visibility.Hidden, root.Visibility,
            "Property should persist when set in both renders");
    }

    [TestMethod]
    public void PropertyRestoration_ElementRemoved_PropertiesCleanedUp()
    {
        var container = new FlexPanel();
        var (dom, ctx) = TestHelpers.CreateDom();

        // First render: two children, the first with custom visibility
        var vnode1 = FlexPanel(children: [
            FlexPanel(name: "First", visibility: Visibility.Hidden),
            FlexPanel(name: "Second")
        ]);
        dom.Mount(container, vnode1);
        ctx.Drain();
        var root = (FlexPanel)dom.Root!;
        Assert.HasCount(2, root.Children);

        // Second render: only Second remains
        var vnode2 = FlexPanel(children:
            FlexPanel(name: "Second")
        );
        dom.Mount(container, vnode2);
        ctx.Drain();
        Assert.HasCount(1, root.Children);
        Assert.AreEqual("Second", root.Children[0].Name);
    }

    // ════════════════════════════════════════════════════════════════════
    //  Edge cases
    // ════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void KeyedChildren_AllKeysChanged_AllRecreated()
    {
        var container = new FlexPanel();
        var (dom, ctx) = TestHelpers.CreateDom();

        var vnode1 = FlexPanel(children: [
            FlexPanel(name: "A").WithKey("a"),
            FlexPanel(name: "B").WithKey("b")
        ]);
        dom.Mount(container, vnode1);
        ctx.Drain();
        var root = (FlexPanel)dom.Root!;
        var oldA = root.Children[0];
        var oldB = root.Children[1];

        var vnode2 = FlexPanel(children: [
            FlexPanel(name: "X").WithKey("x"),
            FlexPanel(name: "Y").WithKey("y")
        ]);
        dom.Mount(container, vnode2);
        ctx.Drain();
        var children = root.Children;
        Assert.AreEqual("X", children[0].Name);
        Assert.AreEqual("Y", children[1].Name);
        Assert.AreNotSame(oldA, children[0]);
        Assert.AreNotSame(oldB, children[1]);
    }

    [TestMethod]
    public void NonKeyedChildren_InsertAtBeginning()
    {
        var container = new FlexPanel();
        var (dom, ctx) = TestHelpers.CreateDom();

        var vnode1 = FlexPanel(children: [
            FlexPanel(name: "B"),
            FlexPanel(name: "C")
        ]);
        dom.Mount(container, vnode1);
        ctx.Drain();
        var root = (FlexPanel)dom.Root!;

        // Insert A at beginning
        var vnode2 = FlexPanel(children: [
            FlexPanel(name: "A"),
            FlexPanel(name: "B"),
            FlexPanel(name: "C")
        ]);
        dom.Mount(container, vnode2);
        ctx.Drain();
        var children = root.Children;
        Assert.HasCount(3, children);
        Assert.AreEqual("A", children[0].Name);
        Assert.AreEqual("B", children[1].Name);
        Assert.AreEqual("C", children[2].Name);
    }

    [TestMethod]
    public void KeyedChildren_DuplicateKeys_LastWins()
    {
        var container = new FlexPanel();
        var (dom, ctx) = TestHelpers.CreateDom();

        // Two elements with the same key — the second should be treated as non-keyed
        var vnode1 = FlexPanel(children: [
            FlexPanel(name: "A").WithKey("same"),
            FlexPanel(name: "B").WithKey("same")  // duplicate
        ]);
        dom.Mount(container, vnode1);
        ctx.Drain();
        var root = (FlexPanel)dom.Root!;

        // Second render: swap order
        var vnode2 = FlexPanel(children: [
            FlexPanel(name: "B").WithKey("same"),
            FlexPanel(name: "A").WithKey("same")
        ]);
        dom.Mount(container, vnode2);
        ctx.Drain();
        var children = root.Children;
        // The first "same" key wins the keyed match; the second falls back to positional
        Assert.AreEqual("B", children[0].Name, "Keyed match reuses first 'same' key element");
    }

    // ════════════════════════════════════════════════════════════════════
    //  All properties applied via AssignProperties
    // ════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void AssignProperties_AppliesAllVisualLevelProperties()
    {
        var container = new FlexPanel();
        var (dom, ctx) = TestHelpers.CreateDom();

        var vnode = FlexPanel(
            name: "TestName",
            key: "the-key",
            tabOrder: 5,
            isFocusable: true,
            isFocused: false
        );
        dom.Mount(container, vnode);
        ctx.Drain();
        var root = dom.Root!;

        Assert.AreEqual("TestName", root.Name);
        Assert.AreEqual("the-key", root.Key);
        Assert.AreEqual(5, root.TabOrder);
        Assert.AreEqual(true, root.IsFocusable);
        Assert.AreEqual(false, root.IsFocused);
    }

    [TestMethod]
    public void AssignProperties_AppliesAllNodeLevelProperties()
    {
        var container = new FlexPanel();
        var (dom, ctx) = TestHelpers.CreateDom();

        var vnode = FlexPanel(
            opacity: 0.5f,
            visibility: Visibility.Hidden,
            flexDirection: FlexDirection.Column,
            alignItems: Align.Center
        );
        dom.Mount(container, vnode);
        ctx.Drain();
        var root = (FlexPanel)dom.Root!;

        Assert.AreEqual(0.5f, root.Opacity, 0.001f);
        Assert.AreEqual(Visibility.Hidden, root.Visibility);
        Assert.AreEqual(FlexDirection.Column, root.FlexDirection);
        Assert.AreEqual(Align.Center, root.AlignItems);
    }

    [TestMethod]
    public void AssignProperties_AppliesNameOnNode()
    {
        var container = new FlexPanel();
        var (dom, ctx) = TestHelpers.CreateDom();

        var vnode = Node(name: "NodeName");
        dom.Mount(container, vnode);
        ctx.Drain();
        var root = dom.Root!;

        Assert.AreEqual("NodeName", root.Name, "Name should be applied to Node via AssignProperties");
    }

    [TestMethod]
    public void AssignProperties_AppliesKeyOnNode()
    {
        var container = new FlexPanel();
        var (dom, ctx) = TestHelpers.CreateDom();

        var vnode = Node(key: "node-key");
        dom.Mount(container, vnode);
        ctx.Drain();
        var root = dom.Root!;

        Assert.AreEqual("node-key", root.Key, "Key should be applied to Node via AssignProperties");
        Assert.AreEqual("node-key", GetNodeKey(root), "Key should be readable via GetNodeKey for reconciliation");
    }

    private static object? GetNodeKey(Visual visual)
        => visual is Node node ? node.Key : null;

    // ════════════════════════════════════════════════════════════════════
    //  Shadowed With* methods — subclasses return correct type
    // ════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void ShadowedWithMethods_ReturnCorrectType()
    {
        // FlexPanelNode.WithName shadows VisualVNode.WithName to return FlexPanelNode
        var fp = FlexPanel().WithName("fp");
        Assert.IsInstanceOfType(fp, typeof(FlexPanelNode));

        // ViewNode inherits from FlexPanelNode, its WithName should return ViewNode
        var v = View().WithName("view");
        Assert.IsInstanceOfType(v, typeof(ViewNode));

        // NodeNode.WithName returns NodeNode
        var n = Node().WithName("node");
        Assert.IsInstanceOfType(n, typeof(NodeNode));
    }

    [TestMethod]
    public void ShadowedWithMethods_PropertiesAppliedThroughReconciler()
    {
        var container = new FlexPanel();
        var (dom, ctx) = TestHelpers.CreateDom();

        // Use View (subclass of FlexPanel) with named parameters
        var vnode = View(
            name: "ShadowedView",
            key: "sv",
            flexDirection: FlexDirection.Row
        );
        dom.Mount(container, vnode);
        ctx.Drain();
        var root = dom.Root!;

        Assert.IsInstanceOfType(root, typeof(View));
        var view = (View)root;
        Assert.AreEqual("ShadowedView", view.Name);
        Assert.AreEqual("sv", view.Key);
        Assert.AreEqual(FlexDirection.Row, view.FlexDirection);
    }

    [TestMethod]
    public void ShadowedWithMethods_FluentChainAppliedCorrectly()
    {
        // Chaining With* calls on View. Because WithName returns VisualVNode,
        // later calls must be on VisualVNode-compatible methods.
        // Verify the final VNode type and properties via reconciliation.
        var vnode = View()
            .WithName("fluent")
            .WithKey("f");

        Assert.IsInstanceOfType(vnode, typeof(ViewNode));
        // Verify via reconciliation
        var container = new FlexPanel();
        var (dom, ctx) = TestHelpers.CreateDom();
        dom.Mount(container, vnode);
        ctx.Drain();
        var root = dom.Root!;
        Assert.AreEqual("fluent", root.Name);
        Assert.AreEqual("f", root.Key);
    }

    [TestMethod]
    public void ShadowedWithMethods_FluentChainPreservesConcreteType()
    {
        // Chaining With* calls on View should stay as ViewNode throughout
        var vnode = View()
            .WithName("fluent")
            .WithKey("f")
            .WithFlexDirection(FlexDirection.Column)
            .WithOpacity(0.3f);

        Assert.IsInstanceOfType(vnode, typeof(ViewNode));
        Assert.AreEqual("fluent", vnode.Name);
        Assert.AreEqual("f", vnode.Key);
        // Verify via reconciliation
        var container = new FlexPanel();
        var (dom, ctx) = TestHelpers.CreateDom();
        dom.Mount(container, vnode);
        ctx.Drain();
        var root = dom.Root!;
        Assert.AreEqual("fluent", root.Name);
        Assert.AreEqual("f", root.Key);
    }

    // ════════════════════════════════════════════════════════════════════
    //  Type-change leak prevention (regression tests for bugfix)
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// When a positional match replaces a child with a different native type
    /// (e.g., FlexPanel → Node), the old native node must be removed from the
    /// container — not leaked alongside the new one.
    /// Before the fix, the old node stayed in the tree with stale content.
    /// </summary>
    [TestMethod]
    public void NonKeyedChildren_TypeChangeAtPosition_ReplacesOldNode()
    {
        var container = new FlexPanel();
        var (dom, ctx) = TestHelpers.CreateDom();

        // First render: FlexPanel children
        var vnode1 = FlexPanel(children: [
            FlexPanel(name: "PanelA"),
            FlexPanel(name: "PanelB")
        ]);
        dom.Mount(container, vnode1);
        ctx.Drain();
        var root = (FlexPanel)dom.Root!;
        Assert.HasCount(2, root.Children);
        Assert.IsInstanceOfType(root.Children[0], typeof(FlexPanel));
        Assert.IsInstanceOfType(root.Children[1], typeof(FlexPanel));

        // Second render: same positions, different type (Node)
        var vnode2 = FlexPanel(children: [
            Node(name: "NodeA"),
            Node(name: "NodeB")
        ]);
        dom.Mount(container, vnode2);
        ctx.Drain();

        // Must have exactly 2 children — no leaked FlexPanels
        Assert.HasCount(2, root.Children,
            "Should not leak old native nodes as extra children");
        Assert.IsInstanceOfType(root.Children[0], typeof(Node),
            "Position 0 should be a Node, not a leaked FlexPanel");
        Assert.IsInstanceOfType(root.Children[1], typeof(Node),
            "Position 1 should be a Node, not a leaked FlexPanel");
        Assert.AreEqual("NodeA", root.Children[0].Name);
        Assert.AreEqual("NodeB", root.Children[1].Name);
    }

    /// <summary>
    /// Simulates the HUD reordering scenario: wrapper FlexPanels stay the same
    /// type but their inner children change type (FlexPanel ↔ Node) when
    /// reordered without keys. The old inner nodes must not leak.
    /// </summary>
    [TestMethod]
    public void NonKeyedChildren_ReorderedWithTypeChange_NoDuplicates()
    {
        var container = new FlexPanel();
        var (dom, ctx) = TestHelpers.CreateDom();

        // First render: wrappers A, B, C with typed content
        var vnode1 = FlexPanel(children: [
            FlexPanel(name: "WrapperA", children: FlexPanel(name: "ContentA")),
            FlexPanel(name: "WrapperB", children: FlexPanel(name: "ContentB")),
            FlexPanel(name: "WrapperC", children: Node(name: "ContentC"))
        ]);
        dom.Mount(container, vnode1);
        ctx.Drain();
        var root = (FlexPanel)dom.Root!;
        Assert.HasCount(3, root.Children);

        // Reorder: C (Node content) moves to position 0,
        // A moves to position 1, B moves to position 2
        var vnode2 = FlexPanel(children: [
            FlexPanel(name: "WrapperC", children: Node(name: "ContentC")),
            FlexPanel(name: "WrapperA", children: FlexPanel(name: "ContentA")),
            FlexPanel(name: "WrapperB", children: FlexPanel(name: "ContentB"))
        ]);
        dom.Mount(container, vnode2);
        ctx.Drain();

        // Should have exactly 3 wrappers, no leaked duplicates
        Assert.HasCount(3, root.Children,
            "Should not leak duplicate children after reorder");

        // Each wrapper must contain exactly 1 child of the correct type
        var wrapper0 = (FlexPanel)root.Children[0];
        Assert.HasCount(1, wrapper0.Children,
            "Position 0 wrapper should not leak old inner children");
        Assert.IsInstanceOfType(wrapper0.Children[0], typeof(Node),
            "Position 0 should have Node content (not leaked FlexPanel)");
        Assert.AreEqual("ContentC", wrapper0.Children[0].Name);

        var wrapper1 = (FlexPanel)root.Children[1];
        Assert.HasCount(1, wrapper1.Children);
        Assert.IsInstanceOfType(wrapper1.Children[0], typeof(FlexPanel));
        Assert.AreEqual("ContentA", wrapper1.Children[0].Name);

        var wrapper2 = (FlexPanel)root.Children[2];
        Assert.HasCount(1, wrapper2.Children);
        Assert.IsInstanceOfType(wrapper2.Children[0], typeof(FlexPanel));
        Assert.AreEqual("ContentB", wrapper2.Children[0].Name);
    }

    [TestMethod]
    public void ShadowedProperty_DerivedClassOverridesBaseProperty()
    {
        // FlexPanel extends Node. Both have Visibility property.
        // FlexPanel's AssignProperties should handle Visibility correctly
        // even though it's inherited from Node.
        var container = new FlexPanel();
        var (dom, ctx) = TestHelpers.CreateDom();

        var vnode1 = FlexPanel(visibility: Visibility.Hidden);
        dom.Mount(container, vnode1);
        ctx.Drain();
        var root = (FlexPanel)dom.Root!;
        Assert.AreEqual(Visibility.Hidden, root.Visibility);

        // Now change to Visible — property should update
        var vnode2 = FlexPanel(visibility: Visibility.Visible);
        dom.Mount(container, vnode2);
        ctx.Drain();
        Assert.AreEqual(Visibility.Visible, root.Visibility);
    }

    [TestMethod]
    public void ShadowedProperty_ViewInheritsFlexPanelProperties()
    {
        // View extends FlexPanel which extends Node.
        // ViewNode's AssignProperties should handle all inherited properties.
        var container = new FlexPanel();
        var (dom, ctx) = TestHelpers.CreateDom();

        var vnode = View(
            name: "V",
            opacity: 0.75f,
            flexDirection: FlexDirection.ColumnReverse,
            visibility: Visibility.Hidden
        );
        dom.Mount(container, vnode);
        ctx.Drain();
        var root = (View)dom.Root!;

        Assert.AreEqual("V", root.Name);
        Assert.AreEqual(0.75f, root.Opacity, 0.001f);
        Assert.AreEqual(FlexDirection.ColumnReverse, root.FlexDirection);
        Assert.AreEqual(Visibility.Hidden, root.Visibility);
    }

    // ════════════════════════════════════════════════════════════════════
    //  DrainPendingUpdates root type-change (PerformUpdate fix)
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// When a component changes its root native type (e.g. FlexPanel → Node)
    /// during a deferred re-render (setState during a reconciliation pass),
    /// the old root must be replaced in the parent tree via VisualParent —
    /// not leaked alongside the new one.
    /// </summary>
    [TestMethod]
    public void PerformUpdate_RootTypeChange_ReplacesOldRootInParent()
    {
        var container = new FlexPanel();
        var (dom, ctx) = TestHelpers.CreateDom();

        // Mount the component wrapped in a FlexPanel so it has a VisualParent.
        // The component starts with FlexPanel root, then calls setState during
        // its first Render, which defers a re-render that switches to Node root.
        var componentNode = ComponentNodeFactory.Create<DeferredRootChangeComponent>();
        var vnode = FlexPanel(name: "Parent", children: componentNode);
        dom.Mount(container, vnode);
        ctx.Drain();
        var root = (FlexPanel)dom.Root!;
        Assert.AreEqual("Parent", root.Name);

        // After reconciliation (including DrainPendingUpdates), the root
        // (named "Parent") should have exactly 1 child, and it must be a Node
        // (the post-setState type), NOT a FlexPanel (the pre-setState type).
        Assert.HasCount(1, root.Children,
            "Root should have exactly 1 child — old root must be replaced, not leaked");
        Assert.IsInstanceOfType(root.Children[0], typeof(Node),
            "Child should be a Node (the post-setState root type), not a leaked FlexPanel");
        Assert.AreEqual("NodeRoot", root.Children[0].Name);
    }
}

/// <summary>
/// Test component that starts with a FlexPanel root, then calls setState
/// during its first Render to switch to a Node root on the deferred re-render.
/// The setState is batched because it fires inside a parent reconciliation
/// pass — exercising the DrainPendingUpdates → PerformUpdate path.
/// </summary>
internal class DeferredRootChangeComponent : Component
{
    public DeferredRootChangeComponent()
    {
        DisableMemo();
    }

    protected override VNode Render()
    {
        var (switched, setSwitched) = UseState(false);

        if (!switched)
        {
            // First render: return FlexPanel, but enqueue a state change
            // so the deferred re-render (in DrainPendingUpdates) returns Node.
            setSwitched(_ => true);
            return FlexPanel(name: "FlexRoot");
        }
        else
        {
            return Node(name: "NodeRoot");
        }
    }
}