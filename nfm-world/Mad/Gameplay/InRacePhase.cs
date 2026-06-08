using Microsoft.Xna.Framework.Graphics;
using NFMWorld.DriverInterface;
using NFMWorld.Gameplay.Gamemodes;
using NFMWorld.Util;
using NFMWorldLibrary.Backend.Gamemodes;
using NFMWorldLibrary.Multiplayer;

namespace NFMWorld.Gameplay;

public class InRacePhase(GraphicsDevice graphicsDevice) : BaseRacePhase(graphicsDevice)
{
    public string playerCarName = "nfmm/radicalone";

    public GameModes gamemode
    {
        get;
        set;
    } = GameModes.Racing;

    public void SetGamemode(GameModes mode)
    {
        gamemode = mode;
        ReloadGamemode();
    }

    protected override IGamemode ReloadGamemode()
    {
        return CreateGameMode(new BaseGamemodeParameters
        {
            Players =
            [
                new PlayerParameters
                {
                    CarName = playerCarName,
                    Color = new Color3(255, 0, 0),
                    PlayerName = "Player",
                    IsBot = false,
                    IsClientPlayer = true
                },
                new PlayerParameters()
                {
                    CarName = "nfmm/audir8",
                    Color = new Color3(255, 0, 0),
                    PlayerName = "Player2",
                    IsBot = true,
                    IsClientPlayer = false
                }
            ]
        });
    }

    protected IGamemode CreateGameMode(BaseGamemodeParameters parameters)
    {
        return gamemode switch
        {
            GameModes.Sandbox => new SandboxGamemode(parameters, this),
            GameModes.TimeTrial => new TimeTrialGamemode(parameters, this),
            GameModes.Football => new FootballGamemode(parameters, this),
            GameModes.Racing => new RaceGamemode(parameters, this),
            _ => throw new ArgumentOutOfRangeException(nameof(gamemode), gamemode, null)
        };
    }
}