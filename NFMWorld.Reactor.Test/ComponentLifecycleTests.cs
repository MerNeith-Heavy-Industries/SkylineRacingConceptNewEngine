using NFMWorld.Reactor.TestFixtures;
using static WorldXaml.UI.Yoga.Nodes;
using static NFMWorld.Reactor.TestFixtures.Nodes;
using WorldXaml.UI.Yoga;

namespace NFMWorld.Reactor.Test;

[TestClass]
public class ComponentLifecycleTests
{
    // ════════════════════════════════════════════════════════════════════
    //  Update() — unmounts stale child components (tree-hosted path)
    // ════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Update_UnmountsStaleChildComponent()
    {
        var parent = new ToggleChildComponent(showChild: true);
        Mount(parent);
        var child = parent.ChildInstance;
        Assert.IsNotNull(child, "Child should exist when showChild=true");
        Assert.IsTrue(child!.IsMounted, "Child should be mounted");

        parent.ShowChild = false;
        parent.Update();
        Assert.IsFalse(child.IsMounted, "Child should be unmounted after removal via Update()");
    }

    [TestMethod]
    public void Update_PersistingChildStaysMounted()
    {
        var parent = new ToggleChildComponent(showChild: true);
        Mount(parent);
        var child = parent.ChildInstance!;
        Assert.IsTrue(child.IsMounted);

        parent.Update();
        Assert.IsTrue(child.IsMounted, "Persisting child should remain mounted");
    }

    [TestMethod]
    public void Update_MultipleCycles_StaleComponentsCleanedUp()
    {
        var parent = new ToggleChildComponent(showChild: true);
        Mount(parent);

        var firstChild = parent.ChildInstance!;
        Assert.IsTrue(firstChild.IsMounted);

        // Cycle 1: toggle off
        parent.ShowChild = false;
        parent.Update();
        Assert.IsFalse(firstChild.IsMounted);

        // Cycle 2: toggle on (new child)
        parent.ShowChild = true;
        parent.Update();
        var secondChild = parent.ChildInstance!;
        Assert.IsTrue(secondChild.IsMounted);
        Assert.AreNotSame(firstChild, secondChild);

        // Cycle 3: toggle off again
        parent.ShowChild = false;
        parent.Update();
        Assert.IsFalse(secondChild.IsMounted);
    }

    [TestMethod]
    public void Update_ReplacesChild_OldUnmountedNewMounted()
    {
        var parent = new ToggleChildComponent(showChild: true);
        Mount(parent);
        var firstChild = parent.ChildInstance!;
        Assert.IsTrue(firstChild.IsMounted);

        parent.ShowChild = false;
        parent.Update();
        Assert.IsFalse(firstChild.IsMounted, "First child should be unmounted");

        parent.ShowChild = true;
        parent.Update();
        var secondChild = parent.ChildInstance!;
        Assert.IsTrue(secondChild.IsMounted, "New child should be mounted");
        Assert.AreNotSame(firstChild, secondChild, "Should be a different instance");
    }

    // ════════════════════════════════════════════════════════════════════
    //  Reconciler path — stale component cleanup (same behavior, direct API)
    // ════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Reconcile_UnmountsStaleChildComponent()
    {
        var container = new FlexPanel();
        var reconciler = new Reconciler();

        var vnode1 = View(children: EmptyComponent());
        var root = reconciler.Reconcile(vnode1, container, null);
        var childNode1 = (EmptyComponentNode)vnode1.Children![0];
        var child = (EmptyComponent)childNode1.Instance!;
        Assert.IsTrue(child.IsMounted);

        var vnode2 = View();
        reconciler.Reconcile(vnode2, container, root);
        Assert.IsFalse(child.IsMounted, "Reconcile should unmount removed child");
    }

    [TestMethod]
    public void Reconcile_PersistingChildStaysMounted()
    {
        var container = new FlexPanel();
        var reconciler = new Reconciler();

        var vnode1 = View(children: EmptyComponent());
        var root = reconciler.Reconcile(vnode1, container, null);
        var childNode1 = (EmptyComponentNode)vnode1.Children![0];
        var child = (EmptyComponent)childNode1.Instance!;
        Assert.IsTrue(child.IsMounted);

        var vnode2 = View(children: EmptyComponent());
        reconciler.Reconcile(vnode2, container, root);
        Assert.IsTrue(child.IsMounted, "Persisting child should stay mounted");
    }

    // ════════════════════════════════════════════════════════════════════
    //  Update() — snapshot rotation (per-property staleness)
    // ════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Update_StalePropertyResetsAfterUpdate()
    {
        var comp = new ToggleVisibilityComponent(visible: Visibility.Hidden);
        Mount(comp);
        Assert.AreEqual(Visibility.Hidden, ((FlexPanel)comp.NativeRoot!).Visibility);

        comp.Visible = null;
        comp.Update();
        Assert.AreEqual(Visibility.Visible, ((FlexPanel)comp.NativeRoot!).Visibility,
            "Stale property should reset after Update via FinishPass snapshot rotation");
    }

    [TestMethod]
    public void Update_PropertyPreservedWhenRespecifiedAcrossMultipleUpdates()
    {
        var comp = new ToggleVisibilityComponent(visible: Visibility.Hidden);
        Mount(comp);

        comp.Visible = Visibility.Visible;
        comp.Update();
        Assert.AreEqual(Visibility.Visible, ((FlexPanel)comp.NativeRoot!).Visibility);

        comp.Visible = Visibility.Visible;
        comp.Update();
        Assert.AreEqual(Visibility.Visible, ((FlexPanel)comp.NativeRoot!).Visibility,
            "Property should persist across multiple updates");
    }

    [TestMethod]
    public void Update_AlternatingProperties_ResetCorrectly()
    {
        var comp = new ToggleVisibilityComponent(visible: Visibility.Hidden);
        Mount(comp);
        Assert.AreEqual(Visibility.Hidden, ((FlexPanel)comp.NativeRoot!).Visibility);

        comp.Visible = null;
        comp.Update();
        Assert.AreEqual(Visibility.Visible, ((FlexPanel)comp.NativeRoot!).Visibility);

        comp.Visible = Visibility.Hidden;
        comp.Update();
        Assert.AreEqual(Visibility.Hidden, ((FlexPanel)comp.NativeRoot!).Visibility);

        comp.Visible = null;
        comp.Update();
        Assert.AreEqual(Visibility.Visible, ((FlexPanel)comp.NativeRoot!).Visibility);
    }

    // ════════════════════════════════════════════════════════════════════
    //  Helpers
    // ════════════════════════════════════════════════════════════════════

    private static void Mount(Component comp) => comp.Mount(new FlexPanel());

    // ════════════════════════════════════════════════════════════════════
    //  Test Components
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Parent component that conditionally renders an EmptyComponent child.
    /// Uses DisableMemo so re-renders are predictable.
    /// </summary>
    private class ToggleChildComponent : Component
    {
        public bool ShowChild { get; set; }
        private EmptyComponentNode? _childNode;
        public EmptyComponent? ChildInstance => _childNode?.Instance as EmptyComponent;

        public ToggleChildComponent(bool showChild)
        {
            ShowChild = showChild;
            DisableMemo();
        }

        protected override VNode Render()
        {
            if (ShowChild)
            {
                _childNode = EmptyComponent();
                return FlexPanel(children: _childNode);
            }

            _childNode = null;
            return FlexPanel();
        }
    }

    /// <summary>
    /// Renders a FlexPanel with an optional Visibility value.
    /// Uses DisableMemo so Render() is always called.
    /// </summary>
    private class ToggleVisibilityComponent : Component
    {
        public Visibility? Visible { get; set; }

        public ToggleVisibilityComponent(Visibility? visible = null)
        {
            Visible = visible;
            DisableMemo();
        }

        protected override VNode Render()
        {
            if (Visible.HasValue)
                return FlexPanel(visibility: Visible.Value);
            return FlexPanel();
        }
    }
}
