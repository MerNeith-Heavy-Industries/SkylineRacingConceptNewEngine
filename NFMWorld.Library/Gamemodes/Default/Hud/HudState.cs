using NFMWorld.DriverInterface;
using NFMWorld.Reactor;
using NFMWorldLibrary;

namespace NFMWorld.UI.Hud;

/// <summary>
/// Immutable snapshot of all HUD state. Updated by the gamemode and consumed
/// by HUD components via <see cref="HUDContexts.Hud"/>.
/// </summary>
public record HudState(
    string CenterText = "",
    Color CenterTextColor = default,
    Font CenterTextFont = default,
    Color CenterTextStrokeColor = default,
    float CenterTextOpacity = 1f,
    float DamageFillAmount = 0f,
    float PowerFillAmount = 0f,
    Color DamageColor = default,
    Color PowerColor = default,
    int CurrentLap = 0,
    int TotalLaps = 0,
    string TimeText = "0:00.000",
    string LapTimeText = "",
    string CheckpointSplitsText = ""
);

/// <summary>
/// Context keys for HUD state distribution.
/// </summary>
public static class HUDContexts
{
    /// <summary>Shared HUD state context.</summary>
    public static readonly Context<HudState> Hud = new(new HudState());
}
