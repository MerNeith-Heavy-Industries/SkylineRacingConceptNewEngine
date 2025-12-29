using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using MessagePack;
using NFMWorld.Mad;
using SoftFloat;

namespace NFMWorld.Mad;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PlayerState
{
    public required DemoEntry DemoEntry;
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
        state.DemoEntry.ApplyToCar(c);
    }
    
    public static PlayerState CreateFrom(uint ticks, InGameCar car)
    {
        return new PlayerState
        {
            DemoEntry = DemoEntry.Create(car),
            CurrentTime = DateTimeOffset.UtcNow,
            Ticks = ticks
        };
    }
}
