using System.Diagnostics;
using Maxine.Extensions;
using Microsoft.Xna.Framework;
using NFMWorld.DriverInterface;
using NFMWorld.Sfx;
using NFMWorld.UI.Hud;
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
            carsInRace[idx] = new BackendCar(BackendGameSparker.GetCar(player.CarName).Rad!, idx, -500 + (400 * idx), 0, player.IsClientPlayer);
            carsInRace[idx].currentCheckpoint = 0;
            carsInRace[idx].currentLap = 0;
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
            if (carsInRace[i].currentLap >= currentStage.nlaps)
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
            inGameCar.Mad.Halted = true;
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
                    if (carsInRace[i].placement == pos)
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
    
    private PowerDamageBars _pdBars = new PowerDamageBars();

    private int _lastClientCheckpoint = 0;
    
    private int _lastCountdownTime = 0;

    private LapTimerSplitsView _lapTimerSplits = new LapTimerSplitsView();

    private CentralTextView _centralTextNode = new CentralTextView();

    private int _playerCarIndex;

    [ClientOnly]
    protected void ClientGameTick()
    {
        FrameTrace.AddMessage($"contox: {carsInRace[_playerCarIndex].Position.X:0.00}, contoz: {carsInRace[_playerCarIndex].Position.Z:0.00}, contoy: {carsInRace[_playerCarIndex].Position.Y:0.00}");
    }

    [ClientOnly]
    protected void ClientReset()
    {
        _playerCarIndex = players.FindIndex(p => p.IsClientPlayer);
        carsInRace[_playerCarIndex].Mad.PowerUp += _pdBars.EventPowerUp;
        
        gamemodeData.ClientCallbacks.ResetCheckpointGlow();
        
        _pdBars.Reset();
        IBackend.Backend.StopAllSounds();

        _lapTimerSplits.DataContext.CurrentLap = 1;
        _lapTimerSplits.DataContext.TotalLaps = currentStage.nlaps;
    }

    [ClientOnly]
    protected void InRaceClient()
    {
        _lapTimerSplits.DataContext.CurrentLap = carsInRace[_playerCarIndex].currentLap + 1;

        _pdBars.SetDamageBarFill(carsInRace[_playerCarIndex].Mad.Hitmag, carsInRace[0].Stats.Maxmag);
        _pdBars.UpdateDamageBarColor();
        _pdBars.SetPowerBarFill((float)carsInRace[_playerCarIndex].Mad.Power);
        _pdBars.UpdatePowerBarColor();

        if (carsInRace[_playerCarIndex].currentCheckpoint != _lastClientCheckpoint)
        {
            _lastClientCheckpoint = carsInRace[_playerCarIndex].currentCheckpoint;
            SfxLibrary.checkpoint?.Play();
        }

        gamemodeData.ClientCallbacks.UpdateCheckpointGlow(
            carsInRace[_playerCarIndex].currentCheckpoint,
            carsInRace[_playerCarIndex].currentCheckpoint == currentStage.checkpoints.Count - 1 && carsInRace[_playerCarIndex].currentLap == currentStage.nlaps - 1
        );
    }

    public void Render()
    {
        _pdBars.LayoutAndRender(G.Viewport);
        _lapTimerSplits.LayoutAndRender(G.Viewport);
        _centralTextNode.LayoutAndRender(G.Viewport);

        if (_currentState == InnerRaceState.Countdown)
        {
            _centralTextNode.DataContext.CenterTextOpacity = 1;
            _centralTextNode.DataContext.CenterText = $"Starting in {_countdownTime}";
            _centralTextNode.DataContext.CenterTextFont = new Font(FontFamily.Adventure, FontStyle.Bold, 24);
            _centralTextNode.DataContext.CenterTextColor = new Color(255, 255, 255);
            _centralTextNode.DataContext.CenterTextStrokeColor = new Color(0, 0, 0);
        }
        else if (_currentState == InnerRaceState.Finished)
        {
            _centralTextNode.DataContext.CenterTextOpacity = 1;
            string finalTime = $"{raceTimer.Elapsed.Minutes:D2}:{raceTimer.Elapsed.Seconds:D2}.{raceTimer.Elapsed.Milliseconds:D3}";
            _centralTextNode.DataContext.CenterText = $"Finished! Time: {finalTime}\nPress R to restart";
            _centralTextNode.DataContext.CenterTextColor = new Color(128, 255, 128);
            _centralTextNode.DataContext.CenterTextStrokeColor = new Color(0, 0, 0);
            _centralTextNode.DataContext.CenterTextFont = new Font(FontFamily.DroidSans, FontStyle.Bold, 24);
        }
        else
        {
            _centralTextNode.DataContext.CenterTextOpacity = 0;
        }
    }

    [ClientOnly]
    protected void ClientCountdownTick()
    {
        if (_countdownTime != _lastCountdownTime)
        {
            _lastCountdownTime = _countdownTime;
            SfxLibrary.countdown[_countdownTime].Play();
            if (_countdownTime <= 0)
            {
                _centralTextNode.DataContext.CenterTextOpacity = 0;
            }
        }
    }
    
    #endregion
}