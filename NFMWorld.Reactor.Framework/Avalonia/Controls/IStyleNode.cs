using WorldXaml.UI.Controls;

namespace WorldXaml.UI.Controls;

/// <summary>
/// An element that owns a <see cref="StyleSheet"/>.
/// Resource lookup walks the logical tree through IResourceNode parents.
/// </summary>
public interface IStyleNode
 {
    /// <summary>
    /// Local resources for this element. Set by XAML or code.
    /// </summary>
    StyleSheet? Styles { get; set; }

    /// <summary>
    /// True when the element has resources (either locally or inherited).
    /// </summary>
    bool HasResources => Styles is { Count: > 0 };
}
