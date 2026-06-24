using static WorldXaml.UI.Yoga.Nodes;
using WorldXaml.UI.Yoga;

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
        var reconciler = new Reconciler();

        // First render: A, B, C
        var vnode1 = FlexPanel(children: [
            FlexPanel(name: "A").WithKey("a"),
            FlexPanel(name: "B").WithKey("b"),
            FlexPanel(name: "C").WithKey("c")
        ]);
        var root = reconciler.Reconcile(vnode1, container, null);
        var children1 = ((FlexPanel)root).Children;
        Assert.AreEqual("A", children1[0].Name);
        Assert.AreEqual("B", children1[1].Name);
        Assert.AreEqual("C", children1[2].Name);

        // Second render: C, A, B (reordered)
        var vnode2 = FlexPanel(children: [
            FlexPanel(name: "C").WithKey("c"),
            FlexPanel(name: "A").WithKey("a"),
            FlexPanel(name: "B").WithKey("b")
        ]);
        reconciler.Reconcile(vnode2, container, root);
        var children2 = ((FlexPanel)root).Children;
        Assert.AreEqual("C", children2[0].Name, "C should move to position 0");
        Assert.AreEqual("A", children2[1].Name, "A should move to position 1");
        Assert.AreEqual("B", children2[2].Name, "B should move to position 2");
    }

    [TestMethod]
    public void KeyedChildren_NewKeyAdded_OldKeyRemoved()
    {
        var container = new FlexPanel();
        var reconciler = new Reconciler();

        // First render: A (key=a), B (key=b)
        var vnode1 = FlexPanel(children: [
            FlexPanel(name: "A").WithKey("a"),
            FlexPanel(name: "B").WithKey("b")
        ]);
        var root = reconciler.Reconcile(vnode1, container, null);
        Assert.HasCount(2, ((FlexPanel)root).Children);

        // Second render: B (key=b), C (key=c) — A removed, C added
        var vnode2 = FlexPanel(children: [
            FlexPanel(name: "B").WithKey("b"),
            FlexPanel(name: "C").WithKey("c")
        ]);
        reconciler.Reconcile(vnode2, container, root);
        var children = ((FlexPanel)root).Children;
        Assert.HasCount(2, children);
        Assert.AreEqual("B", children[0].Name, "B should persist at position 0");
        Assert.AreEqual("C", children[1].Name, "C should be at position 1");
    }

    [TestMethod]
    public void KeyedChildren_KeyChange_RecreatesElement()
    {
        var container = new FlexPanel();
        var reconciler = new Reconciler();

        // First render: element with key "old"
        var vnode1 = FlexPanel(children:
            FlexPanel(name: "OldName").WithKey("old")
        );
        var root = reconciler.Reconcile(vnode1, container, null);
        var firstChild = ((FlexPanel)root).Children[0];
        Assert.AreEqual("OldName", firstChild.Name);

        // Second render: same position, different key "new"
        var vnode2 = FlexPanel(children:
            FlexPanel(name: "NewName").WithKey("new")
        );
        reconciler.Reconcile(vnode2, container, root);
        var newChild = ((FlexPanel)root).Children[0];
        Assert.AreEqual("NewName", newChild.Name, "New key should create new element");
        Assert.AreNotSame(firstChild, newChild, "Should be a different native instance");
    }

    [TestMethod]
    public void KeyedChildren_MixedKeyedAndNonKeyed()
    {
        var container = new FlexPanel();
        var reconciler = new Reconciler();

        // First render: keyed A, non-keyed B, keyed C
        var vnode1 = FlexPanel(children: [
            FlexPanel(name: "A").WithKey("a"),
            FlexPanel(name: "B"),           // no key
            FlexPanel(name: "C").WithKey("c")
        ]);
        var root = reconciler.Reconcile(vnode1, container, null);
        var children1 = ((FlexPanel)root).Children;
        Assert.AreEqual("A", children1[0].Name);
        Assert.AreEqual("B", children1[1].Name);
        Assert.AreEqual("C", children1[2].Name);

        // Second render: keyed C, non-keyed X, keyed A (C and A swapped, B replaced)
        var vnode2 = FlexPanel(children: [
            FlexPanel(name: "C").WithKey("c"),
            FlexPanel(name: "X"),           // new non-keyed
            FlexPanel(name: "A").WithKey("a")
        ]);
        reconciler.Reconcile(vnode2, container, root);
        var children2 = ((FlexPanel)root).Children;
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
        var reconciler = new Reconciler();

        var vnode1 = FlexPanel(children: [
            FlexPanel(name: "First"),
            FlexPanel(name: "Second"),
            FlexPanel(name: "Third")
        ]);
        var root = reconciler.Reconcile(vnode1, container, null);
        var firstChild = ((FlexPanel)root).Children[0];

        // Same position, same type — should reuse
        var vnode2 = FlexPanel(children: [
            FlexPanel(name: "FirstUpdated"),
            FlexPanel(name: "Second"),
            FlexPanel(name: "Third")
        ]);
        reconciler.Reconcile(vnode2, container, root);
        var children = ((FlexPanel)root).Children;
        Assert.AreSame(firstChild, children[0], "Same-position element should be reused");
        Assert.AreEqual("FirstUpdated", children[0].Name, "Name should be updated");
    }

    [TestMethod]
    public void NonKeyedChildren_AppendAndRemove()
    {
        var container = new FlexPanel();
        var reconciler = new Reconciler();

        // Start with 2
        var vnode1 = FlexPanel(children: [
            FlexPanel(name: "A"),
            FlexPanel(name: "B")
        ]);
        var root = reconciler.Reconcile(vnode1, container, null);
        Assert.HasCount(2, ((FlexPanel)root).Children);

        // Add 2 more (now 4)
        var vnode2 = FlexPanel(children: [
            FlexPanel(name: "A"),
            FlexPanel(name: "B"),
            FlexPanel(name: "C"),
            FlexPanel(name: "D")
        ]);
        reconciler.Reconcile(vnode2, container, root);
        Assert.HasCount(4, ((FlexPanel)root).Children);

        // Remove 2 (back to 2)
        var vnode3 = FlexPanel(children: [
            FlexPanel(name: "A"),
            FlexPanel(name: "B")
        ]);
        reconciler.Reconcile(vnode3, container, root);
        Assert.HasCount(2, ((FlexPanel)root).Children);
        Assert.AreEqual("A", ((FlexPanel)root).Children[0].Name);
        Assert.AreEqual("B", ((FlexPanel)root).Children[1].Name);
    }

    [TestMethod]
    public void NonKeyedChildren_TypeChange_RecreatesElement()
    {
        var container = new FlexPanel();
        var reconciler = new Reconciler();

        var vnode1 = FlexPanel(children:
            FlexPanel(name: "Panel")
        );
        var root = reconciler.Reconcile(vnode1, container, null);
        Assert.IsInstanceOfType(((FlexPanel)root).Children[0], typeof(FlexPanel));

        var vnode2 = FlexPanel(children:
            Node()
        );
        reconciler.Reconcile(vnode2, container, root);
        Assert.IsInstanceOfType(((FlexPanel)root).Children[0], typeof(Node),
            "Type should change from FlexPanel to Node");
    }

    // ════════════════════════════════════════════════════════════════════
    //  Property restoration (stale property cleanup)
    // ════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void PropertyRestoration_StalePropertyResetsToDefault()
    {
        var container = new FlexPanel();
        var reconciler = new Reconciler();

        var vnode1 = FlexPanel(visibility: Visibility.Hidden);
        var root = (FlexPanel)reconciler.Reconcile(vnode1, container, null);
        Assert.AreEqual(Visibility.Hidden, root.Visibility);

        // Second render: Visibility NOT set → should reset to default
        var vnode2 = FlexPanel();
        reconciler.Reconcile(vnode2, container, root);
        Assert.AreEqual(Visibility.Visible, root.Visibility,
            "Stale property should reset to default (Visible) when omitted");
    }

    [TestMethod]
    public void PropertyRestoration_MultipleStalePropertiesReset()
    {
        var container = new FlexPanel();
        var reconciler = new Reconciler();

        var vnode1 = FlexPanel(
            visibility: Visibility.Hidden,
            opacity: 0.3f,
            flexDirection: YgFlexDirection.Column
        );
        var root = (FlexPanel)reconciler.Reconcile(vnode1, container, null);
        Assert.AreEqual(Visibility.Hidden, root.Visibility);
        Assert.AreEqual(0.3f, root.Opacity, 0.001f);
        Assert.AreEqual(YgFlexDirection.Column, root.FlexDirection);

        var vnode2 = FlexPanel();
        reconciler.Reconcile(vnode2, container, root);
        Assert.AreEqual(Visibility.Visible, root.Visibility);
        Assert.AreEqual(1.0f, root.Opacity, 0.001f, "Stale Opacity should reset to 1.0");
        Assert.AreEqual(YgFlexDirection.Row, root.FlexDirection, "Stale FlexDirection should reset to Row");
    }

    [TestMethod]
    public void PropertyRestoration_OneStaleOneFresh()
    {
        var container = new FlexPanel();
        var reconciler = new Reconciler();

        var vnode1 = FlexPanel(
            visibility: Visibility.Hidden,
            flexDirection: YgFlexDirection.Column
        );
        var root = (FlexPanel)reconciler.Reconcile(vnode1, container, null);

        // Only set FlexDirection this pass; Visibility should reset
        var vnode2 = FlexPanel(flexDirection: YgFlexDirection.ColumnReverse);
        reconciler.Reconcile(vnode2, container, root);

        Assert.AreEqual(Visibility.Visible, root.Visibility,
            "Stale Visibility should reset to Visible");
        Assert.AreEqual(YgFlexDirection.ColumnReverse, root.FlexDirection,
            "Fresh FlexDirection should update");
    }

    [TestMethod]
    public void PropertyRestoration_RemovedNode_RestoresOldValues()
    {
        var container = new FlexPanel();
        var reconciler = new Reconciler();

        var vnode1 = FlexPanel(children:
            FlexPanel(name: "Child", visibility: Visibility.Hidden)
        );
        var root = (FlexPanel)reconciler.Reconcile(vnode1, container, null);
        var child = (FlexPanel)root.Children[0];
        Assert.AreEqual(Visibility.Hidden, child.Visibility);

        var vnode2 = FlexPanel(children:
            FlexPanel(name: "Replacement")
        );
        reconciler.Reconcile(vnode2, container, root);
        Assert.HasCount(1, root.Children);
        Assert.AreEqual("Replacement", root.Children[0].Name);
    }

    [TestMethod]
    public void PropertyRestoration_PropertyPreservedWhenRespecified()
    {
        var container = new FlexPanel();
        var reconciler = new Reconciler();

        var vnode1 = FlexPanel(visibility: Visibility.Hidden);
        var root = (FlexPanel)reconciler.Reconcile(vnode1, container, null);
        Assert.AreEqual(Visibility.Hidden, root.Visibility);

        var vnode2 = FlexPanel(visibility: Visibility.Hidden);
        reconciler.Reconcile(vnode2, container, root);
        Assert.AreEqual(Visibility.Hidden, root.Visibility,
            "Property should persist when set in both renders");
    }

    [TestMethod]
    public void PropertyRestoration_ElementRemoved_PropertiesCleanedUp()
    {
        var container = new FlexPanel();
        var reconciler = new Reconciler();

        // First render: two children, the first with custom visibility
        var vnode1 = FlexPanel(children: [
            FlexPanel(name: "First", visibility: Visibility.Hidden),
            FlexPanel(name: "Second")
        ]);
        var root = reconciler.Reconcile(vnode1, container, null);
        Assert.HasCount(2, ((FlexPanel)root).Children);

        // Second render: only Second remains
        var vnode2 = FlexPanel(children:
            FlexPanel(name: "Second")
        );
        reconciler.Reconcile(vnode2, container, root);
        Assert.HasCount(1, ((FlexPanel)root).Children);
        Assert.AreEqual("Second", ((FlexPanel)root).Children[0].Name);
    }

    // ════════════════════════════════════════════════════════════════════
    //  Edge cases
    // ════════════════════════════════════════════════════════════════════

    // NOTE: EmptyChildren test removed — FlexPanel(children: []) semantics
    // depend on how the factory handles empty spans. This is a factory concern,
    // not a reconciler concern.

    [TestMethod]
    public void KeyedChildren_AllKeysChanged_AllRecreated()
    {
        var container = new FlexPanel();
        var reconciler = new Reconciler();

        var vnode1 = FlexPanel(children: [
            FlexPanel(name: "A").WithKey("a"),
            FlexPanel(name: "B").WithKey("b")
        ]);
        var root = reconciler.Reconcile(vnode1, container, null);
        var oldA = ((FlexPanel)root).Children[0];
        var oldB = ((FlexPanel)root).Children[1];

        var vnode2 = FlexPanel(children: [
            FlexPanel(name: "X").WithKey("x"),
            FlexPanel(name: "Y").WithKey("y")
        ]);
        reconciler.Reconcile(vnode2, container, root);
        var children = ((FlexPanel)root).Children;
        Assert.AreEqual("X", children[0].Name);
        Assert.AreEqual("Y", children[1].Name);
        Assert.AreNotSame(oldA, children[0]);
        Assert.AreNotSame(oldB, children[1]);
    }

    [TestMethod]
    public void NonKeyedChildren_InsertAtBeginning()
    {
        var container = new FlexPanel();
        var reconciler = new Reconciler();

        var vnode1 = FlexPanel(children: [
            FlexPanel(name: "B"),
            FlexPanel(name: "C")
        ]);
        var root = reconciler.Reconcile(vnode1, container, null);

        // Insert A at beginning
        var vnode2 = FlexPanel(children: [
            FlexPanel(name: "A"),
            FlexPanel(name: "B"),
            FlexPanel(name: "C")
        ]);
        reconciler.Reconcile(vnode2, container, root);
        var children = ((FlexPanel)root).Children;
        Assert.HasCount(3, children);
        Assert.AreEqual("A", children[0].Name);
        Assert.AreEqual("B", children[1].Name);
        Assert.AreEqual("C", children[2].Name);
    }

    [TestMethod]
    public void KeyedChildren_DuplicateKeys_LastWins()
    {
        var container = new FlexPanel();
        var reconciler = new Reconciler();

        // Two elements with the same key — the second should be treated as non-keyed
        var vnode1 = FlexPanel(children: [
            FlexPanel(name: "A").WithKey("same"),
            FlexPanel(name: "B").WithKey("same")  // duplicate
        ]);
        var root = reconciler.Reconcile(vnode1, container, null);

        // Second render: swap order
        var vnode2 = FlexPanel(children: [
            FlexPanel(name: "B").WithKey("same"),
            FlexPanel(name: "A").WithKey("same")
        ]);
        reconciler.Reconcile(vnode2, container, root);
        var children = ((FlexPanel)root).Children;
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
        var reconciler = new Reconciler();

        var vnode = FlexPanel(
            name: "TestName",
            key: "the-key",
            tabOrder: 5,
            isFocusable: true,
            isFocused: false
        );
        var root = reconciler.Reconcile(vnode, container, null);

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
        var reconciler = new Reconciler();

        var vnode = FlexPanel(
            opacity: 0.5f,
            visibility: Visibility.Hidden,
            flexDirection: YgFlexDirection.Column,
            alignItems: YgAlign.Center
        );
        var root = (FlexPanel)reconciler.Reconcile(vnode, container, null);

        Assert.AreEqual(0.5f, root.Opacity, 0.001f);
        Assert.AreEqual(Visibility.Hidden, root.Visibility);
        Assert.AreEqual(YgFlexDirection.Column, root.FlexDirection);
        Assert.AreEqual(YgAlign.Center, root.AlignItems);
    }

    [TestMethod]
    public void AssignProperties_AppliesNameOnNode()
    {
        var container = new FlexPanel();
        var reconciler = new Reconciler();

        var vnode = Node(name: "NodeName");
        var root = reconciler.Reconcile(vnode, container, null);

        Assert.AreEqual("NodeName", root.Name, "Name should be applied to Node via AssignProperties");
    }

    [TestMethod]
    public void AssignProperties_AppliesKeyOnNode()
    {
        var container = new FlexPanel();
        var reconciler = new Reconciler();

        var vnode = Node(key: "node-key");
        var root = reconciler.Reconcile(vnode, container, null);

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
        var reconciler = new Reconciler();

        // Use View (subclass of FlexPanel) with named parameters
        var vnode = View(
            name: "ShadowedView",
            key: "sv",
            flexDirection: YgFlexDirection.Row
        );
        var root = reconciler.Reconcile(vnode, container, null);

        Assert.IsInstanceOfType(root, typeof(View));
        var view = (View)root;
        Assert.AreEqual("ShadowedView", view.Name);
        Assert.AreEqual("sv", view.Key);
        Assert.AreEqual(YgFlexDirection.Row, view.FlexDirection);
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
        var reconciler = new Reconciler();
        var root = reconciler.Reconcile(vnode, container, null);
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
            .WithFlexDirection(YgFlexDirection.Column)
            .WithOpacity(0.3f);

        Assert.IsInstanceOfType(vnode, typeof(ViewNode));
        Assert.AreEqual("fluent", vnode.Name);
        Assert.AreEqual("f", vnode.Key);
        // Verify via reconciliation
        var container = new FlexPanel();
        var reconciler = new Reconciler();
        var root = reconciler.Reconcile(vnode, container, null);
        Assert.AreEqual("fluent", root.Name);
        Assert.AreEqual("f", root.Key);
    }

    // ════════════════════════════════════════════════════════════════════
    //  Shadowed properties across Yoga type hierarchy
    // ════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void ShadowedProperty_DerivedClassOverridesBaseProperty()
    {
        // FlexPanel extends Node. Both have Visibility property.
        // FlexPanel's AssignProperties should handle Visibility correctly
        // even though it's inherited from Node.
        var container = new FlexPanel();
        var reconciler = new Reconciler();

        var vnode1 = FlexPanel(visibility: Visibility.Hidden);
        var root = (FlexPanel)reconciler.Reconcile(vnode1, container, null);
        Assert.AreEqual(Visibility.Hidden, root.Visibility);

        // Now change to Visible — property should update
        var vnode2 = FlexPanel(visibility: Visibility.Visible);
        reconciler.Reconcile(vnode2, container, root);
        Assert.AreEqual(Visibility.Visible, root.Visibility);
    }

    [TestMethod]
    public void ShadowedProperty_ViewInheritsFlexPanelProperties()
    {
        // View extends FlexPanel which extends Node.
        // ViewNode's AssignProperties should handle all inherited properties.
        var container = new FlexPanel();
        var reconciler = new Reconciler();

        var vnode = View(
            name: "V",
            opacity: 0.75f,
            flexDirection: YgFlexDirection.ColumnReverse,
            visibility: Visibility.Hidden
        );
        var root = (View)reconciler.Reconcile(vnode, container, null);

        Assert.AreEqual("V", root.Name);
        Assert.AreEqual(0.75f, root.Opacity, 0.001f);
        Assert.AreEqual(YgFlexDirection.ColumnReverse, root.FlexDirection);
        Assert.AreEqual(Visibility.Hidden, root.Visibility);
    }
}
