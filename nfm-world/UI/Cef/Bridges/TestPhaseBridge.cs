using System.Text.Json;

namespace NFMWorld.UI.Cef;

/// <summary>
/// Bridge for XamlTestPhase — dev-only test page with a counter.
/// </summary>
public sealed class TestPhaseBridge() : PhaseBridge("test")
{
    public override string? PageUrl => CefRenderer.ResolveBasePageUrl() + "#/test";
    public override bool EnableInput => true;

    protected override void OnMessage(string type, JsonElement? args)
    {
        switch (type)
        {
            case "increment":
                IncrementRequested?.Invoke();
                break;
            case "back":
                BackRequested?.Invoke();
                break;
        }
    }

    /// <summary>
    /// Push the current counter value to JS.
    /// </summary>
    public void PushCounter(int value)
    {
        Push("counter", new { value });
    }

    public event Action? IncrementRequested;
    public event Action? BackRequested;
}
