using Microsoft.Xna.Framework.Graphics;
using NFMWorld.DriverInterface;
using NFMWorld.DriverInterface.DriverInterface;
using NFMWorld.Gameplay.Gamemodes;
using NFMWorld.Util;
using NFMWorldLibrary;
using NFMWorldLibrary.Backend.Gamemodes;
using NFMWorldLibrary.Gamemodes;
using NFMWorldLibrary.Multiplayer;
using NFMWorldLibrary.Multiplayer.Packets.C2S;
using NFMWorldLibrary.Multiplayer.Packets.S2C;
using NFMWorldLibrary.Util;
using S2C_PlayerState = NFMWorldLibrary.Multiplayer.Packets.S2C.S2C_PlayerState;

namespace NFMWorld.Gameplay;

public class InMultiplayerRacePhase(
    GraphicsDevice graphicsDevice,
    IMultiplayerClientTransport transport,
    MatchGameplayInfo session,
    uint playerClientId,
    Guid joinToken
)
    : BaseRacePhase(graphicsDevice)
{
    private uint _ticks = 0; // overflows after ~497 days at 60 ticks per second
    private UnlimitedArray<uint> _lastTick = [];
    
    public override void Enter()
    {
        raceState = RaceState.WaitingToStart;

        base.Enter();

        LoadStage(session.StageName);

        transport.SendPacketToServer(new C2S_RaceLoaded
        {
            JoinToken = joinToken
        });
    }

    protected override IGamemode ReloadGamemode()
    {
        var parameters = new BaseGamemodeParameters()
        {
            Players = session.Players
                .Select(c => new PlayerParameters
                {
                    CarName = c.Value.Vehicle,
                    Color = c.Value.Color,
                    PlayerName = c.Value.Name,
                    IsBot = false,
                    IsClientPlayer = c.Value.Id == playerClientId
                })
                .ToArray()
        };

        return session.Gamemode switch
        {
            GameModes.Sandbox => new SandboxGamemode(parameters, this),
            GameModes.Football => new FootballGamemode(parameters, this),
            GameModes.Racing => new RaceGamemode(parameters, this),
            GameModes.TimeTrial => new TimeTrialGamemode(parameters, this),
            _ => throw new ArgumentOutOfRangeException(nameof(session.Gamemode), session.Gamemode, null)
        };
    }

    public override void GameTick()
    {
        FrameTrace.AddMessage($"race state: {raceState}");
        
        foreach (var packet in transport.GetNewPackets())
        {
            switch (packet)
            {
                case S2C_RaceCanStart raceCanStart:
                    raceState = RaceState.InProgress;
                    break;
                case S2C_RaceFailedToStart raceFailedToStart:
                    raceState = RaceState.FailedToStart;
                    break;
                case S2C_PlayerState playerState:
                    var carIndex = session.Players.First(e => e.Value.Id == playerState.PlayerClientId).Key;
                    var car = CarsInRace[carIndex];
                    if (playerState.State.Ticks <= _lastTick[carIndex])
                        break;
                    _lastTick[carIndex] = playerState.State.Ticks;
                    PlayerState.ApplyTo(playerState.State, car);
                    break;
            }
        }

        base.GameTick();
        
        // camera.Position = new Vector3(0, 10000, 0);
        // camera.LookAt = new Vector3(1, 250, 0);
        
        if (raceState == RaceState.InProgress)
        {
            transport.SendPacketToServer(new C2S_PlayerState()
            {
                State = PlayerState.CreateFrom(_ticks++, CarsInRace.First(c => c.Player.IsClientPlayer))
            });
        }
    }

    public override void Render(float alpha)
    {
        base.Render(alpha);
        if (raceState == RaceState.WaitingToStart)
        {
            G.SetFont(new Font(FontFamily.DroidSans, FontStyle.Plain, 26));
            G.SetColor(new Color(255, 255, 255));
            G.DrawStringAligned("Waiting for other players to load...", 0, 150, (int)G.Viewport.X, (int)G.Viewport.Y, TextHorizontalAlignment.Center);
            
            G.SetColor(new Color(0, 0, 0));
            G.DrawStringStrokeAligned("Waiting for other players to load...", 0, 150, (int)G.Viewport.X, (int)G.Viewport.Y, TextHorizontalAlignment.Center);
        }
    }
}