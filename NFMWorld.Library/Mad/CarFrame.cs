using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Maxine.Extensions;
using MemoryPack;
using NFMWorldLibrary.FixedMath;

namespace NFMWorldLibrary;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
[MemoryPackable]
public partial struct CarFrame
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [MemoryPackable]
    public partial struct BitFlags
    {
        public Nibble<uint> Values;
        public bool Right { readonly get => Values[0]; set => Values[0] = value; }
        public bool Left { readonly get => Values[1]; set => Values[1] = value; }
        public bool Up { readonly get => Values[2]; set => Values[2] = value; }
        public bool Down { readonly get => Values[3]; set => Values[3] = value; }
        public bool Handb { readonly get => Values[4]; set => Values[4] = value; }
        public bool Mtouch { readonly get => Values[5]; set => Values[5] = value; }
        public bool Wtouch { readonly get => Values[6]; set => Values[6] = value; }
        public bool Gtouch { readonly get => Values[7]; set => Values[7] = value; }
        public bool Pu { readonly get => Values[8]; set => Values[8] = value; }
        public bool Pd { readonly get => Values[9]; set => Values[9] = value; }
        public bool Pl { readonly get => Values[10]; set => Values[10] = value; }
        public bool Pr { readonly get => Values[11]; set => Values[11] = value; }
        public bool Pushed { readonly get => Values[12]; set => Values[12] = value; }
        public bool Newcar { readonly get => Values[13]; set => Values[13] = value; }
        public bool BadLanding { readonly get => Values[14]; set => Values[14] = value; }
        public bool Wasted { readonly get => Values[15]; set => Values[15] = value; }
        public bool Surfer { readonly get => Values[16]; set => Values[16] = value; }
    }
    
    public (fix64 X, fix64 Y, fix64 Z) CarPosition;
    public (fix64 Xz, fix64 Pxy, fix64 Pzy) CarRotation;
    public (InlineArray4<fix64> Scx, InlineArray4<fix64> Scy, InlineArray4<fix64> Scz) WheelVelocities;
    public fix64 Power;
    public int Damage;
    public (fix64 Ucomp, fix64 Dcomp, fix64 Lcomp, fix64 Rcomp) AngularVelocities;
    public (byte Lap, ushort CheckpointInlap) RacePosition;
    public (sbyte StuntType, fix64 Travxz, fix64 Travxy, fix64 Travzy) StuntState;
    public fix64 Powerup;
    public fix64 Speed;
    public (fix64 Mxz, fix64 Txz) XzReadings;
    public BitFlags TheBitFlags;

    public static CarFrame Create(IInGameCar car)
    {
        CarFrame entry = new CarFrame();

        entry.TheBitFlags.Up = car.Control.Up;
        entry.TheBitFlags.Down = car.Control.Down;
        entry.TheBitFlags.Left = car.Control.Left;
        entry.TheBitFlags.Right = car.Control.Right;
        entry.TheBitFlags.Handb = car.Control.Handb;
        entry.CarPosition.X = car.Position.X;
        entry.CarPosition.Y = car.Position.Y;
        entry.CarPosition.Z = car.Position.Z;
        entry.CarRotation.Xz = car.Rotation.Xz.Degrees;
        entry.CarRotation.Pxy = car.CarPhysics.Pxy;
        entry.CarRotation.Pzy = car.CarPhysics.Pzy;
        for (var i = 0; i < 4; i++)
        {
            entry.WheelVelocities.Scx[i] = car.CarPhysics.Scx[i];
            entry.WheelVelocities.Scy[i] = car.CarPhysics.Scy[i];
            entry.WheelVelocities.Scz[i] = car.CarPhysics.Scz[i];
        }
        entry.Power = car.CarPhysics.Power;
        entry.Damage = car.CarPhysics.Hitmag;
        entry.AngularVelocities.Ucomp = car.CarPhysics.Ucomp;
        entry.AngularVelocities.Dcomp = car.CarPhysics.Dcomp;
        entry.AngularVelocities.Lcomp = car.CarPhysics.Lcomp;
        entry.AngularVelocities.Rcomp = car.CarPhysics.Rcomp;
        entry.RacePosition.CheckpointInlap = car.currentCheckpoint;
        entry.RacePosition.Lap = car.currentLap;
        entry.StuntState.StuntType = car.CarPhysics.Loop;
        entry.StuntState.Travxz = car.CarPhysics.Travxz;
        entry.StuntState.Travxy = car.CarPhysics.Travxy;
        entry.StuntState.Travzy = car.CarPhysics.Travzy;
        entry.TheBitFlags.Surfer = car.CarPhysics.Surfer;
        entry.Powerup = car.CarPhysics.Powerup;
        entry.TheBitFlags.BadLanding = car.CarPhysics.BadLanding;
        entry.TheBitFlags.Wasted = car.CarPhysics.Wasted;
        entry.Speed = car.CarPhysics.Speed;
        entry.TheBitFlags.Mtouch = car.CarPhysics.Mtouch;
        entry.TheBitFlags.Wtouch = car.CarPhysics.Wtouch;
        entry.TheBitFlags.Gtouch = car.CarPhysics.Gtouch;
        entry.TheBitFlags.Pu = car.CarPhysics.Pu;
        entry.TheBitFlags.Pd = car.CarPhysics.Pd;
        entry.TheBitFlags.Pl = car.CarPhysics.Pl;
        entry.TheBitFlags.Pr = car.CarPhysics.Pr;
        entry.TheBitFlags.Pushed = car.CarPhysics.Pushed;
        entry.TheBitFlags.Newcar = car.CarPhysics.Newcar;
        entry.XzReadings.Mxz = car.CarPhysics.Mxz;
        entry.XzReadings.Txz = car.CarPhysics.Txz;

        return entry;
    }

    public void ApplyToCar(IInGameCar car)
    {
        car.Control.Up = TheBitFlags.Up;
        car.Control.Down = TheBitFlags.Down;
        car.Control.Left = TheBitFlags.Left;
        car.Control.Right = TheBitFlags.Right;
        car.Control.Handb = TheBitFlags.Handb;

        f64Vector3 pos = new(CarPosition.X, CarPosition.Y, CarPosition.Z);
        car.Position = pos;

        f64Euler rotation = new(f64AngleSingle.FromDegrees(CarRotation.Xz), f64AngleSingle.FromDegrees(CarRotation.Pzy), f64AngleSingle.FromDegrees(CarRotation.Pxy));
        car.Rotation = rotation;

        car.CarPhysics.Pxy = CarRotation.Pxy;
        car.CarPhysics.Pzy = CarRotation.Pzy;

        for (int i = 0; i < 4; i++)
        {
            car.CarPhysics.Scx[i] = WheelVelocities.Scx[i];
            car.CarPhysics.Scy[i] = WheelVelocities.Scy[i];
            car.CarPhysics.Scz[i] = WheelVelocities.Scz[i];
        }

        car.CarPhysics.Power = Power;
        car.CarPhysics.Hitmag = Damage;
        car.CarPhysics.Ucomp = AngularVelocities.Ucomp;
        car.CarPhysics.Dcomp = AngularVelocities.Dcomp;
        car.CarPhysics.Lcomp = AngularVelocities.Lcomp;
        car.CarPhysics.Rcomp = AngularVelocities.Rcomp;

        car.CarPhysics.Loop = StuntState.StuntType;
        car.CarPhysics.Travxz = StuntState.Travxz;
        car.CarPhysics.Travxy = StuntState.Travxy;
        car.CarPhysics.Travzy = StuntState.Travzy;
        car.CarPhysics.Surfer = TheBitFlags.Surfer;

        car.CarPhysics.Powerup = Powerup;
        car.CarPhysics.BadLanding = TheBitFlags.BadLanding;
        car.CarPhysics.Wasted = TheBitFlags.Wasted;
        car.CarPhysics.Speed = Speed;
        car.CarPhysics.Pushed = TheBitFlags.Pushed;
        car.CarPhysics.Newcar = TheBitFlags.Newcar;

        car.CarPhysics.Mtouch = TheBitFlags.Mtouch;
        car.CarPhysics.Wtouch = TheBitFlags.Wtouch;
        car.CarPhysics.Gtouch = TheBitFlags.Gtouch;

        car.CarPhysics.Pu = TheBitFlags.Pu;
        car.CarPhysics.Pd = TheBitFlags.Pd;
        car.CarPhysics.Pl = TheBitFlags.Pl;
        car.CarPhysics.Pr = TheBitFlags.Pr;

        car.CarPhysics.Mxz = XzReadings.Mxz;
        car.CarPhysics.Txz = XzReadings.Txz;
    }
}