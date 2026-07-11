using Xilium.CefGlue;

namespace NFMWorld.UI.Cef;

/// <summary>
/// Render process handler that sets up the V8 JavaScript context.
/// Injects the nfmw bridge object into the global scope when a JS context is created.
/// </summary>
internal sealed class NfmwRenderProcessHandler : CefRenderProcessHandler
{
    protected override void OnContextCreated(CefBrowser browser, CefFrame frame, CefV8Context context)
    {
        base.OnContextCreated(browser, frame, context);

        context.Enter();
        try
        {
            var global = context.GetGlobal();

            // Create the nfmw bridge object
            var bridge = CefV8Value.CreateObject();
            bridge.SetValue("getPlayerName",
                CefV8Value.CreateFunction("getPlayerName", new NfmwV8Handler(browser, "getPlayerName")),
                CefV8PropertyAttribute.None);
            bridge.SetValue("getSpeed",
                CefV8Value.CreateFunction("getSpeed", new NfmwV8Handler(browser, "getSpeed")),
                CefV8PropertyAttribute.None);

            global.SetValue("nfmw", bridge, CefV8PropertyAttribute.None);

            // Inject a helper that JS can call to subscribe to push events
            global.SetValue("__nfmwRegisterEventSink",
                CefV8Value.CreateFunction("__nfmwRegisterEventSink",
                    new NfmwV8Handler(browser, "__nfmwRegisterEventSink")),
                CefV8PropertyAttribute.None);
        }
        finally
        {
            context.Exit();
        }
    }

    protected override void OnContextReleased(CefBrowser browser, CefFrame frame, CefV8Context context)
    {
        base.OnContextReleased(browser, frame, context);
    }
}

/// <summary>
/// Handles V8 function calls from JavaScript. Sends the request to the browser
/// process via CefProcessMessage for processing by GameBridge.
/// </summary>
internal sealed class NfmwV8Handler : CefV8Handler
{
    private readonly CefBrowser _browser;
    private readonly string _functionName;

    public NfmwV8Handler(CefBrowser browser, string functionName)
    {
        _browser = browser;
        _functionName = functionName;
    }

    protected override bool Execute(string name, CefV8Value obj, CefV8Value[] arguments,
        out CefV8Value returnValue, out string exception)
    {
        returnValue = CefV8Value.CreateNull();
        exception = string.Empty;

        // For event sink registration, store callback reference
        if (_functionName == "__nfmwRegisterEventSink" && arguments.Length > 0 && arguments[0].IsObject)
        {
            // TODO: Store the event sink object for pushing C#→JS events
            returnValue = CefV8Value.CreateBool(true);
            return true;
        }

        // For other calls, send a process message to the browser process
        var msg = CefProcessMessage.Create(_functionName);
        var args = msg.Arguments;
        args.SetSize(arguments.Length);
        for (int i = 0; i < arguments.Length; i++)
        {
            if (arguments[i].IsString)
                args.SetString(i, arguments[i].GetStringValue());
            else if (arguments[i].IsDouble || arguments[i].IsInt || arguments[i].IsUInt)
                args.SetDouble(i, arguments[i].GetDoubleValue());
            else if (arguments[i].IsBool)
                args.SetBool(i, arguments[i].GetBoolValue());
            else
                args.SetNull(i);
        }

        _browser.GetMainFrame().SendProcessMessage(CefProcessId.Browser, msg);
        returnValue = CefV8Value.CreateNull(); // async — result comes via event sink
        return true;
    }
}
