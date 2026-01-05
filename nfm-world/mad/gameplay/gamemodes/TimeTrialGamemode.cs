using System.Diagnostics;
using nfm_world_library;
using nfm_world_library.backend;
using nfm_world_library.backend.gamemodes;
using nfm_world_library.mad.helpers;
using nfm_world.driverinterface;
using nfm_world.files;
using nfm_world.sfx;
using nfm_world.ui.hud;
using nfm_world.ui.yoga;
using nfm_world.util;

namespace nfm_world.gameplay.gamemodes;

public class TimeTrialGamemode(BaseGamemodeParameters gamemodeParameters, BaseRacePhase raceValues)
    : BaseGamemode(gamemodeParameters, raceValues), IClientGamemode
{
    public override event EventHandler<byte[]>? RaceFinished;

    private enum TimeTrialState
    {
        NotStarted,
        Countdown,
        InProgress,
        Finished
    }

    private int _countdownTime = 3;
    // Amount of ticks until we decrease countdown by 1
    private int _innerCountdownTicks = 0;
    private TimeTrialState _currentState = TimeTrialState.NotStarted;

    private bool _writtenData;

    private Stopwatch _raceTimer = new Stopwatch();

    // demo playback and recording
    private SavedTimeTrial? _bestTimeTrial = null;
    private int _tick = 0;
    public static bool PlaybackOnReset = true;
    private SavedTimeTrial currentTimeTrial = null!;
    private long _lastCheckpointSplitDiff = 0;
    private long _lastLapSplitDiff = 0;
    private long _lastLapTime = 0;

    private PowerDamageBars _pdBars = new PowerDamageBars();

    private static TextRun _lapText = null!;

    private static TextRun _timerText = null!;

    private static TextRun _checkpointSplitsText = null!;
    private static TextRun _lapSplitsText = null!;
    private static TextRun _lastLapTimeText = null!;

    private Node _lapTimerSplits = new Node()
    {
        Name = "LapTimerSplits",
        FlexDirection = Yoga.YGFlexDirection.YGFlexDirectionColumn,
        AlignItems = Yoga.YGAlign.YGAlignFlexStart,
        JustifyContent = Yoga.YGJustify.YGJustifyCenter,
        Gap = 10,
        Padding = 10,

        Children =
        {
            new Node()
            {
                Name = "LapDisplay",
                FlexDirection = Yoga.YGFlexDirection.YGFlexDirectionRow,
                Children =
                {
                    new TextRun()
                    {
                        Name = "LapIcon",
                        Font = new Font(FontFamily.Adventure, 1, 24),
                        Color = new Color(255, 255, 255),
                        StrokeColor = new Color(0, 0, 0),
                        Text = "Lap: ",
                        Flex = 1
                    },
                    new TextRun()
                    {
                        Ref = textBlock => _lapText = textBlock,
                        StrokeColor = new Color(0, 0, 0),
                        Name = "LapText",
                        Color = new Color(255, 255, 255),
                        Font = new Font(FontFamily.DroidSans, 1, 24),
                        Flex = 1,
                    }
                }
            },
            new Node()
            {
                Name = "TimeDisplay",
                FlexDirection = Yoga.YGFlexDirection.YGFlexDirectionRow,
                Children =
                {
                    new TextRun()
                    {
                        Name = "TimeIcon",
                        Font = new Font(FontFamily.Adventure, 1, 24),
                        Color = new Color(255, 255, 255),
                        StrokeColor = new Color(0, 0, 0),
                        Text = "Time: ",
                        Flex = 1
                    },
                    new TextRun()
                    {
                        Ref = textBlock => _timerText = textBlock,
                        StrokeColor = new Color(0, 0, 0),
                        Name = "TimeText",
                        Color = new Color(255, 255, 255),
                        Font = new Font(FontFamily.DroidSans, 1, 24),
                        Flex = 1,
                    }
                }
            },
            new Node()
            {
                Children =
                {
                    new TextRun()
                    {
                        Ref = textBlock => _lastLapTimeText = textBlock,
                        Name = "LapTimeText",
                        StrokeColor = new Color(0, 0, 0),
                        Color = new Color(255, 255, 255),
                        Font = new Font(FontFamily.DroidSans, 1, 24),
                        Flex = 1,
                    }
                }
            },
            new Node()
            {
                Children =
                {
                    new TextRun()
                    {
                        Ref = textBlock => _checkpointSplitsText = textBlock,
                        Name = "CheckpointSplitsText",
                        StrokeColor = new Color(0, 0, 0),
                        Color = new Color(255, 255, 255),
                        Font = new Font(FontFamily.DroidSans, 1, 24),
                        Flex = 1,
                    }
                }
            },
            new Node()
            {
                Children =
                {
                    new TextRun()
                    {
                        Ref = textBlock => _lapSplitsText = textBlock,
                        Name = "LapSplitsText",
                        StrokeColor = new Color(0, 0, 0),
                        Color = new Color(255, 255, 255),
                        Font = new Font(FontFamily.DroidSans, 1, 24),
                        Flex = 1,
                    }
                }
            }
        }
    };

    private static TextRun _centerText = null!;
    private Node _centralTextNode = new Node()
    {
        Name = "CentralText",
        AlignItems = Yoga.YGAlign.YGAlignCenter,
        FlexDirection = Yoga.YGFlexDirection.YGFlexDirectionColumn,

        Children =
        {
            new Node()
            {
                AlignItems = Yoga.YGAlign.YGAlignCenter,
                Flex = 1,
                Children = {
                    new TextRun()
                    {
                        Ref = textBlock => _centerText = textBlock,
                        Text = "",
                        Color = new Color(0, 0, 0, 0),
                        Font = new Font(FontFamily.Adventure, 1, 24),
                        Display = Yoga.YGDisplay.YGDisplayNone
                    },
                }
            },

            new Node()
            {
                Flex = 1
            }
        }
    };

    public void SetLapText(int currentLap)
    {
        _lapText.Text = $"{currentLap + 1}/{currentStage.nlaps}";
    }

    public void SetTimeText()
    {
        _timerText.Text = $"{_raceTimer.Elapsed.Minutes:D2}:{_raceTimer.Elapsed.Seconds:D2}.{_raceTimer.Elapsed.Milliseconds:D3}";
    }

    public override void Enter()
    {
        base.Enter();

        _currentState = TimeTrialState.NotStarted;

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
        _raceTimer.Reset();
        _writtenData = false;

        // ghosts
        _bestTimeTrial = null;
        _tick = 0;

        carsInRace.Clear();
        carsInRace[playerCarIndex] = new BackendCar(BackendGameSparker.GetCar(player.CarName).Rad!, 0, 0, 0, true);
        carsInRace[playerCarIndex].Mad.PowerUp += _pdBars.EventPowerUp;
        carsInRace[playerCarIndex].currentCheckpoint = 0;
        carsInRace[playerCarIndex].currentLap = 0;

        // ghost
        carsInRace[playerCarIndex + 1] = new BackendCar(carsInRace[playerCarIndex], 0, false);
        raceValues.GetClientCar(playerCarIndex + 1)!.Sfx!.Mute = true;

        SavedTimeTrial? bestTimeDemo = SavedTimeTrial.Load(player.CarName, currentStage.Path);
        if (bestTimeDemo != null && PlaybackOnReset)
        {
            _bestTimeTrial = bestTimeDemo;
            raceValues.GetClientCar(playerCarIndex + 1).AlphaOverride = 0.2f;
            carsInRace[playerCarIndex + 1].currentLap = 0;
        }
        else
        {
            carsInRace.RemoveAt(1);
        }

        currentTimeTrial = new SavedTimeTrial(player.CarName, currentStage.Path);
        
        raceValues.clientStageRenderer.ResetCheckpointGlow();

        SetTimeText();

        _pdBars.Reset();
        IBackend.Backend.StopAllSounds();

        SetLapText(0);
        _checkpointSplitsText.Display = Yoga.YGDisplay.YGDisplayNone;

        _currentState = TimeTrialState.Countdown;
        _lastLapSplitDiff = 0;
        _lastCheckpointSplitDiff = 0;
        _lapSplitsText.Display = Yoga.YGDisplay.YGDisplayNone;
        _lastLapTime = 0;
        _lastLapTimeText.Text = "";
        _lastLapTimeText.Display = Yoga.YGDisplay.YGDisplayNone;

        carsInRace[playerCarIndex].currentLap = 0;
    }

    public override void GameTick()
    {
        FrameTrace.AddMessage($"contox: {carsInRace[playerCarIndex].Position.X:0.00}, contoz: {carsInRace[playerCarIndex].Position.Z:0.00}, contoy: {carsInRace[playerCarIndex].Position.Y:0.00}");
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
    }

    private void TimeTrialInRace()
    {
        SetLapText(carsInRace[playerCarIndex].currentLap);
        SetTimeText();

        _pdBars.SetDamageBarFill(carsInRace[playerCarIndex].Mad.Hitmag, carsInRace[0].Stats.Maxmag);
        _pdBars.UpdateDamageBarColor();
        _pdBars.SetPowerBarFill((float)carsInRace[playerCarIndex].Mad.Power);
        _pdBars.UpdatePowerBarColor();

        if (_bestTimeTrial != null)
        {
            carsInRace[playerCarIndex + 1].Control.Decode(_bestTimeTrial.GetTick(_tick) ?? (false, false, false, false, false));

            carsInRace[playerCarIndex + 1].Drive(raceValues.CurrentStage);
        }

        currentTimeTrial.RecordTick(carsInRace[playerCarIndex]);
        carsInRace[playerCarIndex].Drive(currentStage);

        if (currentStage.checkpoints.Count == 0)
        {
            // lol
            return;
        }

        FixHoopHelper.HandleFixHoops(currentStage, carsInRace[playerCarIndex]);

        int currentLap = carsInRace[playerCarIndex].currentLap;
        if (CheckPointHelper.HandleCheckPoint(currentStage, carsInRace[playerCarIndex]))
        {
            if (_bestTimeTrial != null && currentTimeTrial.Splits.SplitTimes.Count > 0)
            {
                _lastCheckpointSplitDiff = currentTimeTrial.GetSplitDiff(_bestTimeTrial, currentTimeTrial.Splits.SplitTimes.Count - 1);
            }

            long currentLapSplitDiff = 0;
            if (currentLap > 0 && _bestTimeTrial != null)
            {
                currentLapSplitDiff = currentTimeTrial.GetLapTime(currentStage.checkpoints.Count, currentLap) - _bestTimeTrial.GetLapTime(currentStage.checkpoints.Count, currentLap - 1);
            }

            currentTimeTrial.RecordSplit(_raceTimer.ElapsedMilliseconds);

            if (currentLap != carsInRace[playerCarIndex].currentLap)
            {
                // lap changed
                _lastLapSplitDiff = currentLapSplitDiff;
                _lastLapTime = currentTimeTrial.GetLapTime(currentStage.checkpoints.Count, currentLap);
            }

            SfxLibrary.checkpoint?.Play();
        }
        
        raceValues.clientStageRenderer.UpdateCheckpointGlow(
            carsInRace[playerCarIndex].currentCheckpoint,
            carsInRace[playerCarIndex].currentCheckpoint == currentStage.checkpoints.Count - 1 && carsInRace[playerCarIndex].currentLap == currentStage.nlaps - 1
        );

        if (carsInRace[playerCarIndex].currentLap >= currentStage.nlaps)
        {
            _currentState = TimeTrialState.Finished;
            _raceTimer.Stop();
        }

        _tick++;
    }

    private void TimeTrialFinished()
    {
        if (!_writtenData)
        {
            _writtenData = true;
            if (_bestTimeTrial == null || (currentTimeTrial != null && currentTimeTrial.GetSplitDiff(_bestTimeTrial, currentTimeTrial.Splits.SplitTimes.Count - 1) < 0))
            {
                currentTimeTrial.Save();
            }
        }

        carsInRace[playerCarIndex].Mad.Halted = true;
        carsInRace[playerCarIndex].Drive(raceValues.CurrentStage);
    }

    private void CountdownTick()
    {
        _innerCountdownTicks--;
        if (_innerCountdownTicks <= 0)
        {
            _countdownTime--;
            SfxLibrary.countdown[_countdownTime].Play();
            _innerCountdownTicks = (int)(10 * (1 / Physics.PHYSICS_MULTIPLIER));
            if (_countdownTime <= 0)
            {
                _currentState = TimeTrialState.InProgress;
                _centerText.Display = Yoga.YGDisplay.YGDisplayNone;
                _raceTimer.Start();
            }
        }
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

    private void RenderInfo()
    {
        if ((carsInRace[playerCarIndex].currentCheckpoint != 0 || carsInRace[playerCarIndex].currentLap != 0) && _bestTimeTrial != null)
        {
            _checkpointSplitsText.Display = Yoga.YGDisplay.YGDisplayFlex;
            long diff = currentTimeTrial.GetSplitDiff(_bestTimeTrial, currentTimeTrial.Splits.SplitTimes.Count - 1);
            _checkpointSplitsText.Color = diff > 0 ? new Color(255, 128, 128) : new Color(128, 255, 128);

            long lastSplitChange = diff - _lastCheckpointSplitDiff;
            string lastSplitFmt = FormatTimeMs(lastSplitChange, true);

            string thisDiffFmt = FormatTimeMs(diff, true);
            _checkpointSplitsText.Text = $"CHK Diff: {thisDiffFmt} ({lastSplitFmt})";
        }
        else
        {
            _checkpointSplitsText.Display = Yoga.YGDisplay.YGDisplayNone;
        }

        if (carsInRace[playerCarIndex].currentLap > 0 && _bestTimeTrial != null)
        {
            _lapSplitsText.Display = Yoga.YGDisplay.YGDisplayFlex;
            long lapTime = currentTimeTrial.GetLapTime(currentStage.checkpoints.Count, carsInRace[playerCarIndex].currentLap - 1);
            long bestLapTime = _bestTimeTrial.GetLapTime(currentStage.checkpoints.Count, carsInRace[playerCarIndex].currentLap - 1);
            long lapDiff = lapTime - bestLapTime;
            _lapSplitsText.Color = lapDiff > 0 ? new Color(255, 128, 128) : new Color(128, 255, 128);

            long lastSplitChange = lapDiff - _lastLapSplitDiff;
            string lastLapSplitFmt = FormatTimeMs(lastSplitChange, true);

            string lapDiffFmt = FormatTimeMs(lapDiff, true);

            _lapSplitsText.Text = $"Lap Diff: {lapDiffFmt} ({lastLapSplitFmt})";
        }
        else
        {
            _lapSplitsText.Display = Yoga.YGDisplay.YGDisplayNone;
        }

        if(_lastLapTime > 0)
        {
            _lastLapTimeText.Display = Yoga.YGDisplay.YGDisplayFlex;
            _lastLapTimeText.Text = $"Lap Time: {FormatTimeMs(_lastLapTime, false)}";
        }
    }

    public void Render()
    {
        _pdBars.Render();
        _lapTimerSplits.LayoutAndRender(G.Viewport);
        _centralTextNode.LayoutAndRender(G.Viewport);

        if (_currentState == TimeTrialState.InProgress)
        {
            RenderInfo();
        }
        else if (_currentState == TimeTrialState.Countdown)
        {
            RenderInfo();

            _centerText.Display = Yoga.YGDisplay.YGDisplayFlex;
            _centerText.Font = new Font(FontFamily.Adventure, 1, 24);
            _centerText.Color = new Color(255, 255, 255);
            _centerText.StrokeColor = new Color(0, 0, 0);
            _centerText.Text = $"Starting in {_countdownTime}";
        }
        else if (_currentState == TimeTrialState.Finished)
        {
            RenderInfo();

            string finalTime = $"{_raceTimer.Elapsed.Minutes:D2}:{_raceTimer.Elapsed.Seconds:D2}.{_raceTimer.Elapsed.Milliseconds:D3}";
            _centerText.Display = Yoga.YGDisplay.YGDisplayFlex;
            _centerText.Color = new Color(128, 255, 128);
            _centerText.StrokeColor = new Color(0, 0, 0);
            _centerText.Font = new Font(FontFamily.DroidSans, 1, 24);
            _centerText.Text = $"Finished! Time: {finalTime}";

            bool newBest = _bestTimeTrial == null || (_bestTimeTrial != null && currentTimeTrial.GetSplitDiff(_bestTimeTrial, currentTimeTrial.Splits.SplitTimes.Count - 1) < 0);

            if (newBest)
                _centerText.Text += "\nNew best time!";

            if (_bestTimeTrial != null || newBest)
            {
                long bestTimeMs = Math.Min(currentTimeTrial.Splits.SplitTimes[^1], _bestTimeTrial != null ? _bestTimeTrial.Splits.SplitTimes[^1] : long.MaxValue);

                TimeSpan t = TimeSpan.FromMilliseconds(bestTimeMs);

                string time = string.Format("{0:D2}:{1:D2}:{2:D2}",
                    t.Minutes,
                    t.Seconds,
                    t.Milliseconds);

                _centerText.Text += $"\nBest time: {time}";
            }

            _centerText.Text += "\nPress R to restart";
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
}