using System.Text.Json;

namespace NFMWorld.UI.Cef;

/// <summary>
/// Bridge for in-race HUD. Pushes HudState records each frame (60 fps).
/// Does NOT enable CEF input — clicks pass through to the game.
/// </summary>
public sealed class HudBridge() : PhaseBridge("race")
{
    public override string? PageUrl => CefRenderer.ResolveBasePageUrl() + "#/race";
    public override bool EnableInput => false;

    protected override void OnMessage(string type, JsonElement? args)
    {
        // HUD is read-only for now — no JS → C# messages expected.
        // Add handlers here if interactive HUD elements are added later.
    }

    /// <summary>
    /// Push the full HUD state to JS. Call every frame from GameTick().
    /// </summary>
    public void PushHudState(HudStateData state)
    {
        Push("hudState", state);
    }
}

/// <summary>
/// Per-frame HUD state sent to the race JS page.
/// </summary>
public sealed class HudStateData
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
