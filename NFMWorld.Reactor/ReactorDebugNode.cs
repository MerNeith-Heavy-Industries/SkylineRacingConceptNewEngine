using System.Reflection;

namespace NFMWorld.Reactor;

/// <summary>
/// Per-frame snapshot of a VDOM node captured during reconciliation.
/// Used by <see cref="YogaDebugger"/> (and similar tools) to visualize
/// the Reactor virtual DOM tree — including Component boundaries and
/// constructor inputs — alongside native Yoga layout data.
/// </summary>
/// <remarks>
/// Only populated in DEBUG builds. The tree is rebuilt every frame
/// during reconciliation and exposed via <see cref="NodeDebugger"/>.
/// </remarks>
public sealed class ReactorDebugNode
{
    /// <summary>
    /// Kind of VNode this debug node represents.
    /// </summary>
    public ReactorDebugNodeType Type { get; }

    /// <summary>
    /// For <see cref="ReactorDebugNodeType.VisualVNode"/>: the native
    /// <see cref="Visual"/> subclass (e.g. <c>typeof(FlexPanel)</c>).
    /// For <see cref="ReactorDebugNodeType.ComponentNode"/>: <c>null</c>.
    /// </summary>
    public Type? NativeType { get; }

    /// <summary>
    /// For <see cref="ReactorDebugNodeType.ComponentNode"/>: the
    /// <see cref="Component"/> subclass type.
    /// For <see cref="ReactorDebugNodeType.VisualVNode"/>: <c>null</c>.
    /// </summary>
    public Type? ComponentType { get; }

    /// <summary>
    /// The VNode's <see cref="VisualVNode.Name"/> (for VisualVNodes)
    /// or the component type's short name (for ComponentNodes).
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// The VNode's <see cref="VNode.Key"/> value, if any.
    /// </summary>
    public object? Key { get; }

    /// <summary>
    /// The native <see cref="Visual"/> created/reused during reconciliation.
    /// Carries Yoga layout data (position, size, box model).
    /// <c>null</c> for ComponentNodes whose render output hasn't been
    /// reconciled yet (briefly, during tree construction).
    /// </summary>
    public Visual? NativeVisual { get; private set; }

    /// <summary>
    /// For <see cref="ReactorDebugNodeType.ComponentNode"/>: the
    /// constructor argument values from <see cref="ComponentNode.GetInputs"/>.
    /// <c>null</c> if the component has no constructor parameters.
    /// </summary>
    public object?[]? ComponentInputs { get; }

    /// <summary>
    /// Child nodes in the VDOM tree.
    /// </summary>
    public List<ReactorDebugNode> Children { get; } = [];

    private string? _cachedInputsDisplay;
    private object?[]? _cachedInputs;

    /// <summary>
    /// Creates a debug node for a <see cref="VisualVNode"/>.
    /// </summary>
    public ReactorDebugNode(
        Type nativeType,
        string? name,
        object? key,
        Visual? nativeVisual)
    {
        Type = ReactorDebugNodeType.VisualVNode;
        NativeType = nativeType;
        Name = name;
        Key = key;
        NativeVisual = nativeVisual;
    }

    /// <summary>
    /// Creates a debug node for a <see cref="ComponentNode"/>.
    /// </summary>
    public ReactorDebugNode(
        Type componentType,
        object?[]? componentInputs,
        Visual? nativeVisual)
    {
        Type = ReactorDebugNodeType.ComponentNode;
        ComponentType = componentType;
        Name = componentType.Name;
        ComponentInputs = componentInputs;
        NativeVisual = nativeVisual;
        _cachedInputs = componentInputs;
    }

    /// <summary>
    /// Updates the native visual reference. Used for ComponentNodes
    /// where the rendered root isn't known until after
    /// <see cref="Component.RenderViaReconciler"/> returns.
    /// </summary>
    internal void UpdateNativeVisual(Visual? visual)
    {
        NativeVisual = visual;
    }

    /// <summary>
    /// Returns a human-readable display string for this node.
    /// For VisualVNodes: <c>"name [FlexPanel]"</c> or <c>"[FlexPanel]"</c>.
    /// For ComponentNodes: <c>"MyComponent(foo=42, bar=hello)"</c> or <c>"MyComponent"</c>.
    /// </summary>
    public string ToDisplayString()
    {
        return Type switch
        {
            ReactorDebugNodeType.VisualVNode =>
                Name is not null
                    ? $"{Name} [{NativeType?.Name ?? "?"}]"
                    : $"[{NativeType?.Name ?? "?"}]",

            ReactorDebugNodeType.ComponentNode =>
                FormatComponentDisplay(),

            _ => "?"
        };
    }

    private string FormatComponentDisplay()
    {
        if (ComponentType is null) return "?";

        if (ComponentInputs is null or { Length: 0 })
            return ComponentType.Name;

        // Cache the formatted string — inputs are value types and
        // don't change after the debug node is created.
        if (_cachedInputsDisplay is not null && _cachedInputs == ComponentInputs)
            return _cachedInputsDisplay;

        var paramNames = GetConstructorParameterNames();
        var sb = new System.Text.StringBuilder(ComponentType.Name);
        sb.Append('(');
        for (int i = 0; i < ComponentInputs.Length; i++)
        {
            if (i > 0) sb.Append(", ");
            if (i < paramNames.Length)
                sb.Append(paramNames[i]);
            else
                sb.Append($"arg{i}");
            sb.Append('=');
            sb.Append(FormatValue(ComponentInputs[i]));
        }
        sb.Append(')');

        _cachedInputsDisplay = sb.ToString();
        _cachedInputs = ComponentInputs;
        return _cachedInputsDisplay;
    }

    private string[]? _cachedParamNames;
    private Type? _cachedParamOwnerType;

    private string[] GetConstructorParameterNames()
    {
        if (ComponentType is null) return [];

        if (_cachedParamNames is not null && _cachedParamOwnerType == ComponentType)
            return _cachedParamNames;

        var ctors = ComponentType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        // Pick the constructor whose parameter count matches
        foreach (var ctor in ctors)
        {
            var @params = ctor.GetParameters();
            if (@params.Length == ComponentInputs!.Length)
            {
                _cachedParamNames = new string[@params.Length];
                for (int i = 0; i < @params.Length; i++)
                    _cachedParamNames[i] = @params[i].Name ?? $"arg{i}";
                _cachedParamOwnerType = ComponentType;
                return _cachedParamNames;
            }
        }

        _cachedParamNames = [];
        _cachedParamOwnerType = ComponentType;
        return _cachedParamNames;
    }

    private static string FormatValue(object? value)
    {
        return value switch
        {
            null => "null",
            string s => $"\"{s}\"",
            bool b => b ? "true" : "false",
            float f => f.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture),
            double d => d.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture),
            _ => value.ToString() ?? "?"
        };
    }
}

/// <summary>
/// Distinguishes between the two kinds of VNodes in the debug tree.
/// </summary>
public enum ReactorDebugNodeType
{
    /// <summary>A <see cref="VisualVNode"/> (FlexPanel, Node, View, etc.).</summary>
    VisualVNode,

    /// <summary>A <see cref="ComponentNode"/> wrapping a user <see cref="Component"/>.</summary>
    ComponentNode
}
