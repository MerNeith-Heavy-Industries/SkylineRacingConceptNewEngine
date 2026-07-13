using System.Text.Json;

namespace NFMWorld.UI.Cef;

/// <summary>
/// Abstract base for per-phase C#↔JS bridges. Each phase that uses CEF for UI
/// creates a subclass, registers it with <see cref="CefRenderer"/> on Enter,
/// and unregisters on Exit.
///
/// Usage:
///   public sealed class MainMenuBridge : PhaseBridge
///   {
///       public MainMenuBridge() : base("main-menu") { }
///
///       protected override void OnMessage(string type, JsonElement? args)
///       {
///           switch (type)
///           {
///               case "navigate": OnNavigate(args); break;
///           }
///       }
///   }
///
///   // In MainMenuPhase:
///   _bridge = new MainMenuBridge();
///   _bridge.Register(_cefRenderer);
///   _bridge.Push("account", new { Name = "Player", ... });
/// </summary>
public abstract class PhaseBridge : IDisposable
{
    /// <summary>
    /// Unique identifier for this phase's bridge. Used as the dispatch key
    /// in CefRenderer's message registry.
    /// </summary>
    public string PhaseId { get; }

    /// <summary>
    /// The CefRenderer this bridge is registered with. Set by <see cref="Register"/>.
    /// </summary>
    protected CefRenderer? Renderer { get; private set; }

    /// <summary>
    /// The URL to load when this phase becomes active. Subclasses override this
    /// to return the phase-specific HTML page.
    /// </summary>
    public virtual string? PageUrl => null;

    /// <summary>
    /// Whether CEF input should be forwarded while this phase is active.
    /// Menu phases typically return true; race phases return false.
    /// </summary>
    public virtual bool EnableInput => true;

    protected PhaseBridge(string phaseId)
    {
        PhaseId = phaseId ?? throw new ArgumentNullException(nameof(phaseId));
    }

    /// <summary>
    /// Register this bridge with the given CefRenderer. Called from Phase.Enter().
    /// Navigates to <see cref="PageUrl"/> if non-null. Uses ExecuteJavaScript
    /// for hash-only changes to avoid full page reloads.
    /// </summary>
    public void Register(CefRenderer renderer)
    {
        Renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        Renderer.RegisterMessageHandler(PhaseId, DispatchMessage);

        if (PageUrl is { } url)
        {
            // For hash-based navigation in the single-page app, use JS
            // to change location.hash instead of full LoadUrl. This keeps
            // the V8 context alive and avoids render-process teardown.
            var hashIndex = url.IndexOf("#/", StringComparison.Ordinal);
            if (hashIndex > 0)
            {
                var hash = url[hashIndex..];
                Renderer.ExecuteJavaScript($"window.location.hash = '{hash}';");
            }
            else
            {
                Renderer.Navigate(url);
            }
        }

        OnRegistered();
    }

    /// <summary>
    /// Unregister this bridge from the CefRenderer. Called from Phase.Exit().
    /// </summary>
    public void Unregister()
    {
        if (Renderer != null)
        {
            Renderer.UnregisterMessageHandler(PhaseId);
        }

        OnUnregistered();
        Renderer = null;
    }

    /// <summary>
    /// Push an event from C# to JS. The JS side receives this via
    /// window.__nfmwDispatch("{PhaseId}:{eventType}", data).
    /// </summary>
    protected void Push(string eventType, object? data = null)
    {
        Renderer?.PushToJs(PhaseId, eventType, data);
    }

    /// <summary>
    /// Called when the JS page sends a message via nfmw.call(methodName, ...).
    /// Subclasses override this to handle phase-specific messages.
    /// </summary>
    /// <param name="type">The method name called from JS.</param>
    /// <param name="args">
    /// The first argument from JS, parsed as a JsonElement if the call included
    /// a JSON-stringifiable object argument; otherwise null.
    /// </param>
    protected abstract void OnMessage(string type, JsonElement? args);

    /// <summary>
    /// Called after the bridge is successfully registered and the page URL
    /// has been navigated to (if any).
    /// </summary>
    protected virtual void OnRegistered() { }

    /// <summary>
    /// Called after the bridge is unregistered, before Renderer is set to null.
    /// </summary>
    protected virtual void OnUnregistered() { }

    /// <summary>
    /// Dispatch an incoming JS message. Packages the raw args into a JsonElement
    /// for subclasses to consume.
    /// </summary>
    private void DispatchMessage(string messageType, string? rawJson)
    {
        JsonElement? parsed = null;
        if (rawJson != null)
        {
            try
            {
                using var doc = JsonDocument.Parse(rawJson);
                parsed = doc.RootElement.Clone();
            }
            catch (JsonException)
            {
                // If parsing fails, pass null — the subclass can handle raw args
                // via the raw string if needed (but most will use the typed path).
            }
        }

        OnMessage(messageType, parsed);
    }

    public virtual void Dispose()
    {
        Unregister();
    }
}
