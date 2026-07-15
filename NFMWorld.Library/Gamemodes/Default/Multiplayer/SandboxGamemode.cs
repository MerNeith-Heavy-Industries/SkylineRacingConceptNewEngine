using Maxine.Extensions;
using NFMWorld.DriverInterface;
using NFMWorld.UI.Hud;
using NFMWorldLibrary.FixedMath;

namespace NFMWorldLibrary.Backend.Gamemodes;

public class SandboxGamemode(GamemodeParameters gamemodeParameters, IGamemodeData gamemodeData)
    : BaseGamemode(gamemodeParameters, gamemodeData)
{
    public override event EventHandler<byte[]>? RaceFinished;

    private int _newTick = 0;

    public override void Begin()
    {
        foreach (var (idx, player) in Players.WithIndex())
        {
            var car = CarsInRace[idx] = new BackendCar(player, idx, 0, 0);
            car.Position = new f64Vector3(fix64.Zero, (fix64)(World.Ground - car.GroundAt) - 250, fix64.Zero);
            car.Rotation = car.Rotation with { Xz = f64AngleSingle.FromDegrees(0) };
            car.CarPhysics.Pxy = 0;
            car.CarPhysics.Pzy = 90;

        }
        CarsInRace[NumPlayers] = new BackendCar(BackendGameSparker.GetCar("nfmm/audir8").Rad!, 1, 100, 0, false);

        Reset();
    }

    public override void End()
    {
        // Cleanup for Time Trial mode
    }

    public override void Reset()
    {
        base.Reset();
        ClientServer.RunIfOnClient(ClientReset);
    }

    public override void GameTick()
    {
        ClientServer.RunIfOnClient(ClientGameTick);
        
        FrameTrace.AddMessage($"contox: {CarsInRace[0].Position.X:0.00}, contoz: {CarsInRace[0].Position.Z:0.00}, contoy: {CarsInRace[0].Position.Y:0.00}");

        if (gamemodeData.RaceState == RaceState.InProgress)
        {
            // Inter-car collision is run at the original tickrate (21.4TPS) to emulate original physics behavior
            // We round this up to 3 ticks per 63TPS tick.
            if (++_newTick == Physics.OriginalTicksPerNewTick)
            {
                for (int i = 0; i < CarsInRace.Count; i++)
                for (int j = 0; j < CarsInRace.Count; j++)
                {
                    if (i != j)
                    {
                        CarsInRace[i].Collide(CarsInRace[j]);
                    }
                }

                _newTick = 0;
            }

            foreach (var car in CarsInRace)
            {
                car.Drive(gamemodeData.CurrentStage);
            }
        }
    }

    #region Client

    [ClientOnly]
    protected void ClientReset()
    {
        // PowerUp event no longer needed — CEF HUD reads HudState each frame.
    }

    [ClientOnly]
    protected void ClientGameTick()
    {
        HudState.Damage = (float)CarsInRace[0].CarPhysics.DamagePoints / CarsInRace[0].Stats.Maxmag;
        HudState.Power = (float)CarsInRace[0].CarPhysics.Power / 100f;
    }

    public override void KeyPressed(Key key, in Keys keys)
    {
        base.KeyPressed(key, keys);
        
        // Handle key presses specific to Time Trial mode
        if (key == Key.R)
        {
            Reset();
        }
    }

    public override void KeyReleased(Key key, in Keys keys)
    {
        base.KeyReleased(key, keys);
    }

    public override void Render()
    {
        base.Render();
    }

    #endregion
}