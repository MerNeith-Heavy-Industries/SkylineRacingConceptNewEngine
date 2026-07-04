using System.ComponentModel;
using static NFMWorld.Reactor.Nodes;

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
public class TitleComponent(string title) : Component
{
    public string Title { get; } = title;

    protected override VNode Render()
        => FlexPanel().WithName(Title);
}

/// <summary>
/// Component with multiple parameters including a value type with a default.
/// </summary>
public class CounterComponent(string label, int initialValue = 0) : Component
{
    public string Label { get; } = label;
    public int InitialValue { get; } = initialValue;

    protected override VNode Render()
        => FlexPanel().WithName($"{Label}:{InitialValue}");
}

/// <summary>
/// Component with a nullable reference type parameter.
/// </summary>
public class OptionalTitleComponent(string? subtitle = null) : Component
{
    public string? Subtitle { get; } = subtitle;

    protected override VNode Render()
        => FlexPanel().WithName(Subtitle ?? "(none)");
}

/// <summary>
/// Component with a boolean and float parameter (value types with defaults).
/// </summary>
public class ToggleComponent(bool enabled = true, float opacity = 1.0f) : Component
{
    public bool Enabled { get; } = enabled;
    public float Opacity { get; } = opacity;

    protected override VNode Render()
        => FlexPanel().WithName($"toggle:{Enabled}:{Opacity}");
}

/// <summary>
/// Component that renders children passed via constructor (hybrid pattern).
/// </summary>
public class WrapperComponent(VNode child) : Component
{
    public VNode Child { get; } = child;

    protected override VNode Render()
        => Child;
}

// ════════════════════════════════════════════════════════════════════════
//  Context test components
// ════════════════════════════════════════════════════════════════════════

/// <summary>
/// Provides a <see cref="Context{T}"/> value to descendants and renders a child VNode.
/// </summary>
public class ContextProviderComponent(Context<string> context, string value, VNode child) : Component
{
    protected override VNode Render()
    {
        ProvideContext(context, value);
        return FlexPanel(children: child);
    }
}

/// <summary>
/// Reads a <see cref="Context{T}"/> value and renders a FlexPanel with Name = context value.
/// </summary>
public class ContextConsumerComponent(Context<string> context) : Component
{
    public string? LastReadValue { get; private set; }
    public int RenderCount { get; private set; }

    protected override VNode Render()
    {
        RenderCount++;
        LastReadValue = UseContext(context);
        return FlexPanel().WithName(LastReadValue ?? "(null)");
    }
}

// ════════════════════════════════════════════════════════════════════════
//  Memoization test components
// ════════════════════════════════════════════════════════════════════════

/// <summary>
/// Component with a single int id input. Tracks render count for memo tests.
/// </summary>
public class MemoIdComponent(int id = 0) : Component
{
    public int Id { get; } = id;
    public int RenderCount { get; private set; }

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
public class MemoContextConsumerComponent(Context<string> context) : Component
{
    public string? LastReadValue { get; private set; }
    public int RenderCount { get; private set; }

    protected override VNode Render()
    {
        RenderCount++;
        LastReadValue = UseContext(context);
        return FlexPanel().WithName(LastReadValue ?? "(null)");
    }
}

/// <summary>
/// Memoized component that passes a child VNode through without reading context.
/// Used to test that context changes propagate through memo-skipped intermediates.
/// </summary>
public class MemoPassthroughComponent(VNode child) : Component
{
    public int RenderCount { get; private set; }

    protected override VNode Render()
    {
        RenderCount++;
        return FlexPanel(children: child);
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

// ════════════════════════════════════════════════════════════════════════
//  HUD layout test components — simulates HudHost + LapTimerSplitsView
// ════════════════════════════════════════════════════════════════════════

/// <summary>
/// A record used as context state for HUD layout tests.
/// </summary>
public record HudLayoutState(int CurrentLap = 0, int TotalLaps = 0)
{
    public static readonly Context<HudLayoutState> Context = new(new HudLayoutState());
}

/// <summary>
/// Simulates the HudHost pattern: provides <see cref="HudLayoutState"/> context
/// and wraps each child in an absolutely-positioned full-viewport FlexPanel.
/// </summary>
public class HudHostTestComponent(HudLayoutState state, params VNode[] children) : Component
{
    public int RenderCount { get; private set; }

    protected override VNode Render()
    {
        RenderCount++;
        ProvideContext(HudLayoutState.Context, state);
        return View(flex: 1, children: [
            ..children.Select(child =>
                FlexPanel(position: Position.Absolute, top: 0, right: 0, left: 0, bottom: 0, children: child))
        ]);
    }
}

/// <summary>
/// Simulates LapTimerSplitsView: absolutely positioned at top-left,
/// reads <see cref="HudLayoutState"/> from context and displays lap info.
/// </summary>
public class LapCounterTestComponent : Component
{
    public int RenderCount { get; private set; }
    public int LastCurrentLap { get; private set; }
    public int LastTotalLaps { get; private set; }

    protected override VNode Render()
    {
        RenderCount++;
        var hud = UseContext(HudLayoutState.Context);
        LastCurrentLap = hud.CurrentLap;
        LastTotalLaps = hud.TotalLaps;
        return FlexPanel(
            name: $"lap:{hud.CurrentLap}/{hud.TotalLaps}",
            position: Position.Absolute,
            top: 0,
            left: 0,
            padding: 10f
        );
    }
}
