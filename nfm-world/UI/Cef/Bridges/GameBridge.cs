using System.Text.Json;
using Xilium.CefGlue;

namespace NFMWorld.UI.Cef;

/// <summary>
/// Bridges JS ↔ C# communication for the CEF browser. Owned by <see cref="CefRenderer"/>.
///
/// Message protocol:
///   JS → C#:  window.nfmw.call(methodName, ...args)
///             → CefProcessMessage "nfmw.call" → dispatched to registered handler
///   C# → JS:  window.__nfmwDispatch("{phaseId}:{eventType}", data)
///             (injected by NfmwLoadHandler on page load)
/// </summary>
public sealed class GameBridge
{
    /// <summary>
    /// Delegate for handling JS → C# messages. The string is a JSON-serialized
    /// payload (or null if no args were passed).
    /// </summary>
    public delegate void MessageHandler(string type, string? rawJson);

    private readonly Dictionary<string, MessageHandler> _handlers = new();

    /// <summary>
    /// Register a per-phase message handler. Only one handler per phase ID is allowed.
    /// </summary>
    public void Register(string phaseId, MessageHandler handler)
    {
        _handlers[phaseId] = handler;
    }

    /// <summary>
    /// Unregister a per-phase message handler.
    /// </summary>
    public void Unregister(string phaseId)
    {
        _handlers.Remove(phaseId);
    }

    /// <summary>
    /// Called from NfmwCefClient.OnProcessMessageReceived when the render
    /// process sends a message from JS.
    /// </summary>
    public void HandleProcessMessage(CefBrowser browser, CefProcessMessage message)
    {
        switch (message.Name)
        {
            case "nfmwCall":
                HandleNfmwCall(browser, message);
                break;
        }
    }

    /// <summary>
    /// Handle the unified nfmw.call(methodName, ...args) message.
    /// Dispatches to the currently-registered handler for the active phase.
    /// </summary>
    private void HandleNfmwCall(CefBrowser browser, CefProcessMessage message)
    {
        var args = message.Arguments;
        if (args == null || args.Count < 1) return;

        var methodName = args.GetString(0);

        // Extract payload from second argument (if present)
        string? rawJson = null;
        if (args.Count >= 2)
        {
            rawJson = args.GetString(1);
        }

        // Dispatch to the first registered handler.
        // In the single-browser model, only one phase is active at a time,
        // so we dispatch to whichever handler is registered.
        foreach (var handler in _handlers.Values)
        {
            handler(methodName, rawJson);
            break;
        }
    }

    /// <summary>
    /// Push an event from C# to JS. The JS side should listen via
    /// window.__nfmwDispatch("{phaseId}:{eventType}", callback).
    /// </summary>
    public void PushToJs(CefBrowser? browser, string phaseId, string eventType, object? data)
    {
        if (browser == null) return;

        var fullEvent = $"{phaseId}:{eventType}";
        var json = data != null ? JsonSerializer.Serialize(data) : "null";
        var script = $"window.__nfmwDispatch('{fullEvent}', {json});";
        browser.GetMainFrame().ExecuteJavaScript(script, null, 0);
    }
}
