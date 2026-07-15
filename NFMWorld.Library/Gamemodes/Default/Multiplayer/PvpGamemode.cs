using System.Diagnostics;
using Maxine.Extensions;
using Microsoft.Xna.Framework;
using NFMWorld.DriverInterface;
using NFMWorld.Sfx;
using NFMWorld.DriverInterface.DriverInterface;
using NFMWorldLibrary.Backend.AI;
using NFMWorldLibrary.Helpers;
using NFMWorldLibrary.Multiplayer;
using NFMWorldLibrary.Util;


namespace NFMWorldLibrary.Backend.Gamemodes;

public class PvpGamemode(GamemodeParameters gamemodeParameters, IGamemodeData gamemodeData, PvpConstraint constraint)
    : BaseGamemode(gamemodeParameters, gamemodeData)
{
    public override event EventHandler<byte[]>? RaceFinished;

    protected enum InnerRaceState
    {
        Countdown,
        InProgress,
        Finished
    }

    protected int _countdownTime = 3;
    // Amount of ticks until we decrease countdown by 1
    private int _innerCountdownTicks = 0;
    protected InnerRaceState _currentState = InnerRaceState.Countdown;

    protected Stopwatch raceTimer = new Stopwatch();

    private int _newTick = 0;

    private int _finishTicks;
    
    private int _winner;

    public override void Begin()
    {
        Reset();
    }

    public override void End()
    {
        // Cleanup for Time Trial mode
    }

    public override void Reset()
    {
        base.Reset();

        _countdownTime = 4;
        _innerCountdownTicks = 0; // Tick down immediately to "three"
        _finishTicks = 0;
        raceTimer.Reset();

        CarsInRace.Clear();
        
        foreach (var (idx, player) in Players.WithIndex())
        {
            CarsInRace[idx] = new BackendCar(player, idx, -500 + (400 * idx), 0);
            CarsInRace[idx].CurrentCheckpoint = 0;
            CarsInRace[idx].CurrentLap = 0;
            if (player.IsBot)
            {
                CarsInRace[idx].Bot = new ElStupido(this, GamemodeData);
            }
        }

        _currentState = InnerRaceState.Countdown;

        ClientServer.RunIfOnClient(ClientReset);
    }

    public override void GameTick()
    {
        ClientServer.RunIfOnClient(ClientGameTick);

        if (GamemodeData.RaceState != RaceState.InProgress)
        {
            return;
        }
        
        switch (_currentState)
        {
            case InnerRaceState.Countdown:
                CountdownTick();
                break;
            case InnerRaceState.InProgress:
                InRace();
                break;
            case InnerRaceState.Finished:
                Finished();
                break;
        }
    }

    protected virtual void InRace()
    {
        for (var i = 0; i < CarsInRace.Count; i++)
        {
            var inGameCar = CarsInRace[i];
            if (inGameCar.Bot is { } bot)
            {
                bot.RunAi(inGameCar, i);
            }
        }

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
        
        foreach (var inGameCar in CarsInRace)
        {
            inGameCar.Drive(CurrentStage);
        }

        if (CurrentStage.checkpoints.Count == 0)
        {
            // lol
            return;
        }
        
        for (var i = 0; i < CarsInRace.Count; i++)
        {
            FixHoopHelper.HandleFixHoops(CurrentStage, CarsInRace[i]);
            CheckPointHelper.HandleCheckPoint(CurrentStage, CarsInRace[i]);
        }
        
        CheckPointHelper.CalculatePositions(CurrentStage, CarsInRace);

        for (var i = 0; i < CarsInRace.Count; i++)
        {
            if (CarsInRace[i].CurrentLap >= CurrentStage.nlaps)
            {
                _currentState = InnerRaceState.Finished;
                _winner = i;
                raceTimer.Stop();
            }
        }

        ClientServer.RunIfOnClient(InRaceClient);
    }

    private void Finished()
    {
        foreach (var inGameCar in CarsInRace)
        {
            inGameCar.CarPhysics.Halted = true;
            inGameCar.Drive(GamemodeData.CurrentStage);
        }

        _finishTicks++;

        if (_finishTicks == 30)
        {
            var positions = new byte[CarsInRace.Count];
            // always give position 0 to _winner. assign remaining positions in ascending order based on placement.
            positions[_winner] = 0;
            byte currentPosition = 1;
            for (byte pos = 0; pos < CarsInRace.Count; pos++)
            {
                if (pos == _winner) continue;
                for (byte i = 0; i < CarsInRace.Count; i++)
                {
                    if (i == _winner) continue;
                    if (CarsInRace[i].Placement == pos)
                    {
                        positions[i] = currentPosition;
                        currentPosition++;
                    }
                }
            }
            RaceFinished?.Invoke(this, positions);
        }
    }

    protected virtual void CountdownTick()
    {
        _innerCountdownTicks--;
        if (_innerCountdownTicks <= 0)
        {
            _countdownTime--;
            _innerCountdownTicks = (int)(10 * (1 / Physics.PHYSICS_MULTIPLIER));
            if (_countdownTime <= 0)
            {
                _currentState = InnerRaceState.InProgress;
                raceTimer.Start();
            }
        }

        ClientServer.RunIfOnClient(ClientCountdownTick);
    }
    
    #region Client

    [ClientOnly]
    private int _playerCarIndex;

    [ClientOnly]
    protected void ClientGameTick()
    {
        FrameTrace.AddMessage($"contox: {CarsInRace[_playerCarIndex].Position.X:0.00}, contoz: {CarsInRace[_playerCarIndex].Position.Z:0.00}, contoy: {CarsInRace[_playerCarIndex].Position.Y:0.00}");
    }

    [ClientOnly]
    protected override void ClientReset()
    {
        _playerCarIndex = Players.FindIndex(p => p.IsClientPlayer);
        if (_playerCarIndex == -1)
        {
            Logging.Warning("Client player not found in players list, defaulting to index 0");
            _playerCarIndex = 0;
        }
        base.ClientReset();
    }

    [ClientOnly]
    protected void InRaceClient()
    {
        UpdateHudAndSounds(CarsInRace[_playerCarIndex]);
    }

    public override void Render()
    {
        base.Render();
        
        if (_currentState == InnerRaceState.Finished)
        {
            HudState.StateText = $"Finished! Time: {raceTimer.Elapsed.Minutes:D2}:{raceTimer.Elapsed.Seconds:D2}.{raceTimer.Elapsed.Milliseconds:D3}";
            HudState.StateTextEndsAt = DateTime.Now + TimeSpan.FromSeconds(5);
        }
    }

    [ClientOnly]
    protected void ClientCountdownTick()
    {
        UpdateCountdown(_countdownTime);
    }
    
    #endregion
}