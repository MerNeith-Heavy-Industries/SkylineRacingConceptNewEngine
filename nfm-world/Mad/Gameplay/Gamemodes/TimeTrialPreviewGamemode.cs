using NFMWorld.Util;
using NFMWorldLibrary;
using NFMWorldLibrary.Backend;
using NFMWorldLibrary.Backend.Gamemodes;
using NFMWorldLibrary.Files;

namespace NFMWorld.Gameplay.Gamemodes;

public class TimeTrialPreviewGamemode(BaseGamemodeParameters gamemodeParameters, BaseRacePhase raceValues, SavedTimeTrial timeTrial)
    : TimeTrialClientGamemode(gamemodeParameters, raceValues)
{
    private int _tick = 0;
    private bool _paused;
    private bool _slow;
    private int _slowTicks;
    
    public override void Reset()
    {
        base.Reset();
        _tick = 0;
    }

    protected override BackendCar LoadPlayerCar(int x, int z)
    {
        return new BackendCar(timeTrial.CarData ?? BackendGameSparker.GetCar(player.CarName).Rad!, 0, x, z, true);
    }

    protected override void TimeTrialInRace()
    {
        if (!_paused)
        {
            if (_slow)
            {
                _slowTicks++;
                if (_slowTicks % 3 == 0)
                {
                    if (_tick < timeTrial.DemoData.Ticks.Count && _tick > 0)
                    {
                        timeTrial.DemoData.Ticks[_tick - 1].ApplyToCar(carsInRace[playerCarIndex]);
                    }

                    carsInRace[playerCarIndex].Control.Decode(timeTrial.GetTick(_tick) ?? (false, false, false, false, false));
                    base.TimeTrialInRace();
                    
                    _tick++;
                }
            }
            else
            {
                if (_tick < timeTrial.DemoData.Ticks.Count && _tick > 0)
                {
                    timeTrial.DemoData.Ticks[_tick - 1].ApplyToCar(carsInRace[playerCarIndex]);
                }

                carsInRace[playerCarIndex].Control.Decode(timeTrial.GetTick(_tick) ?? (false, false, false, false, false));
                base.TimeTrialInRace();
                
                _tick++;
            }
        }
    }

    public int? SimulateToCompletion(int tickLimit = 100_000_000)
    {
        while (_currentState != TimeTrialState.Finished)
        {
            GameTick();
            if (_tick > tickLimit)
            {
                return null;
            }
        }

        return _tick;
    }

    public override void KeyPressed(Keys key)
    {
        base.KeyPressed(key);

        if (key == Keys.Space)
        {
            _paused = !_paused;
        }
        if (key == Keys.W)
        {
            _tick++;
        }

        if (key == Keys.S)
        {
            _tick--;
        }

        if (key == Keys.A)
        {
            _slow = true;
            _slowTicks = 0;
        }
    }

    public override void KeyReleased(Keys key)
    {
        base.KeyReleased(key);
    }
}