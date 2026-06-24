using NFMWorld.DriverInterface;
using NFMWorld.Reactor;
using NFMWorldLibrary;

namespace NFMWorld.UI.Hud;

/// <summary>
/// Immutable snapshot of all HUD state. Updated by the gamemode and consumed
/// by HUD components via <see cref="Context"/>.
/// </summary>
public record HudState(
    #region CenterText
    string CenterText = "",
    Color CenterTextColor = default,
    FontFamily? CenterTextFontFamily = null,
    FontStyle? CenterTextFontStyle = null,
    float? CenterTextFontSize = null,
    Color CenterTextStrokeColor = default,
    float CenterTextOpacity = 1f,
    #endregion

    #region Bars
    float DamageFillAmount = 0f,
    float PowerFillAmount = 1f,
    Color DamageColor = default,
    Color PowerColor = default,
    #endregion

    #region Side Display
    int CurrentLap = 0,
    int TotalLaps = 0,
    string TimeText = "0:00.000",
    string LapTimeText = "",
    #endregion

    #region Splits
    bool CheckpointSplitsVisible = false,
    string CheckpointSplitsText = "",
    Color? CheckpointSplitsColor = null,
    
    Color? LapSplitsColor = null,
    string LapSplitsText = ""
    #endregion
)
{
    /// <summary>Shared HUD state context.</summary>
    public static readonly Context<HudState> Context = new(new HudState());
}
