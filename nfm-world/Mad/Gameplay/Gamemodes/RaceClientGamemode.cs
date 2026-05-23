using NFMWorld.DriverInterface;
using NFMWorld.Sfx;
using NFMWorld.UI.Hud;
using NFMWorld.Util;
using NFMWorldLibrary.Backend.Gamemodes;
using WorldXaml.UI.Yoga;

namespace NFMWorld.Gameplay.Gamemodes;

public class RaceClientGamemode(BaseGamemodeParameters gamemodeParameters, BaseRacePhase raceValues)
    : RaceGamemode(gamemodeParameters, raceValues), IClientGamemode
{
    private PowerDamageBars _pdBars = new PowerDamageBars();

    private int _lastClientCheckpoint = 0;
    
    private int _lastCountdownTime = 0;

    private LapTimerSplitsView _lapTimerSplits = new LapTimerSplitsView();

    private CentralTextView _centralTextNode = new CentralTextView();

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
            _centralTextNode.DataContext.CenterTextVisibility = Visibility.Visible;
            _centralTextNode.DataContext.CenterText = $"Starting in {_countdownTime}";
            _centralTextNode.DataContext.CenterTextFont = new Font(FontFamily.Adventure, FontStyle.Bold, 24);
            _centralTextNode.DataContext.CenterTextColor = new Color(255, 255, 255);
            _centralTextNode.DataContext.CenterTextStrokeColor = new Color(0, 0, 0);
        }
        else if (_currentState == InnerRaceState.Finished)
        {
            _centralTextNode.DataContext.CenterTextVisibility = Visibility.Visible;
            string finalTime = $"{raceTimer.Elapsed.Minutes:D2}:{raceTimer.Elapsed.Seconds:D2}.{raceTimer.Elapsed.Milliseconds:D3}";
            _centralTextNode.DataContext.CenterText = $"Finished! Time: {finalTime}\nPress R to restart";
            _centralTextNode.DataContext.CenterTextColor = new Color(128, 255, 128);
            _centralTextNode.DataContext.CenterTextStrokeColor = new Color(0, 0, 0);
            _centralTextNode.DataContext.CenterTextFont = new Font(FontFamily.DroidSans, FontStyle.Bold, 24);
        }
        else
        {
            _centralTextNode.DataContext.CenterTextVisibility = Visibility.Hidden;
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
                _centralTextNode.DataContext.CenterTextVisibility = Visibility.Hidden;
            }
        }
    }
}