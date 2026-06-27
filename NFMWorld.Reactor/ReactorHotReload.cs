#if DEBUG
[assembly: System.Reflection.Metadata.MetadataUpdateHandlerAttribute(typeof(NFMWorld.Reactor.HotReloadService))]

namespace NFMWorld.Reactor;

public static class HotReloadService
{
    /// <summary>
    /// Monotonically increments on each hot reload. Components compare their
    /// <c>_lastHotReloadGeneration</c> against this to detect when a hot reload
    /// has occurred and force re-render + re-execute effects.
    /// </summary>
    public static long Generation { get; private set; }

    public static event Action<Type[]?>? UpdateApplicationEvent;

    internal static void ClearCache(Type[]? types)
    {
        Logging.Debug("Hot Reload Service event - ClearCache");
    }

    internal static void UpdateApplication(Type[]? types)
    {
        Generation++;
        UpdateApplicationEvent?.Invoke(types);

        Logging.Debug("Hot Reload Service event - UpdateApplication");
    }
}
#else
public static class HotReloadService
{
    /// <summary>
    /// Monotonically increments on each hot reload. Components compare their
    /// <c>_lastHotReloadGeneration</c> against this to detect when a hot reload
    /// has occurred and force re-render + re-execute effects.
    /// </summary>
    public static long Generation { get; private set; }

    public static event Action<Type[]?>? UpdateApplicationEvent;
}
#endif
