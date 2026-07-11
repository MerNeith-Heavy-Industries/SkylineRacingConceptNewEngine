using Xilium.CefGlue;

namespace NFMWorld.UI.Cef;

/// <summary>
/// Handles CefProcessMessage calls from the JavaScript bridge.
/// JS → C# calls arrive as process messages dispatched from NfmwV8Handler.
/// This is a stub for the POC — extend with actual game state queries.
/// </summary>
public static class GameBridge
{
    /// <summary>
    /// Called from NfmwCefClient.OnProcessMessageReceived when the render
    /// process sends a message from JS.
    /// </summary>
    public static void HandleProcessMessage(CefBrowser browser, CefProcessMessage message)
    {
        switch (message.Name)
        {
            case "getPlayerName":
                HandleGetPlayerName(browser);
                break;
            case "getSpeed":
                HandleGetSpeed(browser);
                break;
            case "__nfmwRegisterEventSink":
                // Event sink registration — JS is ready to receive push events
                break;
        }
    }

    private static void HandleGetPlayerName(CefBrowser browser)
    {
        var name = "NFMW Player"; // TODO: wire to actual game state
        var response = $"if(window.nfmwEvents) window.nfmwEvents.emit('getPlayerName', {{value:'{name}'}});";
        browser.GetMainFrame().ExecuteJavaScript(response, null, 0);
    }

    private static void HandleGetSpeed(CefBrowser browser)
    {
        var speed = 0.0f; // TODO: wire to actual game state
        var response = $"if(window.nfmwEvents) window.nfmwEvents.emit('getSpeed', {{value:{speed}}});";
        browser.GetMainFrame().ExecuteJavaScript(response, null, 0);
    }

    /// <summary>
    /// Push live data from C# to JS. Call from the game loop each frame.
    /// </summary>
    public static void PushUpdate(CefBrowser? browser, float speed, string playerName, int frameCount)
    {
        if (browser == null) return;
        var script = $@"
if(window.nfmwEvents) {{
    window.nfmwEvents.emit('frameUpdate', {{speed:{speed},player:'{playerName}',frame:{frameCount}}});
}}";
        browser.GetMainFrame().ExecuteJavaScript(script, null, 0);
    }
}
