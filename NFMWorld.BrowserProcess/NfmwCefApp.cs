using Xilium.CefGlue;
using Xilium.CefGlue.Common.Shared;

namespace NFMWorld.UI.Cef;

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
}
