using WorldXaml.UI;
using WorldXaml.UI.Controls;
using WorldXaml.UI.Base;

namespace WorldXaml.UI.Styling;

/// <summary>
/// Defines a set of property values that can be applied to elements.
/// Styles can target elements by type and/or class.
/// </summary>
public class Style
{
    /// <summary>
    /// CSS-like selector string (e.g., "Button.primary", "TextBlock.h1").
    /// Parsed into TargetType and Classes.
    /// </summary>
    public virtual string? Selector
    {
        get => TargetType is null
            ? null
            : $"{TargetType.Name}{(Classes.Count > 0 ? "." + string.Join(".", Classes) : "")}";
        set
        {
            TargetType = null;
            Classes.Clear();
            Setters.Clear();

            if (string.IsNullOrWhiteSpace(value)) return;

            var split = value.Split('.', StringSplitOptions.TrimEntries);
            if (!string.IsNullOrEmpty(split[0]))
            {
                TargetType = Type.GetType(split[0]) ?? AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.ExportedTypes)
                    .FirstOrDefault(t => t.Name == split[0] && t.IsAssignableTo(typeof(Visual)));
            }
            else
            {
                TargetType = null;
            }

            foreach (var entry in split.AsSpan(1..))
            {
                Classes.Add(entry);
            }
        }
    }
    
    /// <summary>
    /// The type this style targets. Set explicitly or parsed from Selector.
    /// </summary>
    public Type? TargetType { get; set; }

    /// <summary>
    /// Class names from the selector that must be present on the element.
    /// </summary>
    public IList<string> Classes { get; } = new List<string>();

    /// <summary>
    /// Property setters applied to matching elements.
    /// </summary>
    public List<Setter> Setters { get; set; } = [];

    /// <summary>
    /// Local resources owned by this style.
    /// </summary>
    public StyleSheet? Resources { get; set; }

    /// <summary>
    /// Returns true when the given element matches this style's selector.
    /// </summary>
    public bool Matches(Visual element)
    {
        // Type check
        if (TargetType is not null)
        {
            if (!TargetType.IsInstanceOfType(element))
                return false;
        }

        // Class check
        if (Classes.Count > 0)
        {
            var elementClasses = element.Classes;
            foreach (var c in Classes)
            {
                if (!elementClasses.Contains(c))
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Apply this style's setters to the target element.
    /// </summary>
    public void Apply(Visual target)
    {
        foreach (var setter in Setters)
            setter.Apply(target);
    }
}

public class Style<T> : Style where T : Visual
{
    public override string? Selector
    {
        get => base.Selector;
        set
        {
            TargetType = null;
            Classes.Clear();
            Setters.Clear();

            if (string.IsNullOrWhiteSpace(value)) return;

            var split = value.Split('.', StringSplitOptions.TrimEntries);
            foreach (var entry in split.AsSpan(1..))
            {
                Classes.Add(entry);
            }
        }
    }

    public Style()
    {
        TargetType = typeof(T);
    }
}