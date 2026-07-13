using System.Diagnostics;
using System.Globalization;
using Microsoft.Xna.Framework;
using NFMWorld.DriverInterface;
using NFMWorld.DriverInterface.DriverInterface;
using NFMWorld.Sfx;
using NFMWorldLibrary.Files;
using NFMWorldLibrary.Helpers;

namespace NFMWorldLibrary.Backend.Gamemodes;

public class TimeTrialGamemode(BaseGamemodeParameters gamemodeParameters, IGamemodeData gamemodeData)
    : BaseGamemode(gamemodeParameters, gamemodeData)
{
    protected const int PlayerCarIndex = 0;
    protected const int GhostCarIndex = 1;

    public override event EventHandler<byte[]>? RaceFinished;

    protected enum TimeTrialState
    {
        NotStarted,
        Countdown,
        InProgress,
        Finished
    }

    protected int _countdownTime = 3;
    // Amount of ticks until we decrease countdown by 1
    protected int _innerCountdownTicks = PlayerCarIndex;
    protected TimeTrialState _currentState = TimeTrialState.NotStarted;

    public override void Enter()
    {
        base.Enter();
        
        _currentState = TimeTrialState.NotStarted;
    }

    public override void Reset()
    {
        base.Reset();
        _countdownTime = 4;
        _innerCountdownTicks = 0; // Tick down immediately to "three"
        
        carsInRace.Clear();
        carsInRace[PlayerCarIndex] = LoadPlayerCar(0, 0);
        carsInRace[PlayerCarIndex].CurrentCheckpoint = 0;
        carsInRace[PlayerCarIndex].CurrentLap = 0;

        _currentState = TimeTrialState.Countdown;

        carsInRace[PlayerCarIndex].CurrentLap = 0;

        ClientServer.RunIfOnClient(ClientReset);
    }

    protected virtual BackendCar LoadPlayerCar(int x, int z)
    {
        return new BackendCar(players[PlayerCarIndex], PlayerCarIndex, x, z);
    }

    public override void GameTick()
    {
        base.GameTick();
        switch (_currentState)
        {
            case TimeTrialState.NotStarted:
                Reset();
                break;
            case TimeTrialState.Countdown:
                CountdownTick();
                break;
            case TimeTrialState.InProgress:
                TimeTrialInRace();
                break;
            case TimeTrialState.Finished:
                TimeTrialFinished();
                break;
        }

        ClientServer.RunIfOnClient(ClientGameTick);
    }

    protected virtual void TimeTrialInRace()
    {
        ClientServer.RunIfOnClient(ClientTimeTrialInRacePre);
        
        carsInRace[PlayerCarIndex].Drive(currentStage);
        
        if (currentStage.checkpoints.Count == 0)
        {
            // lol
            return;
        }

        FixHoopHelper.HandleFixHoops(currentStage, carsInRace[PlayerCarIndex]);
        CheckPointHelper.HandleCheckPoint(currentStage, carsInRace[PlayerCarIndex]);

        if (carsInRace[PlayerCarIndex].CurrentLap >= currentStage.nlaps)
        {
            RaceFinished?.Invoke(this, []);
            _currentState = TimeTrialState.Finished;
        }
        
        ClientServer.RunIfOnClient(ClientTimeTrialInRacePost);
    }
    
    protected virtual void TimeTrialFinished()
    {
        carsInRace[PlayerCarIndex].CarPhysics.Halted = true;
        carsInRace[PlayerCarIndex].Drive(gamemodeData.CurrentStage);

        ClientServer.RunIfOnClient(ClientTimeTrialFinished);
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
                _currentState = TimeTrialState.InProgress;
            }
        }

        ClientServer.RunIfOnClient(ClientCountdownTick);
    }

    #region Client
    
    private Stopwatch _raceTimer = new();

    private bool _writtenData;
    
    // demo playback and recording
    private SavedTimeTrial? _bestTimeTrial;
    private int _tick = 0;
    public static bool PlaybackOnReset = true;
    private SavedTimeTrial? currentTimeTrial;
    private long _lastCheckpointSplitDiff = 0;
    private long _lastLapSplitDiff = 0;
    private long _lastLapTime = 0;

    private ushort _lastCurrentCheckpoint;
    private byte _lastLap;

    [ClientOnly]
    public void SetTimeText()
    {
        HudState.LapTime = (int)_raceTimer.Elapsed.TotalMilliseconds;
    }

    [ClientOnly]
    protected override void ClientReset()
    {
        _raceTimer.Reset();
        _writtenData = false;

        // ghosts
        _bestTimeTrial = null;
        _tick = 0;

        // ghost
        SavedTimeTrial? bestTimeDemo = SavedTimeTrial.Load(players[PlayerCarIndex].CarName, currentStage.Path);
        if (bestTimeDemo != null && PlaybackOnReset)
        {
            _bestTimeTrial = bestTimeDemo;
            carsInRace[GhostCarIndex] = bestTimeDemo.CarData != null
                ? new BackendCar(bestTimeDemo.CarData, PlayerCarIndex, 0, 0, false)
                : new BackendCar(carsInRace[PlayerCarIndex], PlayerCarIndex, false);
            gamemodeData.ClientCallbacks.GetClientCarCallbacks(GhostCarIndex).AlphaOverride = 0.2f;
            carsInRace[GhostCarIndex].CurrentLap = 0;
        }

        currentTimeTrial = new SavedTimeTrial(players[PlayerCarIndex].CarName, currentStage.Path, currentStage.stageLoader, carsInRace[PlayerCarIndex].Rad);
        
        gamemodeData.ClientCallbacks.ResetCheckpointGlow();

        SetTimeText();

        HudState = new HudStateData();
        IBackend.Backend.StopAllSounds();

        _lastLapSplitDiff = 0;
        _lastCheckpointSplitDiff = 0;
        _lastLapTime = 0;
        
        base.ClientReset();
    }

    [ClientOnly]
    protected void ClientGameTick()
    {
        FrameTrace.AddMessage($"contox: {carsInRace[PlayerCarIndex].Position.X:0.00}, contoz: {carsInRace[PlayerCarIndex].Position.Z:0.00}, contoy: {carsInRace[PlayerCarIndex].Position.Y:0.00}");
        
        if (_currentState == TimeTrialState.InProgress)
        {
            HudState.CountdownTimer = 0;
            
            RenderInfo();
        }
        else if (_currentState == TimeTrialState.Countdown)
        {
            RenderInfo();
            
            HudState.CountdownTimer = _countdownTime;
        }
        else if (_currentState == TimeTrialState.Finished)
        {
            RenderInfo();
            
            string finalTime = $"{_raceTimer.Elapsed.Minutes:D2}:{_raceTimer.Elapsed.Seconds:D2}.{_raceTimer.Elapsed.Milliseconds:D3}";
            string centerText = $"Finished! Time: {finalTime}";

            bool newBest = _bestTimeTrial == null || (_bestTimeTrial != null && currentTimeTrial != null && currentTimeTrial.GetSplitDiff(_bestTimeTrial, currentTimeTrial.Splits.SplitTimes.Count - 1) < 0);

            if (newBest)
                centerText += "\nNew best time!";

            if (_bestTimeTrial != null || newBest)
            {
                long bestTimeMs = Math.Min(currentTimeTrial != null ? currentTimeTrial.Splits.SplitTimes[^1] : long.MaxValue, _bestTimeTrial != null ? _bestTimeTrial.Splits.SplitTimes[^1] : long.MaxValue);

                TimeSpan t = TimeSpan.FromMilliseconds(bestTimeMs);

                var time = string.Format(CultureInfo.InvariantCulture, "{0:D2}:{1:D2}:{2:D2}",
                    t.Minutes,
                    t.Seconds,
                    t.Milliseconds);

                centerText += $"\nBest time: {time}";
            }

            centerText += "\nPress R to restart";
            HudState.StateText = centerText;
            HudState.StateTextEndsAt = null; // show until phase resets
        }
    }

    [ClientOnly]
    protected void ClientTimeTrialInRacePre()
    {
        SetTimeText();
        
        base.UpdateHudAndSounds(carsInRace[PlayerCarIndex]);

        if (_bestTimeTrial != null)
        {
            carsInRace[GhostCarIndex].Control.Decode(_bestTimeTrial.GetTick(_tick) ?? (false, false, false, false, false));

            carsInRace[GhostCarIndex].Drive(gamemodeData.CurrentStage);
        }

        currentTimeTrial?.RecordTick(carsInRace[PlayerCarIndex]);

        _lastCurrentCheckpoint = carsInRace[PlayerCarIndex].CurrentCheckpoint;
        _lastLap = carsInRace[PlayerCarIndex].CurrentLap;
    }

    [ClientOnly]
    protected void ClientTimeTrialInRacePost()
    {
        if (carsInRace[PlayerCarIndex].CurrentCheckpoint != _lastCurrentCheckpoint)
        {
            if (_bestTimeTrial != null && currentTimeTrial is { Splits.SplitTimes.Count: > PlayerCarIndex })
            {
                _lastCheckpointSplitDiff = currentTimeTrial.GetSplitDiff(_bestTimeTrial, currentTimeTrial.Splits.SplitTimes.Count - 1);
            }

            long currentLapSplitDiff = 0;
            if (_lastLap > 0 && _bestTimeTrial != null && currentTimeTrial != null)
            {
                currentLapSplitDiff = currentTimeTrial.GetLapTime(currentStage.checkpoints.Count, _lastLap) - _bestTimeTrial.GetLapTime(currentStage.checkpoints.Count, _lastLap - 1);
            }

            currentTimeTrial?.RecordSplit(_raceTimer.ElapsedMilliseconds);

            if (_lastLap != carsInRace[PlayerCarIndex].CurrentLap)
            {
                // lap changed
                _lastLapSplitDiff = currentLapSplitDiff;
                _lastLapTime = currentTimeTrial?.GetLapTime(currentStage.checkpoints.Count, _lastLap) ?? 0;
            }
        }

        if (carsInRace[PlayerCarIndex].CurrentLap >= currentStage.nlaps)
        {
            _raceTimer.Stop();
        }

        _tick++;
    }

    [ClientOnly]
    protected void ClientTimeTrialFinished()
    {
        if (!_writtenData)
        {
            _writtenData = true;
            if (_bestTimeTrial == null || (currentTimeTrial != null && currentTimeTrial.GetSplitDiff(_bestTimeTrial, currentTimeTrial.Splits.SplitTimes.Count - 1) < 0))
            {
                currentTimeTrial?.Save();
            }
        }
    }

    [ClientOnly]
    protected void ClientCountdownTick()
    {
        base.UpdateCountdown(_countdownTime);
        if (_countdownTime <= 0)
        {
            _raceTimer.Start();
        }
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
        
        // Handle key releases specific to Time Trial mode
    }

    [ClientOnly]
    private void RenderInfo()
    {
        if ((carsInRace[PlayerCarIndex].CurrentCheckpoint != 0 || carsInRace[PlayerCarIndex].CurrentLap != 0) && _bestTimeTrial != null && currentTimeTrial != null)
        {
            long diff = currentTimeTrial.GetSplitDiff(_bestTimeTrial, currentTimeTrial.Splits.SplitTimes.Count - 1);
            long lastSplitChange = diff - _lastCheckpointSplitDiff;

            HudState.ChkDiffMs = (int)diff;
            HudState.LastChkDiffMs = (int)lastSplitChange;
        }
        else
        {
            HudState.ChkDiffMs = null;
            HudState.LastChkDiffMs = null;
        }

        if (carsInRace[PlayerCarIndex].CurrentLap > 0 && _bestTimeTrial != null && currentTimeTrial != null)
        {
            long lapTime = currentTimeTrial.GetLapTime(currentStage.checkpoints.Count, carsInRace[PlayerCarIndex].CurrentLap - 1);
            long bestLapTime = _bestTimeTrial.GetLapTime(currentStage.checkpoints.Count, carsInRace[PlayerCarIndex].CurrentLap - 1);
            long lapDiff = lapTime - bestLapTime;
            long lastSplitChange = lapDiff - _lastLapSplitDiff;

            HudState.LapDiffMs = (int)lapDiff;
            HudState.LastLapDiffMs = (int)lastSplitChange;
        }
        else
        {
            HudState.LapDiffMs = null;
            HudState.LastLapDiffMs = null;
        }
    }

    private string FormatTimeMs(long time, bool plusMinus)
    {
        long timeMins = Math.Abs(time / (1000 * 60));
        string timeMinsFmt = $"{timeMins:D2}";
        long timeSecs = Math.Abs(time / 1000 % 60);
        long timeMs = Math.Abs(time % 1000);
        string fmt = $"{(plusMinus ? ((time > 0) ? "+" : "-") : "")}{(timeMins > 0 ? timeMinsFmt + ":" : "")}{timeSecs:D2}.{timeMs:D3}";
        return fmt;
    }

    #endregion
}