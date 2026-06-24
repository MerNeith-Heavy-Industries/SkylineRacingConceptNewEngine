using WorldXaml.UI;
using WorldXaml.UI.Controls;
using WorldXaml.UI.Base;

namespace WorldXaml.UI.Styling;

public interface IStyle : IStyleNode
 {
    /// <summary>
    /// Gets a collection of child styles.
    /// </summary>
    IReadOnlyList<IStyle> Children { get; }
}

/// <summary>
/// Applies a value to a property. Used inside Style or as standalone.
/// </summary>
public class Setter
{
    /// <summary>
    /// The property setter.
    /// </summary>
    public Action<Visual, object?>? PropertySetter { get; set; }

    /// <summary>
    /// The value to assign.
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Apply this setter to the given target element.
    /// </summary>
    public void Apply(Visual target)
    {
        PropertySetter?.Invoke(target, Value);
    }
}

/// <summary>
/// Applies a value to a property. Used inside Style or as standalone.
/// </summary>
public class Setter<TVisual, TProp> : Setter where TVisual : Visual
{
    /// <summary>
    /// The property setter.
    /// </summary>
    public new Action<TVisual, TProp>? PropertySetter { get; set; }

    public Setter()
    {
        base.PropertySetter = (v, val) => PropertySetter?.Invoke((TVisual)v, (TProp)val!);
    }

    /// <summary>
    /// The value to assign.
    /// </summary>
    public new required TProp Value
    {
        get => (TProp)base.Value!;
        set => base.Value = value;
    }
}
