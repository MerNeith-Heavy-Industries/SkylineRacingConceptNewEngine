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
    /// The WorldXamlProperty to set.
    /// </summary>
    public Property? Property { get; set; }

    /// <summary>
    /// The value to assign.
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Apply this setter to the given target element.
    /// </summary>
    public void Apply(BindableObject target)
    {
        if (Property is null) return;
        target.SetBoxedValue(Property, Value);
    }
}
