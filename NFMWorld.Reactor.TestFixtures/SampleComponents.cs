using System.ComponentModel;
using WorldXaml.UI.Yoga;
using static WorldXaml.UI.Yoga.Nodes;

namespace NFMWorld.Reactor.TestFixtures;

/// <summary>
/// Component with no constructor parameters — tests the empty factory case.
/// </summary>
public class EmptyComponent : Component
{
    public int RenderCount { get; private set; }

    protected override VNode Render()
    {
        RenderCount++;
        return FlexPanel();
    }
}

/// <summary>
/// Component with a single required string parameter.
/// </summary>
public class TitleComponent : Component
{
    public string Title { get; }

    public TitleComponent(string title)
    {
        Title = title;
    }

    protected override VNode Render()
        => FlexPanel().WithName(Title);
}

/// <summary>
/// Component with multiple parameters including a value type with a default.
/// </summary>
public class CounterComponent : Component
{
    public string Label { get; }
    public int InitialValue { get; }

    public CounterComponent(string label, int initialValue = 0)
    {
        Label = label;
        InitialValue = initialValue;
    }

    protected override VNode Render()
        => FlexPanel().WithName($"{Label}:{InitialValue}");
}

/// <summary>
/// Component with a nullable reference type parameter.
/// </summary>
public class OptionalTitleComponent : Component
{
    public string? Subtitle { get; }

    public OptionalTitleComponent(string? subtitle = null)
    {
        Subtitle = subtitle;
    }

    protected override VNode Render()
        => FlexPanel().WithName(Subtitle ?? "(none)");
}

/// <summary>
/// Component with a boolean and float parameter (value types with defaults).
/// </summary>
public class ToggleComponent : Component
{
    public bool Enabled { get; }
    public float Opacity { get; }

    public ToggleComponent(bool enabled = true, float opacity = 1.0f)
    {
        Enabled = enabled;
        Opacity = opacity;
    }

    protected override VNode Render()
        => FlexPanel().WithName($"toggle:{Enabled}:{Opacity}");
}

/// <summary>
/// Component that renders children passed via constructor (hybrid pattern).
/// </summary>
public class WrapperComponent : Component
{
    public VNode Child { get; }

    public WrapperComponent(VNode child)
    {
        Child = child;
    }

    protected override VNode Render()
        => Child;
}
