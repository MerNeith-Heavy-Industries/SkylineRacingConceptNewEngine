using nfm_world_library.backend.gamemodes;
using nfm_world.driverinterface;
using nfm_world.sfx;
using nfm_world.ui.elements;
using nfm_world.ui.hud;
using nfm_world.ui.yoga;
using nfm_world.util;

namespace nfm_world.gameplay.gamemodes;

public class RaceClientGamemode(BaseGamemodeParameters gamemodeParameters, BaseRacePhase raceValues)
    : RaceGamemode(gamemodeParameters, raceValues), IClientGamemode
{
    private PowerDamageBars _pdBars = new PowerDamageBars();

    private int _lastClientCheckpoint = 0;
    
    private int _lastCountdownTime = 0;

    private LapTimerSplitsView _lapTimerSplits = new LapTimerSplitsView();

    private static TextRun _centerText = null!;
    private Node _centralTextNode = new Node()
    {
        Name = "CentralText",
        AlignItems = YgAlign.Center,
        FlexDirection = YgFlexDirection.Column,

        Children =
        {
            new Node()
            {
                AlignItems = YgAlign.Center,
                Flex = 1,
                Children = {
                    new TextRun()
                    {
                        Ref = textBlock => _centerText = textBlock,
                        Text = "",
                        Color = new Color(0, 0, 0, 0),
                        Font = new Font(FontFamily.Adventure, 1, 24),
                        Display = YgDisplay.None
                    },
                }
            },

            new Node()
            {
                Flex = 1
            }
        }
    };

    public override void Reset()
    {
        base.Reset();
        carsInRace[playerCarIndex].Mad.PowerUp += _pdBars.EventPowerUp;
        
        raceValues.clientStageRenderer.ResetCheckpointGlow();
        
        _pdBars.Reset();
        IBackend.Backend.StopAllSounds();

        _lapTimerSplits.SetLapText(1, currentStage.nlaps);
    }

    protected override void InRace()
    {
        _lapTimerSplits.SetLapText(carsInRace[playerCarIndex].currentLap, currentStage.nlaps);

        _pdBars.SetDamageBarFill(carsInRace[playerCarIndex].Mad.Hitmag, carsInRace[0].Stats.Maxmag);
        _pdBars.UpdateDamageBarColor();
        _pdBars.SetPowerBarFill((float)carsInRace[playerCarIndex].Mad.Power);
        _pdBars.UpdatePowerBarColor();

        base.InRace();
        
        if (carsInRace[playerCarIndex].currentCheckpoint != _lastClientCheckpoint)
        {
            _lastClientCheckpoint = carsInRace[playerCarIndex].currentCheckpoint;
            SfxLibrary.checkpoint?.Play();
        }

        raceValues.clientStageRenderer.UpdateCheckpointGlow(
            carsInRace[playerCarIndex].currentCheckpoint,
            carsInRace[playerCarIndex].currentCheckpoint == currentStage.checkpoints.Count - 1 && carsInRace[playerCarIndex].currentLap == currentStage.nlaps - 1
        );
    }

    public void Render()
    {
        _pdBars.LayoutAndRender(G.Viewport);
        _lapTimerSplits.LayoutAndRender(G.Viewport);
        _centralTextNode.LayoutAndRender(G.Viewport);

        if (_currentState == InnerRaceState.Countdown)
        {
            _centerText.Display = YgDisplay.Flex;
            _centerText.Font = new Font(FontFamily.Adventure, 1, 24);
            _centerText.Color = new Color(255, 255, 255);
            _centerText.StrokeColor = new Color(0, 0, 0);
            _centerText.Text = $"Starting in {_countdownTime}";
        }
        else if (_currentState == InnerRaceState.Finished)
        {
            string finalTime = $"{raceTimer.Elapsed.Minutes:D2}:{raceTimer.Elapsed.Seconds:D2}.{raceTimer.Elapsed.Milliseconds:D3}";
            _centerText.Display = YgDisplay.Flex;
            _centerText.Color = new Color(128, 255, 128);
            _centerText.StrokeColor = new Color(0, 0, 0);
            _centerText.Font = new Font(FontFamily.DroidSans, 1, 24);
            _centerText.Text = $"Finished! Time: {finalTime}";

            _centerText.Text += "\nPress R to restart";
        }
    }

    protected override void CountdownTick()
    {
        base.CountdownTick();
        if (_countdownTime != _lastCountdownTime)
        {
            _lastCountdownTime = _countdownTime;
            SfxLibrary.countdown[_countdownTime].Play();
            if (_countdownTime <= 0)
            {
                _centerText.Display = YgDisplay.None;
            }
        }
    }
}