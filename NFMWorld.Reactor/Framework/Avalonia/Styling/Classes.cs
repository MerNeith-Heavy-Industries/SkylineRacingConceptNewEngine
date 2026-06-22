using WorldXaml.ObservableCollections;

namespace WorldXaml.UI.Controls;

/// <summary>
/// A collection of CSS-like class names for an element.
/// Styles can target elements by class name.
/// Usage: Button(classes: "primary large") or element.Classes.Add("primary").
/// </summary>
public class Classes : NonSynchronizedObservableList<string>
{
    /// <summary>
    /// Batch-add classes from a space-separated string.
    /// </summary>
    public void AddRange(string? classes)
    {
        if (string.IsNullOrWhiteSpace(classes)) return;
        foreach (var c in classes.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            Add(c);
    }

    /// <summary>
    /// Returns true if the element has ALL listed classes.
    /// </summary>
    public bool HasAll(params string[] names)
        => names.All(Contains);

    /// <summary>
    /// Returns true if the element has ANY of the listed classes.
    /// </summary>
    public bool HasAny(params string[] names)
        => names.Any(Contains);
}
