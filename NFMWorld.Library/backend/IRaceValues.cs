using NFMWorld.Mad;
using NFMWorld.Util;

namespace NFMWorld.Library.backend;

public interface IRaceValues
{
    UnlimitedArray<IInGameCar> CarsInRace { get; }
    IStage CurrentStage { get; }
}