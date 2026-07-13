using System.Text.Json;

namespace NFMWorld.UI.Cef;

/// <summary>
/// Bridge for MainMenuPhase — handles navigation and account state.
/// </summary>
public sealed class MainMenuBridge : PhaseBridge
{
    public override string? PageUrl => CefRenderer.ResolveBasePageUrl() + "#/main-menu";
    public override bool EnableInput => true;

    public MainMenuBridge() : base("main-menu") { }

    protected override void OnMessage(string type, JsonElement? args)
    {
        switch (type)
        {
            case "navigate":
                if (args is { } a && a.TryGetProperty("page", out var page))
                {
                    NavigateRequested?.Invoke(page.GetString() ?? "");
                }
                break;
            case "logout":
                LogoutRequested?.Invoke();
                break;
        }
    }

    /// <summary>
    /// Push account state to the menu. Call whenever the active account changes.
    /// </summary>
    public void PushAccount(string? name, bool isLoggedIn)
    {
        Push("account", new
        {
            name = name ?? "",
            isLoggedIn,
            avatarUrl = (string?)null,
        });
    }

    public event Action<string>? NavigateRequested;
    public event Action? LogoutRequested;
}
