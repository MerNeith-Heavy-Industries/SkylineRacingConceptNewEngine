using Xilium.CefGlue;

namespace NFMWorld.UI.Cef;

/// <summary>
/// Factory that creates <see cref="NfmwResourceHandler"/> instances for requests
/// under the <c>nfmw://</c> custom scheme. All files are served from the
/// <c>data/html/dist/</c> directory relative to <see cref="AppContext.BaseDirectory"/>.
/// </summary>
internal sealed class NfmwSchemeHandlerFactory : CefSchemeHandlerFactory
{
    private readonly string _distRoot;

    /// <summary>
    /// Create a factory serving files from the default dist/ location.
    /// </summary>
    public NfmwSchemeHandlerFactory()
        : this(Path.Combine(AppContext.BaseDirectory, "data", "html", "dist"))
    {
    }

    /// <summary>
    /// Create a factory serving files from <paramref name="distRoot"/>.
    /// The directory must exist on disk.
    /// </summary>
    internal NfmwSchemeHandlerFactory(string distRoot)
    {
        _distRoot = Path.GetFullPath(distRoot);
        if (!Directory.Exists(_distRoot))
            throw new DirectoryNotFoundException($"Web UI dist directory not found: {_distRoot}");
    }

    protected override CefResourceHandler Create(CefBrowser browser, CefFrame frame, string schemeName,
        CefRequest request)
    {
        return new NfmwResourceHandler(_distRoot, request);
    }
}

/// <summary>
/// Serves files from a local <c>data/html/dist/</c> directory via the <c>nfmw://app/</c>
/// custom CEF scheme. Implements SPA fallback: any URL that does not map to an existing
/// file with a recognised extension is served as <c>index.html</c>.
/// </summary>
internal sealed class NfmwResourceHandler : CefResourceHandler
{
    private readonly string _distRoot;
    private Stream? _stream;
    private string? _mimeType;
    private int _statusCode;
    private bool _completed;

    public NfmwResourceHandler(string distRoot, CefRequest request)
    {
        _distRoot = distRoot;

        var (status, filePath, mime) = ResolveFile(request);

        _statusCode = status;
        if (filePath is not null)
        {
            _stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            _mimeType = mime;
        }
    }

    // ── CefResourceHandler overrides (CefGlue 120+ API) ───────────────

    protected override bool Open(CefRequest request, out bool handleRequest, CefCallback callback)
    {
        handleRequest = true;
        callback.Continue();
        return true;
    }

    protected override void GetResponseHeaders(CefResponse response, out long responseLength, out string? redirectUrl)
    {
        var stream = _stream;

        response.Status = _statusCode;
        response.MimeType = _mimeType ?? "text/html";

        // Required for <script type="module"> and <link> under custom schemes —
        // CEF applies CORS checks even for same-origin requests on non-http schemes.
        response.SetHeaderByName("Access-Control-Allow-Origin", "*", overwrite: true);

        if (stream is not null)
        {
            responseLength = stream.Length;
            redirectUrl = null;
        }
        else
        {
            responseLength = -1;
            redirectUrl = null;
        }
    }

    protected override bool Read(Stream response, int bytesToRead, out int bytesRead, CefResourceReadCallback callback)
    {
        bytesRead = 0;

        var stream = _stream;
        if (stream is null || _completed)
            return false;

        try
        {
            var buffer = new byte[bytesToRead];
            bytesRead = stream.Read(buffer, 0, bytesToRead);

            if (bytesRead > 0)
            {
                response.Write(buffer, 0, bytesRead);
            }

            if (bytesRead == 0 || stream.Position >= stream.Length)
            {
                _completed = true;
            }

            return bytesRead > 0;
        }
        catch (ObjectDisposedException)
        {
            bytesRead = 0;
            _completed = true;
            return false;
        }
    }

    protected override bool Skip(long bytesToSkip, out long bytesSkipped, CefResourceSkipCallback callback)
    {
        bytesSkipped = 0;

        var stream = _stream;
        if (stream is null || _completed)
            return false;

        try
        {
            var start = stream.Position;
            stream.Seek(bytesToSkip, SeekOrigin.Current);
            bytesSkipped = stream.Position - start;

            if (stream.Position >= stream.Length)
                _completed = true;

            return bytesSkipped > 0;
        }
        catch (ObjectDisposedException)
        {
            bytesSkipped = 0;
            _completed = true;
            return false;
        }
    }

    protected override void Cancel()
    {
        DisposeStream();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            DisposeStream();
        base.Dispose(disposing);
    }

    private void DisposeStream()
    {
        Interlocked.Exchange(ref _stream, null)?.Dispose();
    }

    // ── Resolution logic ──────────────────────────────────────────────

    // CefRuntime.GetMimeType is unreliable for .js/.mjs/.css etc, so we use
    // our own mapping with CEF's lookup as a fallback.
    private static readonly Dictionary<string, string> MimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".js"]   = "application/javascript",
        [".mjs"]  = "application/javascript",
        [".css"]  = "text/css",
        [".html"] = "text/html",
        [".htm"]  = "text/html",
        [".json"] = "application/json",
        [".svg"]  = "image/svg+xml",
        [".png"]  = "image/png",
        [".jpg"]  = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".gif"]  = "image/gif",
        [".webp"] = "image/webp",
        [".ico"]  = "image/x-icon",
        [".woff"]  = "font/woff",
        [".woff2"] = "font/woff2",
        [".ttf"]   = "font/ttf",
        [".eot"]   = "application/vnd.ms-fontobject",
        [".wasm"]  = "application/wasm",
    };

    private static string GetMimeType(string extension)
    {
        if (MimeTypes.TryGetValue(extension, out var mime))
            return mime;

        return CefRuntime.GetMimeType(extension) ?? "application/octet-stream";
    }

    /// <summary>
    /// Resolve a CEF request URL to a physical file path under <c>data/html/dist/</c>.
    /// Returns (statusCode, filePath, mimeType). filePath is null for 404 responses.
    /// </summary>
    private (int status, string? filePath, string? mime) ResolveFile(CefRequest request)
    {
        var uri = new Uri(request.Url);
        var path = uri.AbsolutePath.TrimStart('/');

        // Reject directory traversal attempts
        if (path.Contains("..") || path.Contains('\\'))
            return (404, null, null);

        // Root or empty path → index.html (SPA fallback)
        if (string.IsNullOrEmpty(path))
        {
            var indexPath = Path.Combine(_distRoot, "index.html");
            if (File.Exists(indexPath))
                return (200, indexPath, "text/html");
            return (404, null, null);
        }

        // Direct file lookup
        var candidate = Path.GetFullPath(Path.Combine(_distRoot, path));

        // Security: ensure resolved path is still under distRoot
        if (!candidate.StartsWith(_distRoot + Path.DirectorySeparatorChar)
            && candidate != _distRoot)
            return (404, null, null);

        if (File.Exists(candidate))
        {
            var mime = GetMimeType(Path.GetExtension(candidate));
            return (200, candidate, mime);
        }

        // File not found → SPA fallback (the Preact hash router handles routes)
        var spaFallback = Path.Combine(_distRoot, "index.html");
        if (File.Exists(spaFallback))
            return (200, spaFallback, "text/html");

        return (404, null, null);
    }
}
