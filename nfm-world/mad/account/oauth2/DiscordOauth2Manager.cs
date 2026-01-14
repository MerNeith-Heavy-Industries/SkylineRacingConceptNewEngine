using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Diagnostics;

namespace nfm_world.mad.account.oauth2;

/// <summary>
/// Manager to help process Discord Oauth2 account management.
/// Behavior:
///  - Create a temporary local HTTP server to handle login callback
///  - Open the default web browser asking the user to log in and authorize the given client id
///  - Await user login on HTTP server callback
///  - Extract the authorization `code` and (optionally) exchange it for an access token
/// </summary>
public class DiscordOauth2Manager
{
    /// <summary>
    /// Result returned by the OAuth flow.
    /// </summary>
    public class DiscordOauthResult
    {
        public bool Success { get; set; }
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public string? Code { get; set; }
        public string? Error { get; set; }
        public string? RawResponse { get; set; }
    }

    /// <summary>
    /// Start an OAuth2 authorization flow.
    /// - `clientId`: Discord application client id
    /// - `scopes`: scopes requested (e.g. new[] { "identify", "email" })
    /// - `clientSecret`: optional. If provided, an authorization code will be exchanged for an access token.
    /// Returns a <see cref="DiscordOauthResult"/> containing either `AccessToken` (if exchange performed)
    /// or the `Code` (if no client secret provided) or an error message.
    /// </summary>
    public async Task<DiscordOauthResult> AuthorizeAsync(string clientId, string[] scopes, string? clientSecret = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("clientId is required", nameof(clientId));

        // Start a loopback listener on an ephemeral port
        var listener = new TcpListener(IPAddress.Loopback, 8812);
        listener.Start();
        try
        {
            var localEp = (IPEndPoint)listener.LocalEndpoint!;
            var port = localEp.Port;
            var redirectUri = $"http://127.0.0.1:{port}/callback";

            var scopeStr = Uri.EscapeDataString(string.Join(' ', scopes));
            var authUrl = $"https://discord.com/api/oauth2/authorize?response_type=code&client_id={Uri.EscapeDataString(clientId)}&scope={scopeStr}&redirect_uri={Uri.EscapeDataString(redirectUri)}&prompt=consent";

            // Open the browser to the authorization URL
            OpenBrowser(authUrl);

            // Wait for a single incoming HTTP request (the OAuth2 redirect)
            using var acceptCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            var acceptTask = listener.AcceptTcpClientAsync();
            var completed = await Task.WhenAny(acceptTask, Task.Delay(Timeout.Infinite, acceptCts.Token));
            if (acceptTask.IsCompletedSuccessfully)
            {
                using var client = acceptTask.Result;
                var query = await ReadRequestAndRespondAsync(client);

                // parse query string for code or error
                var qs = ParseQuery(query);
                if (qs.TryGetValue("error", out var err))
                {
                    return new DiscordOauthResult { Success = false, Error = err };
                }

                qs.TryGetValue("code", out var code);
                if (string.IsNullOrEmpty(code))
                {
                    return new DiscordOauthResult { Success = false, Error = "No code in callback" };
                }

                // If a client secret is provided, exchange the code for a token
                if (!string.IsNullOrEmpty(clientSecret))
                {
                    using var http = new HttpClient();
                    var tokenEndpoint = "https://discord.com/api/oauth2/token";
                    var form = new Dictionary<string, string>
                    {
                        ["client_id"] = clientId,
                        ["client_secret"] = clientSecret,
                        ["grant_type"] = "authorization_code",
                        ["code"] = code,
                        ["redirect_uri"] = redirectUri
                    };

                    using var resp = await http.PostAsync(tokenEndpoint, new FormUrlEncodedContent(form), ct);
                    var body = await resp.Content.ReadAsStringAsync(ct);
                    if (!resp.IsSuccessStatusCode)
                        return new DiscordOauthResult { Success = false, Error = $"Token exchange failed: {resp.StatusCode}", RawResponse = body };

                    try
                    {
                        using var doc = JsonDocument.Parse(body);
                        var root = doc.RootElement;
                        root.TryGetProperty("access_token", out var at);
                        root.TryGetProperty("refresh_token", out var rt);
                        return new DiscordOauthResult
                        {
                            Success = true,
                            AccessToken = at.GetString(),
                            RefreshToken = rt.GetString(),
                            RawResponse = body
                        };
                    }
                    catch (JsonException)
                    {
                        return new DiscordOauthResult { Success = false, Error = "Invalid JSON from token endpoint", RawResponse = body };
                    }
                }

                // No exchange performed — return the code
                return new DiscordOauthResult { Success = true, Code = code };
            }
            else
            {
                return new DiscordOauthResult { Success = false, Error = "Listener accept timed out or was cancelled" };
            }
        }
        finally
        {
            try { listener.Stop(); } catch { }
        }
    }

    private static void OpenBrowser(string url)
    {
        try
        {
            // Cross-platform approach
            var psi = new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            };
            Process.Start(psi);
        }
        catch
        {
            // Fallbacks for platforms where UseShellExecute=false by default
            if (OperatingSystem.IsWindows())
                Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
            else if (OperatingSystem.IsMacOS())
                Process.Start("open", url);
            else
                Process.Start("xdg-open", url);
        }
    }

    private static async Task<string> ReadRequestAndRespondAsync(TcpClient client)
    {
        using var ns = client.GetStream();
        var buffer = new byte[8192];
        var sb = new StringBuilder();
        int read;
        // Read until header terminator \r\n\r\n
        while ((read = await ns.ReadAsync(buffer.AsMemory(0, buffer.Length))) > 0)
        {
            sb.Append(Encoding.ASCII.GetString(buffer, 0, read));
            if (sb.ToString().Contains("\r\n\r\n"))
                break;
            if (sb.Length > 64 * 1024) // defensive
                break;
        }

        var req = sb.ToString();
        // first line: GET /callback?code=... HTTP/1.1
        var firstLine = req.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
        var parts = firstLine.Split(' ');
        var pathAndQuery = parts.Length >= 2 ? parts[1] : "/";

        // Respond with a simple page telling the user they can close the browser
        var responseBody = "<html><body><h2>Authentication complete — you can close this window.</h2></body></html>";
        var response = "HTTP/1.1 200 OK\r\n" +
                       "Content-Type: text/html; charset=utf-8\r\n" +
                       $"Content-Length: {Encoding.UTF8.GetByteCount(responseBody)}\r\n" +
                       "Connection: close\r\n\r\n" +
                       responseBody;

        var respBytes = Encoding.UTF8.GetBytes(response);
        await ns.WriteAsync(respBytes.AsMemory(0, respBytes.Length));
        await ns.FlushAsync();

        // extract query part
        var qIdx = pathAndQuery.IndexOf('?');
        var query = qIdx >= 0 ? pathAndQuery[(qIdx + 1)..] : string.Empty;
        return query;
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        var dict = new Dictionary<string, string>();
        if (string.IsNullOrEmpty(query))
            return dict;
        var parts = query.Split('&', StringSplitOptions.RemoveEmptyEntries);
        foreach (var p in parts)
        {
            var idx = p.IndexOf('=');
            if (idx >= 0)
            {
                var k = Uri.UnescapeDataString(p[..idx]);
                var v = Uri.UnescapeDataString(p[(idx + 1)..]);
                dict[k] = v;
            }
            else
            {
                dict[Uri.UnescapeDataString(p)] = string.Empty;
            }
        }
        return dict;
    }
}