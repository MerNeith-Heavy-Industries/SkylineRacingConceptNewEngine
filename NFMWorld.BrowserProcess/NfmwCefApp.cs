using Xilium.CefGlue;
using Xilium.CefGlue.Common.Shared;

namespace NFMWorld.UI.Cef;

internal abstract class CommonCefApp : CefApp
{
    private readonly CustomScheme[] _customSchemes;

    internal CommonCefApp(CustomScheme[] customSchemes = null) => this._customSchemes = customSchemes;

    protected override void OnRegisterCustomSchemes(CefSchemeRegistrar registrar)
    {
        if (this._customSchemes == null)
            return;
        foreach (CustomScheme customScheme in this._customSchemes)
            registrar.AddCustomScheme(customScheme.SchemeName, customScheme.Options);
    }
}

/// <summary>
/// Minimal CefApp implementation. Handles subprocess detection and provides
/// the render process handler for V8 JavaScript context setup.
/// </summary>
internal sealed class NfmwCefApp : CommonCefApp
{
    private readonly CefRenderProcessHandler _renderProcessHandler;

    internal NfmwCefApp(CefRenderProcessHandler renderProcessHandler, CustomScheme[] customSchemes) : base(customSchemes)
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
