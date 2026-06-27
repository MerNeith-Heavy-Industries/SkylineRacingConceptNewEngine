namespace NFMWorld.Reactor;

/// <summary>
/// Marks a property of a <see cref="PropertyObject"/> as a bindable property that can be used in XAML bindings.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class PropertyAttribute : Attribute;