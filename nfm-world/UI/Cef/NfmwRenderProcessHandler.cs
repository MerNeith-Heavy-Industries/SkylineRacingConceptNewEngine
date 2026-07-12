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
            var attrs = CefV8PropertyAttribute.ReadOnly | CefV8PropertyAttribute.DontDelete;

            // Register nfmw.call directly on window (flat naming, matches CefMessageRouter pattern).
            // The JS bridge will call window.__nfmwCall(methodName, jsonPayload).
            using var func = CefV8Value.CreateFunction("__nfmwCall", new NfmwV8Handler("nfmwCall"));
            global.SetValue("__nfmwCall", func, attrs);
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
internal sealed class NfmwV8Handler(string functionName) : CefV8Handler
{
    protected override bool Execute(string name, CefV8Value obj, CefV8Value[] arguments,
        out CefV8Value returnValue, out string exception)
    {
        returnValue = CefV8Value.CreateNull();
        exception = string.Empty;

        try
        {
            // Get browser and frame from the current V8 context.
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
            var msg = CefProcessMessage.Create(functionName);
            if (msg == null)
            {
                exception = $"Failed to create process message '{functionName}'";
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
                else if (arguments[i].IsNull)
                    msgArgs.SetNull(i);
                else
                {
                    exception = "Invalid argument type";
                    return false;
                }
            }

            frame.SendProcessMessage(CefProcessId.Browser, msg);

            return true;
        }
        catch (Exception ex)
        {
            exception = ex.ToString();
            return false;
        }
    }
}
