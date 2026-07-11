using Xilium.CefGlue;

namespace NFMWorld.UI.Cef;

/// <summary>
/// Minimal CefClient providing the off-screen render handler and load handler.
/// </summary>
internal sealed class NfmwCefClient(NfmwCefRenderHandler renderHandler) : CefClient
{
    protected override CefRenderHandler GetRenderHandler() => renderHandler;

    protected override CefLoadHandler GetLoadHandler() => new NfmwLoadHandler();

    protected override bool OnProcessMessageReceived(CefBrowser browser, CefFrame frame,
        CefProcessId sourceProcess, CefProcessMessage message)
    {
        // Forward process messages from the render process to GameBridge
        GameBridge.HandleProcessMessage(browser, message);
        return true;
    }
}

/// <summary>
/// Tracks load state for the browser.
/// </summary>
internal sealed class NfmwLoadHandler : CefLoadHandler
{
    protected override void OnLoadEnd(CefBrowser browser, CefFrame frame, int httpStatusCode)
    {
        if (frame.IsMain)
        {
            // Inject nfmwEvents bridge for C#→JS push communication,
            // but only if the page hasn't already defined it (preserve page-defined listeners).
            const string script = 
                """
                if (!window.nfmwEvents) {
                    window.nfmwEvents = {
                        _listeners: {},
                        on: function(event, callback) {
                            if (!this._listeners[event]) this._listeners[event] = [];
                            this._listeners[event].push(callback);
                        },
                        emit: function(event, data) {
                            var handlers = this._listeners[event];
                            if (handlers) { handlers.forEach(function(h) { h(data); }); }
                        }
                    };
                }
                window.nfmwEvents.emit('ready', {});

                """;
            frame.ExecuteJavaScript(script, null, 0);
        }

        base.OnLoadEnd(browser, frame, httpStatusCode);
    }
}
