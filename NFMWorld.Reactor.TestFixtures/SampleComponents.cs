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

// ════════════════════════════════════════════════════════════════════════
//  Context test components
// ════════════════════════════════════════════════════════════════════════

/// <summary>
/// Provides a <see cref="Context{T}"/> value to descendants and renders a child VNode.
/// </summary>
public class ContextProviderComponent : Component
{
    private readonly Context<string> _context;
    private readonly string _value;
    private readonly VNode _child;

    public ContextProviderComponent(Context<string> context, string value, VNode child)
    {
        _context = context;
        _value = value;
        _child = child;
    }

    protected override VNode Render()
    {
        ProvideContext(_context, _value);
        return FlexPanel(children: _child);
    }
}

/// <summary>
/// Reads a <see cref="Context{T}"/> value and renders a FlexPanel with Name = context value.
/// </summary>
public class ContextConsumerComponent : Component
{
    private readonly Context<string> _context;
    public string? LastReadValue { get; private set; }
    public int RenderCount { get; private set; }

    public ContextConsumerComponent(Context<string> context)
    {
        _context = context;
    }

    protected override VNode Render()
    {
        RenderCount++;
        LastReadValue = UseContext(_context);
        return FlexPanel().WithName(LastReadValue ?? "(null)");
    }
}

// ════════════════════════════════════════════════════════════════════════
//  Memoization test components
// ════════════════════════════════════════════════════════════════════════

/// <summary>
/// Component with a single int id input. Tracks render count for memo tests.
/// </summary>
public class MemoIdComponent : Component
{
    public int Id { get; }
    public int RenderCount { get; private set; }

    public MemoIdComponent(int id = 0)
    {
        Id = id;
    }

    protected override VNode Render()
    {
        RenderCount++;
        return FlexPanel().WithName($"id:{Id}");
    }
}

/// <summary>
/// Like <see cref="MemoIdComponent"/> but calls <see cref="Component.DisableMemo"/>
/// so it always re-renders regardless of input changes.
/// </summary>
public class NoMemoIdComponent : Component
{
    public int Id { get; }
    public int RenderCount { get; private set; }

    public NoMemoIdComponent(int id = 0)
    {
        Id = id;
        DisableMemo();
    }

    protected override VNode Render()
    {
        RenderCount++;
        return FlexPanel().WithName($"id:{Id}");
    }
}

/// <summary>
/// Reads a <see cref="Context{T}"/> value and renders it. Tracks render count for memo tests.
/// </summary>
public class MemoContextConsumerComponent : Component
{
    private readonly Context<string> _context;
    public string? LastReadValue { get; private set; }
    public int RenderCount { get; private set; }

    public MemoContextConsumerComponent(Context<string> context)
    {
        _context = context;
    }

    protected override VNode Render()
    {
        RenderCount++;
        LastReadValue = UseContext(_context);
        return FlexPanel().WithName(LastReadValue ?? "(null)");
    }
}

/// <summary>
/// Memoized component that passes a child VNode through without reading context.
/// Used to test that context changes propagate through memo-skipped intermediates.
/// </summary>
public class MemoPassthroughComponent : Component
{
    private readonly VNode _child;
    public int RenderCount { get; private set; }

    public MemoPassthroughComponent(VNode child)
    {
        _child = child;
    }

    protected override VNode Render()
    {
        RenderCount++;
        return FlexPanel(children: _child);
    }
}

/// <summary>
/// Like <see cref="ContextProviderComponent"/> but always re-renders (memo disabled).
/// Useful for testing context propagation through the Reconciler.
/// </summary>
public class AlwaysRenderProviderComponent : Component
{
    private readonly Context<string> _context;
    private readonly string _value;
    private readonly VNode _child;

    public AlwaysRenderProviderComponent(Context<string> context, string value, VNode child)
    {
        _context = context;
        _value = value;
        _child = child;
        DisableMemo();
    }

    protected override VNode Render()
    {
        ProvideContext(_context, _value);
        return FlexPanel(children: _child);
    }
}
