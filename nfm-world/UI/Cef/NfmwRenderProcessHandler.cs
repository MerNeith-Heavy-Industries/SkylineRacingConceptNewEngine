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

        // Diagnostic trace
        try { System.IO.File.AppendAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "nfmw_v8_trace.txt"), $"[{DateTime.UtcNow:O}] OnContextCreated: url={frame.Url}\n"); } catch { }

        context.Enter();
        try
        {
            var global = context.GetGlobal();
            var attrs = CefV8PropertyAttribute.ReadOnly | CefV8PropertyAttribute.DontDelete;

            // Register nfmw.call directly on window (flat naming, matches CefMessageRouter pattern).
            // The JS bridge will call window.__nfmwCall(methodName, jsonPayload).
            using (var func = CefV8Value.CreateFunction("__nfmwCall", new NfmwV8Handler("nfmwCall")))
            {
                global.SetValue("__nfmwCall", func, attrs);
            }

            // Legacy POC functions — kept for backward compatibility
            using (var getPlayerName = CefV8Value.CreateFunction("getPlayerName", new NfmwV8Handler("getPlayerName")))
            {
                global.SetValue("__nfmwGetPlayerName", getPlayerName, attrs);
            }
            using (var getSpeed = CefV8Value.CreateFunction("getSpeed", new NfmwV8Handler("getSpeed")))
            {
                global.SetValue("__nfmwGetSpeed", getSpeed, attrs);
            }

            try { System.IO.File.AppendAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "nfmw_v8_trace.txt"), $"[{DateTime.UtcNow:O}] OnContextCreated: registered __nfmwCall on window\n"); } catch { }
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
    private readonly string _functionName;

    public NfmwV8Handler(string functionName)
    {
        _functionName = functionName;
    }

    protected override bool Execute(string name, CefV8Value obj, CefV8Value[] arguments,
        out CefV8Value returnValue, out string exception)
    {
        returnValue = CefV8Value.CreateNull();
        exception = string.Empty;

        try
        {
            // Diagnostic: write to a temp file to confirm V8 handler is invoked.
            // The render process stdout doesn't always surface to the browser
            // process console, so we use a file trace instead.
            try
            {
                System.IO.File.AppendAllText(
                    System.IO.Path.Combine(System.IO.Path.GetTempPath(), "nfmw_v8_trace.txt"),
                    $"[{DateTime.UtcNow:O}] Execute: func={_functionName}, name={name}, args={arguments.Length}\n");
            }
            catch { /* best-effort */ }

            // For event sink registration, store callback reference
            if (_functionName == "__nfmwRegisterEventSink" && arguments.Length > 0 && arguments[0].IsObject)
            {
                returnValue = CefV8Value.CreateBool(true);
                return true;
            }

            // Get browser and frame from the current V8 context.
            // Use browser.GetFrame(frameId) pattern from CefMessageRouter —
            // this is the reliable way to get a frame that can send process
            // messages from the render process.
            var ctx = CefV8Context.GetCurrentContext();
            if (ctx == null)
            {
                exception = "No current V8 context";
                return false;
            }

            var browser = ctx.GetBrowser();
            if (browser == null)
            {
                exception = "No browser in current V8 context";
                return false;
            }

            var frameId = ctx.GetFrame().Identifier;
            var frame = browser.GetFrame(frameId);
            if (frame == null || !frame.IsValid)
            {
                exception = "Frame is not valid";
                return false;
            }

            // Build the process message
            var msg = CefProcessMessage.Create(_functionName);
            if (msg == null)
            {
                exception = $"Failed to create process message '{_functionName}'";
                return false;
            }

            var msgArgs = msg.Arguments;
            msgArgs.SetSize(arguments.Length);
            for (int i = 0; i < arguments.Length; i++)
            {
                if (arguments[i].IsString)
                    msgArgs.SetString(i, arguments[i].GetStringValue());
                else if (arguments[i].IsDouble || arguments[i].IsInt || arguments[i].IsUInt)
                    msgArgs.SetDouble(i, arguments[i].GetDoubleValue());
                else if (arguments[i].IsBool)
                    msgArgs.SetBool(i, arguments[i].GetBoolValue());
                else
                    msgArgs.SetNull(i);
            }

            frame.SendProcessMessage(CefProcessId.Browser, msg);

            try { System.IO.File.AppendAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "nfmw_v8_trace.txt"), $"[{DateTime.UtcNow:O}] Sent: {_functionName} via frameId={frameId}\n"); } catch { }

            return true;
        }
        catch (Exception ex)
        {
            exception = ex.ToString();
            try { System.IO.File.AppendAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "nfmw_v8_trace.txt"), $"[{DateTime.UtcNow:O}] ERROR: {ex}\n"); } catch { }
            return false;
            return false;
        }
    }
}
