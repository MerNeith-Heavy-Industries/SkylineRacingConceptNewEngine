using Maxine.Extensions;
using NFMWorld.DriverInterface;
using NFMWorld.UI.Hud;

namespace NFMWorldLibrary.Backend.Gamemodes;

public class SandboxGamemode(BaseGamemodeParameters gamemodeParameters, IGamemodeData gamemodeData)
    : BaseGamemode(gamemodeParameters, gamemodeData)
{
    public override event EventHandler<byte[]>? RaceFinished;

    private int _newTick = 0;

    public override void Enter()
    {
        foreach (var (idx, player) in players.WithIndex())
        {
            carsInRace[idx] = new BackendCar(player, idx, 0, 0);
        }
        carsInRace[NumPlayers] = new BackendCar(BackendGameSparker.GetCar("nfmm/audir8").Rad!, 1, 100, 0, false);

        Reset();
    }

    public override void Exit()
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
        
        FrameTrace.AddMessage($"contox: {carsInRace[0].Position.X:0.00}, contoz: {carsInRace[0].Position.Z:0.00}, contoy: {carsInRace[0].Position.Y:0.00}");

        if (gamemodeData.raceState == RaceState.InProgress)
        {
            // Inter-car collision is run at the original tickrate (21.4TPS) to emulate original physics behavior
            // We round this up to 3 ticks per 63TPS tick.
            if (++_newTick == Physics.OriginalTicksPerNewTick)
            {
                for (int i = 0; i < carsInRace.Count; i++)
                for (int j = 0; j < carsInRace.Count; j++)
                {
                    if (i != j)
                    {
                        carsInRace[i].Collide(carsInRace[j]);
                    }
                }

                _newTick = 0;
            }

            foreach (var car in carsInRace)
            {
                car.Drive(gamemodeData.CurrentStage);
            }
        }
    }

    #region Client
    
    private PowerDamageBars _pdBars = new PowerDamageBars();

    [ClientOnly]
    protected void ClientReset()
    {
        carsInRace[0].Mad.PowerUp += _pdBars.EventPowerUp;
    }

    [ClientOnly]
    protected void ClientGameTick()
    {
        _pdBars.SetDamageBarFill(carsInRace[0].Mad.Hitmag, carsInRace[0].Stats.Maxmag);
        _pdBars.UpdateDamageBarColor();
        _pdBars.SetPowerBarFill((float)carsInRace[0].Mad.Power);
        _pdBars.UpdatePowerBarColor();
    }

    public void KeyPressed(Keys key)
    {
        // Handle key presses specific to Time Trial mode
        if (key == Keys.R)
        {
            Reset();
        }
    }

    public void KeyReleased(Keys key)
    {
        // Handle key releases specific to Time Trial mode
    }

    public void Render()
    {
        _pdBars.LayoutAndRender(G.Viewport);
    }

    #endregion
}