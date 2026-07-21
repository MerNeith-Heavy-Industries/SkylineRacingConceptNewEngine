using System.Text.Json;
using Xilium.CefGlue;

namespace NFMWorld.UI.Cef;

/// <summary>
/// Bridges JS ↔ C# communication for the CEF browser. Owned by <see cref="CefRenderer"/>.
///
/// Message protocol:
///   JS → C#:  window.__nfmwCall(methodName, ...args)
///             → CefProcessMessage "nfmwCall" → dispatched to registered handler
///   C# → JS:  CefProcessMessage "nfmwPush" → NfmwRenderProcessHandler
///             → dispatches to window.__nfmwDispatch("{phaseId}:{eventType}", data)
///             Binary payloads use SetBinary on the process message args natively.
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
    /// Push an event from C# to JS via CefProcessMessage (avoids string
    /// escaping issues and supports binary payloads natively).
    /// The JS side receives this via window.__nfmwDispatch(event, data).
    /// </summary>
    public static void PushToJs(CefBrowser? browser, string phaseId, string eventType, object? data)
    {
        if (browser == null) return;

        var msg = CefProcessMessage.Create("nfmwPush");
        var args = msg.Arguments;

        // [0] = full event name ("phaseId:eventType")
        args.SetString(0, $"{phaseId}:{eventType}");

        // [1] = JSON payload (string on JS side)
        args.SetString(1, data != null ? JsonSerializer.Serialize(data) : "null");

        browser.GetMainFrame().SendProcessMessage(CefProcessId.Renderer, msg);
    }

    /// <summary>
    /// Push an event from C# to JS via CefProcessMessage (avoids string
    /// escaping issues and supports binary payloads natively).
    /// The JS side receives this via window.__nfmwDispatch(event, data).
    /// </summary>
    public static void PushToJs(CefBrowser? browser, string phaseId, string eventType, byte[] binary)
    {
        if (browser == null) return;

        var msg = CefProcessMessage.Create("nfmwPush");
        var args = msg.Arguments;

        // [0] = full event name ("phaseId:eventType")
        args.SetString(0, $"{phaseId}:{eventType}");

        // [1] = binary payload (uint8array on JS side)
        args.SetBinary(1, CefBinaryValue.Create(binary));

        browser.GetMainFrame().SendProcessMessage(CefProcessId.Renderer, msg);
    }
}
