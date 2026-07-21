using System.Text.Json;
using MemoryPack;
using NFMWorld.DriverInterface;

namespace NFMWorld.UI.Cef;

/// <summary>
/// Bridge for in-race HUD. Pushes HudState records each frame (60 fps).
/// Does NOT enable CEF input — clicks pass through to the game.
/// </summary>
public sealed class HudBridge() : PhaseBridge("race")
{
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
        PushMemoryPack("hudState", state);
    }
}
