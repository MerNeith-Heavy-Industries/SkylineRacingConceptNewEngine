using nfm_world_library.mad;

namespace nfm_world_library.backend.ai;

/// <summary>
/// Base AI class for gamemode-specific AI implementations.
/// </summary>
public abstract class BaseAi
{
    public abstract void RunAi(IInGameCar car, int currentCarIndex);
}

// End of ReLitAi class