using System.Diagnostics;
using Maxine.Extensions;
using Microsoft.Xna.Framework;
using NFMWorld.DriverInterface;
using NFMWorld.DriverInterface.DriverInterface;
using NFMWorld.Sfx;
using NFMWorldLibrary.Backend.AI;
using NFMWorldLibrary.Helpers;
using NFMWorldLibrary.Util;


namespace NFMWorldLibrary.Backend.Gamemodes;

public class RaceGamemode(BaseGamemodeParameters gamemodeParameters, IGamemodeData gamemodeData)
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

    public override void Enter()
    {
        Reset();
    }

    public override void Exit()
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

        carsInRace.Clear();
        
        foreach (var (idx, player) in players.WithIndex())
        {
            carsInRace[idx] = new BackendCar(player, idx, -500 + (400 * idx), 0);
            carsInRace[idx].CurrentCheckpoint = 0;
            carsInRace[idx].CurrentLap = 0;
            if (player.IsBot)
            {
                carsInRace[idx].Bot = new ElStupido(this, gamemodeData);
            }
        }

        _currentState = InnerRaceState.Countdown;

        ClientServer.RunIfOnClient(ClientReset);
    }

    public override void GameTick()
    {
        ClientServer.RunIfOnClient(ClientGameTick);

        if (gamemodeData.raceState != RaceState.InProgress)
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
        for (var i = 0; i < carsInRace.Count; i++)
        {
            var inGameCar = carsInRace[i];
            if (inGameCar.Bot is { } bot)
            {
                bot.RunAi(inGameCar, i);
            }
        }

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
        
        foreach (var inGameCar in carsInRace)
        {
            inGameCar.Drive(currentStage);
        }

        if (currentStage.checkpoints.Count == 0)
        {
            // lol
            return;
        }
        
        for (var i = 0; i < carsInRace.Count; i++)
        {
            FixHoopHelper.HandleFixHoops(currentStage, carsInRace[i]);
            CheckPointHelper.HandleCheckPoint(currentStage, carsInRace[i]);
        }
        
        CheckPointHelper.CalculatePositions(currentStage, carsInRace);

        for (var i = 0; i < carsInRace.Count; i++)
        {
            if (carsInRace[i].CurrentLap >= currentStage.nlaps)
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
        foreach (var inGameCar in carsInRace)
        {
            inGameCar.CarPhysics.Halted = true;
            inGameCar.Drive(gamemodeData.CurrentStage);
        }

        _finishTicks++;

        if (_finishTicks == 30)
        {
            var positions = new byte[carsInRace.Count];
            // always give position 0 to _winner. assign remaining positions in ascending order based on placement.
            positions[_winner] = 0;
            byte currentPosition = 1;
            for (byte pos = 0; pos < carsInRace.Count; pos++)
            {
                if (pos == _winner) continue;
                for (byte i = 0; i < carsInRace.Count; i++)
                {
                    if (i == _winner) continue;
                    if (carsInRace[i].Placement == pos)
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
        FrameTrace.AddMessage($"contox: {carsInRace[_playerCarIndex].Position.X:0.00}, contoz: {carsInRace[_playerCarIndex].Position.Z:0.00}, contoy: {carsInRace[_playerCarIndex].Position.Y:0.00}");
    }

    [ClientOnly]
    protected override void ClientReset()
    {
        _playerCarIndex = players.FindIndex(p => p.IsClientPlayer);
        base.ClientReset();
    }

    [ClientOnly]
    protected void InRaceClient()
    {
        UpdateHudAndSounds(carsInRace[_playerCarIndex]);
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