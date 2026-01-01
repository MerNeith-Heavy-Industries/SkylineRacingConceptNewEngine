using Maxine.Extensions;
using NFMWorld.Util;

namespace NFMWorld.Mad.ai;

/// <summary>
/// Base AI class for gamemode-specific AI implementations.
/// </summary>
public abstract class BaseAi
{
    public abstract void RunAi(IInGameCar car, int currentCarIndex);
}

// End of ReLitAi class