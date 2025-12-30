using System.Diagnostics;
using Maxine.Extensions;
using NFMWorld.DriverInterface;
using NFMWorld.Mad;
using NFMWorld.Mad.ai;
using NFMWorld.Mad.gamemodes;
using NFMWorld.Mad.helpers;
using NFMWorld.Mad.UI.yoga;
using NFMWorld.Util;
using Color = NFMWorld.Util.Color;

public class RaceGamemode(BaseGamemodeParameters gamemodeParameters, BaseRacePhase baseRacePhase)
    : BaseGamemode(gamemodeParameters, baseRacePhase)
{
    public override event EventHandler<byte[]>? RaceFinished;

    private enum InnerRaceState
    {
        Countdown,
        InProgress,
        Finished
    }

    private int _countdownTime = 3;
    // Amount of ticks until we decrease countdown by 1
    private int _innerCountdownTicks = 0;
    private InnerRaceState _currentState = InnerRaceState.Countdown;

    private Stopwatch raceTimer = new Stopwatch();

    private PowerDamageBars _pdBars = new PowerDamageBars();

    private static TextBlock _lapText = null!;
    
    private int _newTick = 0;

    private int _finishTicks;
    
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
                    new TextBlock()
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

    private int _winner;

    public void SetLapText(int currentLap)
    {
        _lapText.Text = $"{currentLap + 1}/{currentStage.nlaps}";
    }

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
            carsInRace[idx] = new InGameCar(idx, GameSparker.GetCar(player.CarName).Car!, -500 + (400 * idx), 0, idx == playerCarIndex);
            carsInRace[idx].currentCheckpoint = 0;
            carsInRace[idx].currentLap = 0;
            if (player.IsBot)
            {
                carsInRace[idx].Bot = new ReLitAi(this, baseRacePhase);
            }
        }
        carsInRace[playerCarIndex].Mad.PowerUp += _pdBars.EventPowerUp;

        foreach (var cp in currentStage.checkpoints)
        {
            cp.Glow = false;
        }

        _pdBars.Reset();
        IBackend.Backend.StopAllSounds();

        SetLapText(1);

        _currentState = InnerRaceState.Countdown;
    }

    public override void GameTick()
    {
        FrameTrace.AddMessage($"contox: {carsInRace[playerCarIndex].Position.X:0.00}, contoz: {carsInRace[playerCarIndex].Position.Z:0.00}, contoy: {carsInRace[playerCarIndex].Position.Y:0.00}");

        if (baseRacePhase.raceState != RaceState.InProgress)
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

    private void InRace()
    {
        SetLapText(carsInRace[playerCarIndex].currentLap);

        _pdBars.SetDamageBarFill(carsInRace[playerCarIndex].Mad.Hitmag, carsInRace[0].Stats.Maxmag);
        _pdBars.UpdateDamageBarColor();
        _pdBars.SetPowerBarFill((float)carsInRace[playerCarIndex].Mad.Power);
        _pdBars.UpdatePowerBarColor();

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
        if (++_newTick == GameSparker.OriginalTicksPerNewTick)
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
        
            if (CheckPointHelper.HandleCheckPoint(currentStage, carsInRace[i]))
            {
                if (i == playerCarIndex)
                    SfxLibrary.checkpoint?.Play();
            }
        }
        
        CheckPointHelper.CalculatePositions(currentStage, carsInRace);

        if (carsInRace[playerCarIndex].currentCheckpoint == currentStage.checkpoints.Count - 1 && carsInRace[playerCarIndex].currentLap == currentStage.nlaps - 1)
        {
            currentStage.checkpoints[^1].Finish = true;
        }
        else
        {
            currentStage.checkpoints[^1].Finish = false;
        }

        if (carsInRace[playerCarIndex].currentCheckpoint > 0)
        {
            currentStage.checkpoints[carsInRace[playerCarIndex].currentCheckpoint - 1].Glow = false;
        }
        else
        {
            currentStage.checkpoints[^1].Glow = false;
        }

        if (carsInRace[playerCarIndex].currentCheckpoint < currentStage.checkpoints.Count)
        {
            currentStage.checkpoints[carsInRace[playerCarIndex].currentCheckpoint].Glow = true;
        }

        for (var i = 0; i < carsInRace.Count; i++)
        {
            if (carsInRace[i].currentLap >= currentStage.nlaps)
            {
                _currentState = InnerRaceState.Finished;
                _winner = i;
                raceTimer.Stop();
            }
        }
    }

    private void Finished()
    {
        foreach (var inGameCar in carsInRace)
        {
            inGameCar.Mad.Halted = true;
            inGameCar.Drive(baseRacePhase.CurrentStage);
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

    private void CountdownTick()
    {
        _innerCountdownTicks--;
        if (_innerCountdownTicks <= 0)
        {
            _countdownTime--;
            SfxLibrary.countdown[_countdownTime].Play();
            _innerCountdownTicks = (int)(10 * (1 / GameSparker.PHYSICS_MULTIPLIER));
            if (_countdownTime <= 0)
            {
                _currentState = InnerRaceState.InProgress;
                _centerText.Display = Yoga.YGDisplay.YGDisplayNone;
                raceTimer.Start();
            }
        }
    }

    public override void KeyPressed(Keys key)
    {
    }

    public override void KeyReleased(Keys key)
    {
        // Handle key releases specific to Time Trial mode
    }

    public override void Render()
    {
        _pdBars.Render();
        _lapTimerSplits.LayoutAndRender(G.Viewport);
        _centralTextNode.LayoutAndRender(G.Viewport);

        if (_currentState == InnerRaceState.Countdown)
        {
            _centerText.Display = Yoga.YGDisplay.YGDisplayFlex;
            _centerText.Font = new Font(FontFamily.Adventure, 1, 24);
            _centerText.Color = new Color(255, 255, 255);
            _centerText.StrokeColor = new Color(0, 0, 0);
            _centerText.Text = $"Starting in {_countdownTime}";
        }
        else if (_currentState == InnerRaceState.Finished)
        {
            string finalTime = $"{raceTimer.Elapsed.Minutes:D2}:{raceTimer.Elapsed.Seconds:D2}.{raceTimer.Elapsed.Milliseconds:D3}";
            _centerText.Display = Yoga.YGDisplay.YGDisplayFlex;
            _centerText.Color = new Color(128, 255, 128);
            _centerText.StrokeColor = new Color(0, 0, 0);
            _centerText.Font = new Font(FontFamily.DroidSans, 1, 24);
            _centerText.Text = $"Finished! Time: {finalTime}";

            _centerText.Text += "\nPress R to restart";
        }
    }
}