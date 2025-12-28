using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using MessagePack;
using NFMWorld.Mad;
using SoftFloat;

namespace NFMWorld.Mad;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PlayerState
{
    public required bool Left;
    public required bool Right;
    public required bool Up;
    public required bool Down;
    public required bool Handb;
    public required bool Newcar;
    public required bool Mtouch;
    public required bool Wtouch;
    public required bool Pushed;
    public required bool Gtouch;
    public required bool pl;
    public required bool pr;
    public required bool pd;
    public required bool pu;
    public required bool dest;
    public required fix64 x;
    public required fix64 y;
    public required fix64 z;
    public required fix64 xz;
    public required fix64 xy;
    public required fix64 zy;
    public required fix64 speed;
    public required fix64 power;
    public required fix64 mxz;
    public required fix64 pzy;
    public required fix64 pxy;
    public required fix64 txz;
    public required int loop;
    public required int wxz;
    public required int pcleared;
    public required int clear;
    public required int nlaps;
    public required uint Ticks;
    private ulong _currentTimeInMs;

    [IgnoreMember]
    public required DateTimeOffset CurrentTime
    {
        readonly get => DateTimeOffset.FromUnixTimeMilliseconds((long)_currentTimeInMs);
        set => _currentTimeInMs = (ulong)value.ToUnixTimeMilliseconds();
    }
    
    public static void ApplyTo(PlayerState state, InGameCar c)
    {
        c.Control.Left = state.Left;
        c.Control.Right = state.Right;
        c.Control.Up = state.Up;
        c.Control.Down = state.Down;
        c.Control.Handb = state.Handb;
        c.Mad.Newcar = state.Newcar;
        c.Mad.Mtouch = state.Mtouch;
        c.Mad.Wtouch = state.Wtouch;
        c.Mad.Pushed = state.Pushed;
        c.Mad.Gtouch = state.Gtouch;
        c.Mad.Pl = state.pl;
        c.Mad.Pr = state.pr;
        c.Mad.Pd = state.pd;
        c.Mad.Pu = state.pu;
        c.CarRef.Position = new Vector3((float)state.x, (float)state.y, (float)state.z);
        c.CarRef.Rotation = new Euler(AngleSingle.FromDegrees(state.xz), AngleSingle.FromDegrees(state.zy), AngleSingle.FromDegrees(state.xy));
        c.Mad.Speed = state.speed;
        c.Mad.Power = state.power;
        c.Mad.Mxz = state.mxz;
        c.Mad.Pzy = state.pzy;
        c.Mad.Pxy = state.pxy;
        c.Mad.Txz = state.txz;
        c.Mad.Loop = state.loop;
        c.CarRef.TurningWheelAngle = c.CarRef.TurningWheelAngle with { Xz = AngleSingle.FromDegrees(state.wxz) };
        c.Mad.Pcleared = state.pcleared;
        c.Mad.Clear = state.clear;
        c.Mad.Nlaps = state.nlaps;
    }
    
    public static PlayerState CreateFrom(uint ticks, InGameCar car)
    {
        return new PlayerState
        {
            Left = car.Control.Left,
            Right = car.Control.Right,
            Up = car.Control.Up,
            Down = car.Control.Down,
            Handb = car.Control.Handb,
            Newcar = car.Mad.Newcar,
            Mtouch = car.Mad.Mtouch,
            Wtouch = car.Mad.Wtouch,
            Pushed = car.Mad.Pushed,
            Gtouch = car.Mad.Gtouch,
            pl = car.Mad.Pl,
            pr = car.Mad.Pr,
            pd = car.Mad.Pd,
            pu = car.Mad.Pu,
            x = (fix64)car.CarRef.Position.X,
            y = (fix64)car.CarRef.Position.Y,
            z = (fix64)car.CarRef.Position.Z,
            xz = car.CarRef.Rotation.Xz.DegreesSFloat,
            xy = car.CarRef.Rotation.Xy.DegreesSFloat,
            zy = car.CarRef.Rotation.Zy.DegreesSFloat,
            speed = car.Mad.Speed,
            power = car.Mad.Power,
            mxz = car.Mad.Mxz,
            pzy = car.Mad.Pzy,
            pxy = car.Mad.Pxy,
            txz = car.Mad.Txz,
            loop = car.Mad.Loop,
            wxz = (int)car.CarRef.TurningWheelAngle.Xz.DegreesSFloat,
            pcleared = car.Mad.Pcleared,
            clear = car.Mad.Clear,
            nlaps = car.Mad.Nlaps,
            dest = false,
            Ticks = ticks,
            CurrentTime = DateTimeOffset.UtcNow
        };
    }
}
