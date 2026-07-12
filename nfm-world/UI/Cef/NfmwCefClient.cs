using Xilium.CefGlue;

namespace NFMWorld.UI.Cef;

/// <summary>
/// Minimal CefClient providing the off-screen render handler, load handler,
/// and process message routing through the owning CefRenderer's GameBridge.
/// </summary>
internal sealed class NfmwCefClient : CefClient
{
    private readonly NfmwCefRenderHandler _renderHandler;
    private readonly CefRenderer _cefRenderer;

    public NfmwCefClient(NfmwCefRenderHandler renderHandler, CefRenderer cefRenderer)
    {
        _renderHandler = renderHandler;
        _cefRenderer = cefRenderer;
    }

    protected override CefRenderHandler GetRenderHandler() => _renderHandler;

    protected override CefLoadHandler GetLoadHandler() => new NfmwLoadHandler();

    protected override bool OnProcessMessageReceived(CefBrowser browser, CefFrame frame,
        CefProcessId sourceProcess, CefProcessMessage message)
    {
        // Diagnostic: log every received process message.
        // Remove once JS→C# messaging is confirmed working.
        System.Console.WriteLine($"[CEF] OnProcessMessageReceived: {message.Name} (args: {message.Arguments.Count})");

        // Forward process messages from the render process to the renderer's GameBridge
        _cefRenderer.Bridge.HandleProcessMessage(browser, message);
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

                // __nfmwDispatch is the primary C#→JS push channel.
                // Events are named "{phaseId}:{eventType}" — JS can listen with:
                //   window.__nfmwDispatch = function(event, data) { ... }
                // or via nfmwEvents:
                //   window.nfmwEvents.on(event, function(data) { ... })
                if (!window.__nfmwDispatch) {
                    window.__nfmwDispatch = function(event, data) {
                        if (window.nfmwEvents) {
                            window.nfmwEvents.emit(event, data);
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
