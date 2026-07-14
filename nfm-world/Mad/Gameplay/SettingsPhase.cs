using Microsoft.Xna.Framework.Graphics;
using NFMWorld.DriverInterface;
using NFMWorld.UI;
using NFMWorld.UI.Cef;

namespace NFMWorld.Gameplay;

/// <summary>
/// CEF-backed settings phase. Replaces the legacy ImGui SettingsMenu.
/// Pushes onto the phase stack from MainMenuPhase when the user clicks SETTINGS.
/// </summary>
public class SettingsPhase : BaseStageRenderingPhase
{
    private readonly SettingsBridge _bridge = new();

    public SettingsPhase(GraphicsDevice graphicsDevice, string stageName) : base(graphicsDevice, stageName)
    {
        CefBridge = _bridge;

        _bridge.CloseRequested += OnCloseRequested;
        _bridge.RestartConfirmed += OnRestartConfirmed;
    }

    private void OnCloseRequested()
    {
        GameSparker.PopPhase();
    }

    private void OnRestartConfirmed()
    {
        System.Environment.Exit(0);
    }

    public override void KeyPressed(Key key, bool imguiWantsKeyboard, in Keys keys)
    {
        base.KeyPressed(key, imguiWantsKeyboard, keys);

        if (_bridge.IsCapturing)
        {
            _bridge.HandleCapturedKey(key);
        }
    }

    public override void Exit()
    {
        _bridge.CloseRequested -= OnCloseRequested;
        _bridge.RestartConfirmed -= OnRestartConfirmed;
        base.Exit();
    }
}
