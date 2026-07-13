using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Xilium.CefGlue;

namespace NFMWorld.UI.Cef;

/// <summary>
/// Off-screen CEF render handler. Receives OnPaint callbacks with dirty rects
/// and uploads changed pixel regions to a Texture2D for compositing in the
/// FNA draw pipeline.
/// </summary>
internal sealed class NfmwCefRenderHandler(GraphicsDevice graphicsDevice) : CefRenderHandler
{
    private Texture2D? _browserTexture;
    private int _textureWidth;
    private int _textureHeight;
    private bool _needsFullUpload = true;

    // Pre-allocated buffer for copying dirty rect pixel data
    private byte[]? _copyBuffer;

    public Texture2D? BrowserTexture => _browserTexture;
    public int ViewWidth { get; private set; }
    public int ViewHeight { get; private set; }

    public event Action? OnBrowserPainted;

    public void SetViewSize(int width, int height)
    {
        ViewWidth = width;
        ViewHeight = height;
    }

    protected override void GetViewRect(CefBrowser browser, out CefRectangle rect)
    {
        rect = new CefRectangle(0, 0, Math.Max(ViewWidth, 1), Math.Max(ViewHeight, 1));
    }

    protected override bool GetScreenInfo(CefBrowser browser, CefScreenInfo screenInfo)
    {
        screenInfo.DeviceScaleFactor = 1.0f;
        screenInfo.Depth = 32;
        screenInfo.DepthPerComponent = 8;
        screenInfo.IsMonochrome = false;
        screenInfo.Rectangle = new CefRectangle(0, 0, Math.Max(ViewWidth, 1), Math.Max(ViewHeight, 1));
        screenInfo.AvailableRectangle = screenInfo.Rectangle;
        return true;
    }

    protected override void OnPaint(CefBrowser browser, CefPaintElementType type,
        CefRectangle[] dirtyRects, IntPtr buffer, int width, int height)
    {
        if (width <= 0 || height <= 0 || buffer == IntPtr.Zero)
            return;

        EnsureTexture(width, height);

        if (_browserTexture == null)
            return;

        var bytesPerPixel = 4; // BGRA
        var stride = width * bytesPerPixel;

        if (_needsFullUpload || dirtyRects.Length == 0)
        {
            // Full upload
            var totalBytes = width * height * bytesPerPixel;
            EnsureCopyBuffer(totalBytes);
            unsafe
            {
                fixed (byte* dst = _copyBuffer)
                {
                    Buffer.MemoryCopy(buffer.ToPointer(), dst, totalBytes, totalBytes);
                }
            }
            _browserTexture.SetData(_copyBuffer!);
            _needsFullUpload = false;
        }
        else
        {
            // Dirty-rect partial upload — only upload changed regions to GPU
            foreach (var rect in dirtyRects)
            {
                var clampedRect = new Rectangle(
                    Math.Max(0, rect.X),
                    Math.Max(0, rect.Y),
                    Math.Min(rect.Width, width - rect.X),
                    Math.Min(rect.Height, height - rect.Y));

                if (clampedRect.Width <= 0 || clampedRect.Height <= 0)
                    continue;

                var rectBytes = clampedRect.Width * clampedRect.Height * bytesPerPixel;
                EnsureCopyBuffer(rectBytes);

                // Copy only this dirty rect's pixels from the full buffer
                unsafe
                {
                    var srcPtr = (byte*)buffer.ToPointer();
                    fixed (byte* dstPtr = _copyBuffer)
                    {
                        for (int y = 0; y < clampedRect.Height; y++)
                        {
                            var srcOffset = ((clampedRect.Y + y) * stride) + (clampedRect.X * bytesPerPixel);
                            var dstOffset = y * clampedRect.Width * bytesPerPixel;
                            Buffer.MemoryCopy(
                                srcPtr + srcOffset,
                                dstPtr + dstOffset,
                                rectBytes - dstOffset,
                                clampedRect.Width * bytesPerPixel);
                        }
                    }
                }

                _browserTexture.SetData(0, clampedRect, _copyBuffer!, 0, rectBytes);
            }
        }

        OnBrowserPainted?.Invoke();
    }

    protected override void OnPopupShow(CefBrowser browser, bool show)
    {
        // Popups not supported in this off-screen implementation
    }

    protected override void OnPopupSize(CefBrowser browser, CefRectangle rect)
    {
    }

    protected override void OnScrollOffsetChanged(CefBrowser browser, double x, double y)
    {
    }

    protected override void OnImeCompositionRangeChanged(CefBrowser browser,
        CefRange selectedRange, CefRectangle[] characterBounds)
    {
    }

    protected override void OnAcceleratedPaint(CefBrowser browser,
        CefPaintElementType type, CefRectangle[] dirtyRects, nint sharedTextureHandle)
    {
        // Accelerated paint uses shared textures — not used in off-screen mode
    }

    protected override CefAccessibilityHandler GetAccessibilityHandler()
    {
        return null!;
    }

    private void EnsureTexture(int width, int height)
    {
        if (_browserTexture == null || _textureWidth != width || _textureHeight != height)
        {
            _browserTexture?.Dispose();
            _browserTexture = new Texture2D(graphicsDevice, width, height, false, SurfaceFormat.Color);
            _textureWidth = width;
            _textureHeight = height;
            _needsFullUpload = true;
        }
    }

    private void EnsureCopyBuffer(int size)
    {
        if (_copyBuffer == null || _copyBuffer.Length < size)
        {
            _copyBuffer = new byte[size];
        }
    }

    public void DestroyTexture()
    {
        _browserTexture?.Dispose();
        _browserTexture = null;
        _textureWidth = 0;
        _textureHeight = 0;
        _needsFullUpload = true;
    }
}
