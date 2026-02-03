using Avalonia.Markup.Xaml;

namespace nfm_world.ui.yoga.xaml;

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
