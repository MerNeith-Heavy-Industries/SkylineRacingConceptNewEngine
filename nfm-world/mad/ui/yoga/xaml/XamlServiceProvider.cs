namespace nfm_world.ui.yoga.xaml;

/// <summary>
/// Provides access to the root object during XAML instantiation.
/// </summary>
public interface IRootObjectProvider
{
    /// <summary>
    /// Gets the root object being constructed.
    /// </summary>
    object? RootObject { get; }

    /// <summary>
    /// Gets the intermediate root object (used during nested object construction).
    /// </summary>
    object? IntermediateRootObject { get; }
}

/// <summary>
/// Provides URI context during XAML loading.
/// </summary>
public interface IUriContext
{
    /// <summary>
    /// Gets or sets the base URI of the current XAML document.
    /// </summary>
    Uri? BaseUri { get; set; }
}

/// <summary>
/// Provides information about the target of a markup extension.
/// </summary>
public interface IProvideValueTarget
{
    /// <summary>
    /// Gets the target object where a value is being set.
    /// </summary>
    object? TargetObject { get; }

    /// <summary>
    /// Gets the target property where a value is being set.
    /// </summary>
    object? TargetProperty { get; }
}

/// <summary>
/// Interface for adding child elements to a parent.
/// </summary>
public interface IAddChild
{
    void AddChild(object child);
    void AddText(string text);
}

/// <summary>
/// Generic interface for adding typed child elements.
/// </summary>
public interface IAddChild<in T>
{
    void AddChild(T child);
    void AddText(string text);
}

/// <summary>
/// Simple service provider implementation for XAML runtime.
/// </summary>
public class XamlServiceProvider : IServiceProvider
{
    private readonly Dictionary<Type, object> _services = new();

    public void AddService(Type serviceType, object service)
    {
        _services[serviceType] = service;
    }

    public void AddService<T>(T service) where T : notnull
    {
        _services[typeof(T)] = service;
    }

    public object? GetService(Type serviceType)
    {
        return _services.TryGetValue(serviceType, out var service) ? service : null;
    }
}

/// <summary>
/// Root object provider implementation.
/// </summary>
public class RootObjectProviderImpl : IRootObjectProvider
{
    public object? RootObject { get; set; }
    public object? IntermediateRootObject { get; set; }
}

/// <summary>
/// URI context implementation.
/// </summary>
public class UriContextImpl : IUriContext
{
    public Uri? BaseUri { get; set; }
}

/// <summary>
/// Provide value target implementation.
/// </summary>
public class ProvideValueTargetImpl : IProvideValueTarget
{
    public object? TargetObject { get; set; }
    public object? TargetProperty { get; set; }
}
