using Xilium.CefGlue;

namespace NFMWorld.UI.Cef;

/// <summary>
/// Minimal CefApp implementation. Handles subprocess detection and provides
/// the render process handler for V8 JavaScript context setup.
/// </summary>
internal sealed class NfmwCefApp : CefApp
{
    private readonly CefRenderProcessHandler _renderProcessHandler;

    public NfmwCefApp(CefRenderProcessHandler renderProcessHandler)
    {
        _renderProcessHandler = renderProcessHandler;
    }

    protected override CefRenderProcessHandler GetRenderProcessHandler()
    {
        return _renderProcessHandler;
    }

    protected override void OnBeforeCommandLineProcessing(string processType, CefCommandLine commandLine)
    {
        // Reduce CEF's background process overhead
        commandLine.AppendSwitch("--disable-background-networking");
        commandLine.AppendSwitch("--disable-sync");
        commandLine.AppendSwitch("--disable-extensions");
        commandLine.AppendSwitch("--disable-plugins");

        base.OnBeforeCommandLineProcessing(processType, commandLine);
    }
}
