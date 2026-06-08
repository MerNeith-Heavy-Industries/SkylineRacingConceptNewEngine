using NFMWorld.DriverInterface;
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
    private bool _simulating;

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
        if (!_simulating || _paused)
        {
            if (_tick < timeTrial.DemoData.Ticks.Count && _tick > 0)
            {
                timeTrial.DemoData.Ticks[_tick - 1].ApplyToCar(carsInRace[playerCarIndex]);
            }
        }

        carsInRace[playerCarIndex].Control
            .Decode(timeTrial.GetTick(_tick) ?? (false, false, false, false, false));
        base.TimeTrialInRace();

        if (_slow && !_paused)
        {
            _slowTicks++;
            if (_slowTicks % 3 == 0)
            {
                _tick++;
            }
        }
        else
        {
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

        if (key == Keys.M)
        {
            _simulating = !_simulating;
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

        G.SetFont(new Font(FontFamily.RobotoMono, FontStyle.Plain, 16));
        G.SetColor(Color.Black);
        G.DrawStringStroke($"Tick: {_tick} / {timeTrial.DemoData.Ticks.Count} ({_currentState}) (Simulating: {_simulating})", 10, 250);
        G.SetColor(Color.White);
        G.DrawString($"Tick: {_tick} / {timeTrial.DemoData.Ticks.Count} ({_currentState}) (Simulating: {_simulating})", 10, 250);
    }
}