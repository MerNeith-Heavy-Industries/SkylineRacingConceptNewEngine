#if DEBUG
using WorldXaml.UI.Base;

[assembly: System.Reflection.Metadata.MetadataUpdateHandlerAttribute(typeof(NFMWorld.Reactor.HotReloadService))]

namespace NFMWorld.Reactor;

public static class HotReloadService
{
    public static event Action<Type[]?>? UpdateApplicationEvent;

    internal static void ClearCache(Type[]? types)
    {
        Logging.Debug("Hot Reload Service event - ClearCache");
    }

    internal static void UpdateApplication(Type[]? types)
    {
        UpdateApplicationEvent?.Invoke(types);

        Logging.Debug("Hot Reload Service event - UpdateApplication");
    }
}
#else
public static class HotReloadService
{
    public static event Action<Type[]?>? UpdateApplicationEvent;
}
#endif
