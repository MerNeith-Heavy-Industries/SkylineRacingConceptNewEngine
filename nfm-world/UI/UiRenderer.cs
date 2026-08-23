using Lua;
using Microsoft.Xna.Framework;
using NFMWorld;
using NFMWorld.Reactor;
using NFMWorldLibrary.Util;

namespace WorldXaml.UI.Yoga;

public class UiRenderer : IDisposable
{
    private uint _maxEvent = 0;
    
    private readonly Dictionary<string, MessageHandler> _toCsharpHandlers = new();
    private readonly Dictionary<uint, (string Event, Action<LuaValue> Handler)> _toLuaHandlers = new();

    public View? ActiveRoot { get; private set; }
    
    public UiRenderer(WorldGame worldGame)
    {
        var state = LuaHelpers.OpenState();
        LuaUiLibrary.Register(state, SetActiveRoot, Call, OnEvent);
        state.DoFile("data/uis/router.luau");
    }

    private Action OnEvent(string @event, Action<LuaValue> callback)
    {
        var key = _maxEvent++;
        _toLuaHandlers[key] = (@event, callback);
        return () => _toLuaHandlers.Remove(key);
    }

    private void Call(string method, LuaValue payload)
    {
        // Dispatch to the first registered handler.
        // Only one phase is active at a time, so we dispatch to whichever handler is registered.
        foreach (var handler in _toCsharpHandlers.Values)
        {
            handler(method, payload);
            break;
        }
    }

    private void SetActiveRoot(View view)
    {
        FocusManager.ActiveNode = null;
        FocusManager.FocusedNode = null;
        ActiveRoot = view;
    }

    public void Update(GameTime gameTime)
    {
    }

    public void Render()
    {
        ActiveRoot?.LayoutAndRender(G.Viewport);
    }
    
    /// <summary>
    /// Delegate for handling Lua → C# messages.
    /// </summary>
    public delegate void MessageHandler(string method, LuaValue payload);

    /// <summary>
    /// Register a per-phase message handler. Only one handler per phase ID is allowed.
    /// </summary>
    public void Register(string phaseId, MessageHandler handler)
    {
        _toCsharpHandlers[phaseId] = handler;
    }

    /// <summary>
    /// Unregister a per-phase message handler.
    /// </summary>
    public void Unregister(string phaseId)
    {
        _toCsharpHandlers.Remove(phaseId);
    }

    /// <summary>
    /// Push an event from C# to Lua for a specific phase.
    /// </summary>
    public void PushToLua(string phaseId, string eventType, LuaValue payload)
    {
        var fullEventId = $"{phaseId}:{eventType}";
        
        foreach (var (_, (@event, handler)) in _toLuaHandlers)
        {
            if (@event == fullEventId)
            {
                handler(payload);
            }
        }
    }

    public void Navigate(string phaseId)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        
    }
}