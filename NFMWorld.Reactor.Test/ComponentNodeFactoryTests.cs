using NFMWorld.Reactor.TestFixtures;
using static NFMWorld.Reactor.TestFixtures.Nodes;
using static WorldXaml.UI.Yoga.Nodes;
using WorldXaml.UI.Yoga;

namespace NFMWorld.Reactor.Test;

[TestClass]
public class ComponentNodeFactoryTests
{
    /// <summary>
    /// Verifies that the generated component factory creates a node with the correct name.
    /// </summary>
    [TestMethod]
    public void TitleComponent_Factory_CreatesNodeWithName()
    {
        var node = TitleComponent(title: "Hello");

        Assert.IsNotNull(node);
        Assert.AreEqual(typeof(TitleComponent), node.ComponentType);
        Assert.AreEqual("Hello", ((TitleComponent)node.CreateComponent()).Title);
    }

    /// <summary>
    /// Verifies that a component with no args can be created via generated factory.
    /// </summary>
    [TestMethod]
    public void EmptyComponent_Factory_CreatesNode()
    {
        var node = EmptyComponent();

        Assert.IsNotNull(node);
        Assert.AreEqual(typeof(EmptyComponent), node.ComponentType);
    }

    /// <summary>
    /// Verifies that default values are used when optional parameters are not provided.
    /// </summary>
    [TestMethod]
    public void CounterComponent_DefaultValue_UsedWhenNotProvided()
    {
        var node = CounterComponent(label: "Score");

        var comp = (CounterComponent)node.CreateComponent();
        Assert.AreEqual("Score", comp.Label);
        Assert.AreEqual(0, comp.InitialValue); // default
    }

    /// <summary>
    /// Verifies that explicit values override defaults.
    /// </summary>
    [TestMethod]
    public void CounterComponent_ExplicitValue_OverridesDefault()
    {
        var node = CounterComponent(label: "Score", initialValue: 42);

        var comp = (CounterComponent)node.CreateComponent();
        Assert.AreEqual("Score", comp.Label);
        Assert.AreEqual(42, comp.InitialValue);
    }

    /// <summary>
    /// Verifies nullable string parameters work with null default.
    /// </summary>
    [TestMethod]
    public void OptionalTitleComponent_NullDefault_Works()
    {
        var node = OptionalTitleComponent();

        var comp = (OptionalTitleComponent)node.CreateComponent();
        Assert.IsNull(comp.Subtitle);
    }

    /// <summary>
    /// Verifies boolean and float defaults are correctly resolved.
    /// </summary>
    [TestMethod]
    public void ToggleComponent_Defaults_AreCorrect()
    {
        var node = ToggleComponent();

        var comp = (ToggleComponent)node.CreateComponent();
        Assert.IsTrue(comp.Enabled);
        Assert.AreEqual(1.0f, comp.Opacity);
    }

    /// <summary>
    /// Verifies that With* methods on the generated node override factory values.
    /// </summary>
    [TestMethod]
    public void CounterComponent_WithMethods_OverrideFactoryArgs()
    {
        var node = CounterComponent(label: "Old")
            .WithLabel("New")
            .WithInitialValue(99);

        var comp = (CounterComponent)node.CreateComponent();
        Assert.AreEqual("New", comp.Label);
        Assert.AreEqual(99, comp.InitialValue);
    }

    /// <summary>
    /// Verifies that a component can be created and rendered via the Reconciler.
    /// </summary>
    [TestMethod]
    public void ComponentNode_RenderCount_Increments()
    {
        var node = EmptyComponent();
        var comp = (EmptyComponent)node.CreateComponent();

        // Mount the component into a container (calls Render internally)
        var container = new FlexPanel();
        comp.Mount(container);
        Assert.AreEqual(1, comp.RenderCount);

        // Update triggers another render
        comp.Update();
        Assert.AreEqual(2, comp.RenderCount);
    }

    /// <summary>
    /// Verifies that ComponentNodeFactory.Create fallback works for components without generated wrappers.
    /// </summary>
    [TestMethod]
    public void ComponentNodeFactory_Create_UntypedFallback()
    {
        var node = ComponentNodeFactory.Create<EmptyComponent>();

        Assert.IsNotNull(node);
        Assert.AreEqual(typeof(EmptyComponent), node.ComponentType);
        Assert.IsInstanceOfType(node.CreateComponent(), typeof(EmptyComponent));
    }
}
