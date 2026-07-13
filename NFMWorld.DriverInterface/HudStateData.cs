using MemoryPack;

namespace NFMWorld.DriverInterface;

/// <summary>
/// Per-frame HUD state sent from the gamemode to the CEF race overlay.
/// </summary>
[MemoryPackable]
[GenerateTypeScript]
public partial class HudStateData
{
    public double Speed { get; set; }
    public double Power { get; set; }
    public double Damage { get; set; }
    public double MaxPower { get; set; }
    public int Lap { get; set; }
    public int TotalLaps { get; set; }
    public double LapTime { get; set; }
    public double BestLapTime { get; set; }
    public double[] Splits { get; set; } = [];
    public int Position { get; set; }
    public int TotalRacers { get; set; }
    public string? StateText { get; set; }
    public double? StateTextDuration { get; set; }
}
