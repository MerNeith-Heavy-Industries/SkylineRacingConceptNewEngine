using NFMWorld.Gameplay.Gamemodes;
using NFMWorldLibrary.Backend;
using NFMWorldLibrary.Backend.Gamemodes;
using NFMWorldLibrary.Files;

namespace NFMWorldLibrary.Multiplayer;

public abstract class BaseGamemodeFactory
{
    public abstract IGamemode CreateGameMode(GamemodeParameters parameters, IGamemodeData gamemodeData);
}

public class SandboxGamemodeFactory : BaseGamemodeFactory
{
    public override IGamemode CreateGameMode(GamemodeParameters parameters, IGamemodeData gamemodeData)
        => new SandboxGamemode(parameters, gamemodeData);
}
public class TimeTrialGamemodeFactory : BaseGamemodeFactory
{
    public override IGamemode CreateGameMode(GamemodeParameters parameters, IGamemodeData gamemodeData)
        => new TimeTrialGamemode(parameters, gamemodeData);
}
public class TimeTrialPreviewGamemodeFactory(SavedTimeTrial timeTrial) : BaseGamemodeFactory
{
    public override IGamemode CreateGameMode(GamemodeParameters parameters, IGamemodeData gamemodeData)
        => new TimeTrialPreviewGamemode(parameters, gamemodeData, timeTrial);
}
public class PvpGamemodeFactory(PvpConstraint constraint) : BaseGamemodeFactory
{
    public override IGamemode CreateGameMode(GamemodeParameters parameters, IGamemodeData gamemodeData)
        => new PvpGamemode(parameters, gamemodeData, constraint);
}
public class FootballGamemodeFactory : BaseGamemodeFactory
{
    public override IGamemode CreateGameMode(GamemodeParameters parameters, IGamemodeData gamemodeData)
        => new FootballGamemode(parameters, gamemodeData);
}

public enum PvpConstraint
{
    Racing, Wasting, Both
}