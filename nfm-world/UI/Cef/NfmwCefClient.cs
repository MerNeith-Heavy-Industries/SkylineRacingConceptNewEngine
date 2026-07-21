using NFMWorldLibrary;
using Xilium.CefGlue;

namespace NFMWorld.UI.Cef;

/// <summary>
/// Minimal CefClient providing the off-screen render handler, load handler,
/// and process message routing through the owning CefRenderer's GameBridge.
/// </summary>
internal sealed class NfmwCefClient(NfmwCefRenderHandler renderHandler, CefRenderer cefRenderer)
    : CefClient
{
    protected override CefRenderHandler GetRenderHandler() => renderHandler;

    protected override CefLoadHandler GetLoadHandler() => new NfmwLoadHandler();

    protected override bool OnProcessMessageReceived(CefBrowser browser, CefFrame frame,
        CefProcessId sourceProcess, CefProcessMessage message)
    {
        // Diagnostic: log every received process message.
        // Remove once JS→C# messaging is confirmed working.
        Logging.Debug($"[CEF] OnProcessMessageReceived: {message.Name} (args: {message.Arguments?.Count})");

        // Forward process messages from the render process to the renderer's GameBridge
        cefRenderer.Bridge.HandleProcessMessage(browser, message);
        return true;
    }
}

/// <summary>
/// Tracks load state for the browser. Injects the nfmwEvents event emitter
/// and the __nfmwDispatch bridge for C# → JS push communication on page load.
/// </summary>
internal sealed class NfmwLoadHandler : CefLoadHandler
{
    protected override void OnLoadEnd(CefBrowser browser, CefFrame frame, int httpStatusCode)
    {
        if (frame.IsMain)
        {
            // Inject nfmwEvents bridge + __nfmwDispatch for C#→JS push communication.
            // Preserves page-defined listeners if they already exist.
            const string script = "window.nfmwEvents.emit('ready', {});";
            frame.ExecuteJavaScript(script, null, 0);
        }

        base.OnLoadEnd(browser, frame, httpStatusCode);
    }
}
