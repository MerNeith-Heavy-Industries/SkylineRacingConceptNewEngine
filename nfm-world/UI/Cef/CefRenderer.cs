using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Xilium.CefGlue;

namespace NFMWorld.UI.Cef;

/// <summary>
/// High-level CEF renderer for NFM-World, modeled on ImGuiRenderer.
/// Manages CEF lifecycle, input forwarding, and compositing the off-screen
/// browser texture into the FNA draw pipeline.
///
/// Usage:
///   _cefRenderer.Initialize();        // once, after GraphicsDevice is ready
///   _cefRenderer.Update(gameTime);    // each frame in Update()
///   _cefRenderer.Render();            // each frame in Draw(), between 3D and ImGui
///   _cefRenderer.Shutdown();          // once, in Dispose/UnloadContent
/// </summary>
public sealed class CefRenderer(Game game, string initialUrl, int browserWidth = 1280, int browserHeight = 720)
    : IDisposable
{
    private readonly Game _game = game ?? throw new ArgumentNullException(nameof(game));
    private readonly GraphicsDevice _graphicsDevice = game.GraphicsDevice;

    // CEF components
    private NfmwCefRenderHandler? _renderHandler;
    private NfmwCefClient? _cefClient;
    private CefBrowserHost? _browserHost;
    private CefBrowser? _browser;
    private bool _isInitialized;

    /// <summary>
    /// The JS ↔ C# message bridge. Exposed internally for NfmwCefClient
    /// to route process messages. Phases register handlers via
    /// <see cref="RegisterMessageHandler"/> / <see cref="UnregisterMessageHandler"/>.
    /// </summary>
    internal GameBridge Bridge { get; } = new();

    // Rendering
    private SpriteBatch? _spriteBatch;
    private BasicEffect? _effect;
    private bool _textureNeedsUpdate;

    // Input
    private int _scrollWheelValue;
    private MouseState _lastMouseState;
    private KeyboardState _lastKeyboardState;
    private static readonly Keys[] AllKeys = Enum.GetValues<Keys>();

    // Settings
    private bool _inputEnabled = true;

    public bool IsInitialized => _isInitialized;

    /// <summary>
    /// Initialize CEF and create the off-screen browser. Must be called after
    /// the game's GraphicsDevice is ready (Initialize/LoadContent).
    /// </summary>
    public void Initialize()
    {
        if (_isInitialized) return;

        // 1. Load CEF native runtime
        CefRuntime.Load();

        // 2. Create settings
        var settings = new CefSettings
        {
            WindowlessRenderingEnabled = true,
            MultiThreadedMessageLoop = false,
            ExternalMessagePump = false,
            NoSandbox = true,
            BackgroundColor = new CefColor(0, 0, 0, 0), // Transparent
            RootCachePath = Path.Combine(Path.GetTempPath(), "NFMW_CefCache"),
            LogSeverity = CefLogSeverity.Warning,
        };

        // 3. Create handlers
        _renderHandler = new NfmwCefRenderHandler(_graphicsDevice);
        _renderHandler.SetViewSize(browserWidth, browserHeight);
        _renderHandler.OnBrowserPainted += () => _textureNeedsUpdate = true;

        var renderProcessHandler = new NfmwRenderProcessHandler();
        var app = new NfmwCefApp(renderProcessHandler);
        _cefClient = new NfmwCefClient(_renderHandler, this);

        // 4. Initialize CEF
        var mainArgs = new CefMainArgs([]);
        CefRuntime.Initialize(mainArgs, settings, app, IntPtr.Zero);

        // 5. Create browser
        var windowInfo = CefWindowInfo.Create();
        windowInfo.SetAsWindowless(IntPtr.Zero, true); // transparent = true

        var browserSettings = new CefBrowserSettings
        {
            WindowlessFrameRate = 60,
            BackgroundColor = new CefColor(0, 0, 0, 0),
        };

        _browser = CefBrowserHost.CreateBrowserSync(windowInfo, _cefClient, browserSettings, initialUrl);
        _browserHost = _browser?.GetHost();

        TextInputEXT.TextInput += ForwardTextInput;

        _isInitialized = true;
    }

    /// <summary>
    /// Pump CEF message loop and forward input. Call each frame in Update().
    /// </summary>
    public void Update(GameTime gameTime)
    {
        if (!_isInitialized) return;

        CefRuntime.DoMessageLoopWork();

        ForwardInput();
    }

    /// <summary>
    /// Draw the browser texture as a full-screen overlay. Call in Draw().
    /// </summary>
    public void Render()
    {
        if (!_isInitialized || _renderHandler?.BrowserTexture == null)
            return;

        _spriteBatch ??= new SpriteBatch(_graphicsDevice);

        var texture = _renderHandler.BrowserTexture;
        var viewport = _graphicsDevice.Viewport;

        var oldBlend = _graphicsDevice.BlendState;
        var oldDepth = _graphicsDevice.DepthStencilState;
        var oldRaster = _graphicsDevice.RasterizerState;

        _graphicsDevice.BlendState = BlendState.AlphaBlend;
        _graphicsDevice.DepthStencilState = DepthStencilState.None;
        _graphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;

        _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
            SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise);
        _spriteBatch.Draw(texture, new Rectangle(0, 0, viewport.Width, viewport.Height), Color.White);
        _spriteBatch.End();

        _graphicsDevice.BlendState = oldBlend;
        _graphicsDevice.DepthStencilState = oldDepth;
        _graphicsDevice.RasterizerState = oldRaster;

        _textureNeedsUpdate = false;
    }

    /// <summary>
    /// Navigate the browser to a new URL.
    /// </summary>
    public void Navigate(string url)
    {
        _browser?.GetMainFrame().LoadUrl(url);
    }

    /// <summary>
    /// Execute JavaScript in the browser. Use for C# → JS push updates.
    /// </summary>
    public void ExecuteJavaScript(string code)
    {
        _browser?.GetMainFrame().ExecuteJavaScript(code, null, 0);
    }

    /// <summary>
    /// Enable or disable input forwarding to the browser.
    /// </summary>
    public void SetInputEnabled(bool enabled)
    {
        _inputEnabled = enabled;
    }

    /// <summary>
    /// Resize the browser viewport.
    /// </summary>
    public void Resize(int width, int height)
    {
        _renderHandler?.SetViewSize(width, height);
        _browserHost?.WasResized();
    }

    public CefBrowser? GetBrowser() => _browser;

    /// <summary>
    /// Open the Chromium DevTools window for debugging the webview.
    /// </summary>
    public void ShowDevTools()
    {
        if (_browserHost != null)
        {
            var wi = CefWindowInfo.Create();
            wi.SetAsPopup(IntPtr.Zero, "DevTools");
            _browserHost.ShowDevTools(wi, _cefClient!, new CefBrowserSettings(), new CefPoint(0, 0));
        }
    }

    /// <summary>
    /// Close the Chromium DevTools window.
    /// </summary>
    public void CloseDevTools()
    {
        _browserHost?.CloseDevTools();
    }

    /// <summary>
    /// Resolve the base page URL for the single-page app. All phases share
    /// one index.html; navigation uses hash fragments (#/main-menu, etc.).
    /// </summary>
    public static string ResolveBasePageUrl()
    {
        // Check for dev mode: NFMW_VITE_DEV env var or .vite-dev marker file
        var isDev = System.Environment.GetEnvironmentVariable("NFMW_VITE_DEV") == "1"
                    || File.Exists(Path.Combine(AppContext.BaseDirectory, "data", "html", ".vite-dev"));

        if (isDev)
        {
            return "http://localhost:5173/";
        }

        // Production: load from built dist/
        var indexPath = Path.Combine(AppContext.BaseDirectory, "data", "html", "dist", "index.html");

        if (File.Exists(indexPath))
        {
            return new Uri(indexPath).AbsoluteUri;
        }

        return "about:blank";
    }

    /// <summary>
    /// Register a per-phase message handler. Called by <see cref="PhaseBridge.Register"/>.
    /// </summary>
    public void RegisterMessageHandler(string phaseId, GameBridge.MessageHandler handler)
    {
        Bridge.Register(phaseId, handler);
    }

    /// <summary>
    /// Unregister a per-phase message handler. Called by <see cref="PhaseBridge.Unregister"/>.
    /// </summary>
    public void UnregisterMessageHandler(string phaseId)
    {
        Bridge.Unregister(phaseId);
    }

    /// <summary>
    /// Push an event from C# to JS for a specific phase.
    /// The JS side receives this via window.__nfmwDispatch("{phaseId}:{eventType}", data).
    /// </summary>
    public void PushToJs(string phaseId, string eventType, object? data)
    {
        GameBridge.PushToJs(_browser, phaseId, eventType, data);
    }

    /// <summary>
    /// Push an event from C# to JS for a specific phase.
    /// The JS side receives this via window.__nfmwDispatch("{phaseId}:{eventType}", data).
    /// </summary>
    public void PushToJs(string phaseId, string eventType, byte[] binary)
    {
        GameBridge.PushToJs(_browser, phaseId, eventType, binary);
    }

    #region Input Forwarding

    private void ForwardTextInput(char c)
    {
        var host = _browserHost!;

        var keyboard = Keyboard.GetState();
        var isShiftDown = keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift);
        var isCtrlDown = keyboard.IsKeyDown(Keys.LeftControl) || keyboard.IsKeyDown(Keys.RightControl);
        var isAltDown = keyboard.IsKeyDown(Keys.LeftAlt) || keyboard.IsKeyDown(Keys.RightAlt);

        var mods = CefEventFlags.None;
        if (isShiftDown) mods |= CefEventFlags.ShiftDown;
        if (isCtrlDown) mods |= CefEventFlags.ControlDown;
        if (isAltDown) mods |= CefEventFlags.AltDown;

        var charEvent = new CefKeyEvent
        {
            WindowsKeyCode = c,
            NativeKeyCode = c,
            Modifiers = mods,
            IsSystemKey = false,
            EventType = CefKeyEventType.Char,
        };
        host.SendKeyEvent(charEvent);
    }

    private void ForwardInput()
    {
        if (!_inputEnabled || _browserHost == null || !_game.IsActive)
            return;

        var mouse = Mouse.GetState();
        ForwardMouseInput(mouse);
        ForwardKeyboardInput();
        _lastMouseState = mouse;
    }

    private void ForwardMouseInput(MouseState mouse)
    {
        var host = _browserHost!;

        // Mouse move
        if (mouse.X != _lastMouseState.X || mouse.Y != _lastMouseState.Y)
        {
            var mouseEvent = new CefMouseEvent(mouse.X, mouse.Y, CefEventFlags.None);
            host.SendMouseMoveEvent(mouseEvent, false);
        }

        // Mouse buttons
        var mouseEvt = new CefMouseEvent(mouse.X, mouse.Y, CefEventFlags.None);
        ProcessMouseButton(host, mouseEvt, mouse.LeftButton, _lastMouseState.LeftButton,
            CefMouseButtonType.Left);
        ProcessMouseButton(host, mouseEvt, mouse.RightButton, _lastMouseState.RightButton,
            CefMouseButtonType.Right);
        ProcessMouseButton(host, mouseEvt, mouse.MiddleButton, _lastMouseState.MiddleButton,
            CefMouseButtonType.Middle);

        // Scroll wheel
        var scrollDelta = mouse.ScrollWheelValue - _scrollWheelValue;
        if (scrollDelta != 0)
        {
            var wheelEvent = new CefMouseEvent(mouse.X, mouse.Y, CefEventFlags.None);
            host.SendMouseWheelEvent(wheelEvent, 0, scrollDelta);
            _scrollWheelValue = mouse.ScrollWheelValue;
        }
    }

    private void ProcessMouseButton(CefBrowserHost host, CefMouseEvent mouseEvent,
        ButtonState current, ButtonState previous, CefMouseButtonType button)
    {
        if (current != previous)
        {
            host.SendMouseClickEvent(mouseEvent, button, current == ButtonState.Released, 1);
        }
    }

    private void ForwardKeyboardInput()
    {
        var keyboard = Keyboard.GetState();
        var host = _browserHost!;
        
        var isShiftDown = keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift);
        var isCtrlDown = keyboard.IsKeyDown(Keys.LeftControl) || keyboard.IsKeyDown(Keys.RightControl);
        var isAltDown = keyboard.IsKeyDown(Keys.LeftAlt) || keyboard.IsKeyDown(Keys.RightAlt);

        foreach (var key in AllKeys)
        {
            var isDown = keyboard.IsKeyDown(key);
            var wasDown = _lastKeyboardState.IsKeyDown(key);

            if (isDown != wasDown)
            {
                var (windowsKeyCode, modifiers) = MapKeyToCef(key, isShiftDown, isCtrlDown, isAltDown);
                if (windowsKeyCode != 0)
                {
                    var keyEvent = new CefKeyEvent
                    {
                        WindowsKeyCode = windowsKeyCode,
                        NativeKeyCode = (int)key,
                        Modifiers = modifiers,
                        IsSystemKey = false,
                        EventType = isDown ? CefKeyEventType.RawKeyDown : CefKeyEventType.KeyUp,
                    };
                    host.SendKeyEvent(keyEvent);
                }
            }
        }

        _lastKeyboardState = keyboard;
    }

    /// <summary>
    /// Map FNA Keys to CEF Windows virtual key codes and modifier flags.
    /// </summary>
    private static (int KeyCode, CefEventFlags Modifiers) MapKeyToCef(Keys key, bool isShiftDown, bool isCtrlDown, bool isAltDown)
    {
        var mods = CefEventFlags.None;
        if (isShiftDown) mods |= CefEventFlags.ShiftDown;
        if (isCtrlDown) mods |= CefEventFlags.ControlDown;
        if (isAltDown) mods |= CefEventFlags.AltDown;
        
        return key switch
        {
            Keys.Back => (0x08, mods),              // VK_BACK
            Keys.Tab => (0x09, mods),               // VK_TAB
            Keys.Enter => (0x0D, mods),             // VK_RETURN
            Keys.Escape => (0x1B, mods),            // VK_ESCAPE
            Keys.Space => (0x20, mods),             // VK_SPACE
            Keys.PageUp => (0x21, mods),            // VK_PRIOR
            Keys.PageDown => (0x22, mods),          // VK_NEXT
            Keys.End => (0x23, mods),               // VK_END
            Keys.Home => (0x24, mods),              // VK_HOME
            Keys.Left => (0x25, mods),              // VK_LEFT
            Keys.Up => (0x26, mods),                // VK_UP
            Keys.Right => (0x27, mods),             // VK_RIGHT
            Keys.Down => (0x28, mods),              // VK_DOWN
            Keys.Delete => (0x2E, mods),            // VK_DELETE
            Keys.Insert => (0x2D, mods),            // VK_INSERT
            >= Keys.D0 and <= Keys.D9 => (0x30 + (key - Keys.D0), mods),
            >= Keys.A and <= Keys.Z => (0x41 + (key - Keys.A), mods),
            >= Keys.NumPad0 and <= Keys.NumPad9 => (0x60 + (key - Keys.NumPad0), mods),
            Keys.Multiply => (0x6A, mods),          // VK_MULTIPLY
            Keys.Add => (0x6B, mods),               // VK_ADD
            Keys.Subtract => (0x6D, mods),          // VK_SUBTRACT
            Keys.Decimal => (0x6E, mods),           // VK_DECIMAL
            Keys.Divide => (0x6F, mods),            // VK_DIVIDE
            >= Keys.F1 and <= Keys.F12 => (0x70 + (key - Keys.F1), mods),
            Keys.NumLock => (0x90, mods),           // VK_NUMLOCK
            Keys.Scroll => (0x91, mods),            // VK_SCROLL
            Keys.LeftShift or Keys.RightShift => (0x10, mods),  // VK_SHIFT
            Keys.LeftControl or Keys.RightControl => (0x11, mods), // VK_CONTROL
            Keys.LeftAlt or Keys.RightAlt => (0x12, mods),       // VK_MENU
            Keys.OemSemicolon => (0xBA, mods),      // VK_OEM_1
            Keys.OemPlus => (0xBB, mods),           // VK_OEM_PLUS
            Keys.OemComma => (0xBC, mods),          // VK_OEM_COMMA
            Keys.OemMinus => (0xBD, mods),          // VK_OEM_MINUS
            Keys.OemPeriod => (0xBE, mods),         // VK_OEM_PERIOD
            Keys.OemQuestion => (0xBF, mods),       // VK_OEM_2
            Keys.OemTilde => (0xC0, mods),          // VK_OEM_3
            Keys.OemOpenBrackets => (0xDB, mods),   // VK_OEM_4
            Keys.OemCloseBrackets => (0xDD, mods),  // VK_OEM_6
            Keys.OemPipe => (0xDC, mods),           // VK_OEM_5
            Keys.OemQuotes => (0xDE, mods),         // VK_OEM_7
            _ => (0, mods),
        };
    }

    #endregion

    #region Shutdown

    public void Shutdown()
    {
        if (!_isInitialized) return;

        _renderHandler?.DestroyTexture();

        _browserHost?.CloseBrowser();
        _browserHost?.Dispose();
        _browserHost = null;
        _browser = null;

        _spriteBatch?.Dispose();
        _spriteBatch = null;

        // CefRuntime.Shutdown must be called on the same thread as Initialize
        CefRuntime.Shutdown();
        _isInitialized = false;

        TextInputEXT.TextInput -= ForwardTextInput;
    }

    public void Dispose()
    {
        Shutdown();
    }

    #endregion
}
