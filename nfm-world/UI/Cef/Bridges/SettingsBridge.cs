using System.Text.Json;
using MemoryPack;
using NFMWorld.DriverInterface;

namespace NFMWorld.UI.Cef;

/// <summary>
/// Bridge for SettingsPhase — handles settings read/write, key binding capture,
/// and confirmation dialogs.
/// </summary>
public sealed class SettingsBridge() : PhaseBridge("settings")
{
    public override bool EnableInput => true;

    private string? _capturingAction;
    private string? _originalConfig;

    protected override void OnMessage(string type, JsonElement? args)
    {
        switch (type)
        {
            case "getConfig":
                PushInitialState();
                break;
            case "applySetting":
                ApplySettingFromJs(args);
                break;
            case "saveConfig":
                var requireRestart = SettingsMenu.SaveConfigAndCheckRestart();
                // Snapshot the newly-saved state as the new baseline
                _originalConfig = null; // will be re-captured on next getConfig if needed
                if (requireRestart)
                    Push("requireRestart", true);
                else
                    Push("saved", true);
                break;
            case "close":
                // Cancel: restore settings to the state when the page was opened
                if (_originalConfig != null)
                    SettingsMenu.LoadConfigFromSnapshot(_originalConfig);
                CloseRequested?.Invoke();
                break;
            case "restartNow":
                RestartConfirmed?.Invoke();
                break;
            case "startCapture":
                if (args is { } a && a.TryGetProperty("action", out var action))
                    _capturingAction = action.GetString();
                break;
            case "stopCapture":
                _capturingAction = null;
                break;
            case "resetDefaults":
                HandleResetDefaults(args);
                break;
        }
    }

    /// <summary>
    /// Push current settings snapshot and available options to JS.
    /// Called on initial registration and when JS requests a refresh.
    /// </summary>
    public void PushInitialState()
    {
        // Capture the baseline config before any JS-driven changes
        _originalConfig ??= SettingsMenu.SaveConfigToString();

        var snapshot = SettingsMenu.GetCurrentSnapshot();
        PushMemoryPack("config", snapshot);

        var options = SettingsMenu.GetAvailableOptions();
        PushMemoryPack("options", options);
    }

    protected override void OnRegistered()
    {
        SettingsMenu.ResolutionsChanged += OnResolutionsChanged;
    }

    protected override void OnUnregistered()
    {
        SettingsMenu.ResolutionsChanged -= OnResolutionsChanged;
    }

    private void OnResolutionsChanged()
    {
        // Push updated options (new resolution added) and current config
        var options = SettingsMenu.GetAvailableOptions();
        PushMemoryPack("options", options);
        var snapshot = SettingsMenu.GetCurrentSnapshot();
        PushMemoryPack("config", snapshot);
    }

    /// <summary>
    /// Set the current key capture action. Called by SettingsPhase.KeyPressed
    /// when a key is pressed during capture.
    /// </summary>
    public void HandleCapturedKey(Key key)
    {
        if (_capturingAction == null) return;

        if (key == Key.Escape)
        {
            _capturingAction = null;
            Push("keyCaptured", new { action = (string?)null, keyCode = (int)Key.None, cancelled = true });
            return;
        }

        // Resolve conflicts: clear any existing binding that uses this key
        var allProps = typeof(SettingsMenu.KeyBindings).GetProperties();
        foreach (var prop in allProps)
        {
            if (prop.Name != _capturingAction
                && prop.GetValue(SettingsMenu.Bindings) is Key existingKey
                && existingKey == key)
            {
                prop.SetValue(SettingsMenu.Bindings, Key.None);
            }
        }

        // Set the new binding
        var property = typeof(SettingsMenu.KeyBindings).GetProperty(_capturingAction);
        property?.SetValue(SettingsMenu.Bindings, key);

        var capturedAction = _capturingAction;
        _capturingAction = null;
        Push("keyCaptured", new { action = capturedAction, keyCode = (int)key, cancelled = false });
    }

    public bool IsCapturing => _capturingAction != null;

    private void ApplySettingFromJs(JsonElement? args)
    {
        if (args is not { } a || !a.TryGetProperty("key", out var keyProp))
            return;

        var key = keyProp.GetString() ?? "";
        SettingsMenu.ApplySetting(key, a);
    }

    private void HandleResetDefaults(JsonElement? args)
    {
        if (args is not { } a || !a.TryGetProperty("section", out var sectionProp))
            return;

        var section = sectionProp.GetString() ?? "";
        switch (section)
        {
            case "keyboard":
                SettingsMenu.Bindings = new SettingsMenu.KeyBindings();
                break;
            case "camera":
                SettingsMenu.ResetCameraDefaults();
                break;
        }
        PushInitialState();
    }

    public event Action? CloseRequested;

    /// <summary>Fired when the user confirms they want to restart now.</summary>
    public event Action? RestartConfirmed;
}

// ── MemoryPack data models ────────────────────────────────────────

/// <summary>
/// Complete snapshot of all current settings, sent from C# to JS.
/// </summary>
[MemoryPackable]
[GenerateTypeScript]
public sealed partial class SettingsSnapshot
{
    // Video
    public int SelectedRenderer { get; set; }
    public int SelectedResolution { get; set; }
    public int SelectedDisplayMode { get; set; }
    public bool Vsync { get; set; }
    public int FpsLimit { get; set; }
    public int Antialias { get; set; }
    public int ShadowCascadeLevel { get; set; }
    public int ShadowResolution { get; set; }
    public int RenderDistance { get; set; }
    public bool LowLatency { get; set; }
    public float LineWidth { get; set; }

    // Audio
    public float MasterVolume { get; set; }
    public float MusicVolume { get; set; }
    public float EffectsVolume { get; set; }
    public bool MuteAll { get; set; }
    public bool RemasteredMusic { get; set; }

    // Game (Camera)
    public float Fov { get; set; }
    public int FollowY { get; set; }
    public int FollowZ { get; set; }
    public bool SmoothFov { get; set; }

    // Key bindings
    public KeyBindingData[] KeyBindings { get; set; } = [];
}

/// <summary>
/// Single key binding sent to JS.
/// </summary>
[MemoryPackable]
[GenerateTypeScript]
public sealed partial class KeyBindingData
{
    /// <summary>Property name on KeyBindings (e.g., "Accelerate").</summary>
    public string Action { get; set; } = "";

    /// <summary>Human-readable display name (e.g., "Accelerate").</summary>
    public string DisplayName { get; set; } = "";

    /// <summary>SDL Key enum integer value.</summary>
    public int KeyCode { get; set; }
}

/// <summary>
/// Lists of valid choices for each dropdown/slider, sent once on enter.
/// </summary>
[MemoryPackable]
[GenerateTypeScript]
public sealed partial class AvailableOptions
{
    public string[] Renderers { get; set; } = [];
    public string[] Resolutions { get; set; } = [];
    public string[] DisplayModes { get; set; } = [];
    public string[] AntialiasModes { get; set; } = [];
    public string[] ShadowCascadeLevels { get; set; } = [];
    public string[] ShadowResolutions { get; set; } = [];
    public string[] RenderDistanceNames { get; set; } = [];
}
