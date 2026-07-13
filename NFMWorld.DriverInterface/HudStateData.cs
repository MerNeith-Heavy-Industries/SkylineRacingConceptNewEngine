using MemoryPack;

namespace NFMWorld.DriverInterface;

/// <summary>
/// Per-frame HUD state sent from the gamemode to the CEF race overlay.
/// </summary>
[MemoryPackable]
[GenerateTypeScript]
public partial class HudStateData
{
    public float Speed { get; set; }
    public float Power { get; set; }
    public float Damage { get; set; }
    public int Lap { get; set; }
    public int TotalLaps { get; set; }
    public int LapTime { get; set; }
    public int Position { get; set; }
    public int TotalRacers { get; set; }
    public string? StateText { get; set; }
    public DateTime? StateTextEndsAt { get; set; }
    public int? LapDiffMs { get; set; }
    public int? LastLapDiffMs { get; set; }
    public int? ChkDiffMs { get; set; }
    public int? LastChkDiffMs { get; set; }
    public int CountdownTimer { get; set; }
}
