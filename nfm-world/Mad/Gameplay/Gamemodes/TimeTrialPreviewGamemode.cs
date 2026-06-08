using NFMWorld.Util;
using NFMWorldLibrary;
using NFMWorldLibrary.Backend;
using NFMWorldLibrary.Backend.Gamemodes;
using NFMWorldLibrary.Files;

namespace NFMWorld.Gameplay.Gamemodes;

public class TimeTrialPreviewGamemode(
    BaseGamemodeParameters gamemodeParameters,
    BaseRacePhase raceValues,
    SavedTimeTrial timeTrial)
    : TimeTrialClientGamemode(gamemodeParameters, raceValues)
{
    private int _tick = 0;
    private bool _paused;
    private bool _slow;
    private int _slowTicks;
    private bool _shift;
    private bool _ctrl;

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
        if (_slow && !_paused)
        {
            _slowTicks++;
            if (_slowTicks % 3 == 0)
            {
                if (_tick < timeTrial.DemoData.Ticks.Count && _tick > 0)
                {
                    timeTrial.DemoData.Ticks[_tick - 1].ApplyToCar(carsInRace[playerCarIndex]);
                }

                carsInRace[playerCarIndex].Control
                    .Decode(timeTrial.GetTick(_tick) ?? (false, false, false, false, false));
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

            carsInRace[playerCarIndex].Control
                .Decode(timeTrial.GetTick(_tick) ?? (false, false, false, false, false));
            base.TimeTrialInRace();

            if (!_paused)
            {
                _tick++;
            }
        }
    }

    public override void KeyPressed(Keys key)
    {
        base.KeyPressed(key);

        if (key is Keys.ShiftKey or Keys.LShiftKey or Keys.RShiftKey)
        {
            _shift = true;
        }

        if (key is Keys.ControlKey or Keys.LControlKey or Keys.RControlKey)
        {
            _ctrl = true;
        }

        if (key == Keys.Space)
        {
            _paused = !_paused;
        }

        if (key == Keys.W)
        {
            if (_ctrl)
            {
                _tick += 63 * 60;
            }
            else if (_shift)
            {
                _tick += 63;
            }
            else
            {
                _tick++;
            }
        }

        if (key == Keys.S)
        {
            if (_ctrl)
            {
                _tick -= 63 * 60;
            }
            else if (_shift)
            {
                _tick -= 63;
            }
            else
            {
                _tick--;
            }
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

        if (key is Keys.ShiftKey or Keys.LShiftKey or Keys.RShiftKey)
        {
            _shift = false;
        }

        if (key is Keys.ControlKey or Keys.LControlKey or Keys.RControlKey)
        {
            _ctrl = false;
        }
    }

    public override void Render()
    {
        base.Render();

        G.SetColor(Color.White);
        G.DrawStringStroke($"Tick: {_tick} / {timeTrial.DemoData.Ticks.Count} ({_currentState})", 10, 250);
        G.SetColor(Color.Black);
        G.DrawString($"Tick: {_tick} / {timeTrial.DemoData.Ticks.Count} ({_currentState})", 10, 250);
    }
}