using System.Net;
using NFMWorldLibrary.Multiplayer.Packets.S2C;

namespace NFMWorldLibrary.Multiplayer;

/// <summary>
/// Result of SRV resolution for a Game Master instance.
/// </summary>
public record ResolvedGameMaster
{
    /// <summary>HTTP endpoint for Lobby→GM API calls (e.g., /create-race).</summary>
    public required Uri HttpEndpoint { get; init; }

    /// <summary>ENet UDP address clients use to connect for races.</summary>
    public required IpAndPort GameAddress { get; init; }

    /// <summary>Domain name this GM was resolved from.</summary>
    public required string Domain { get; init; }

    /// <summary>Whether this GM is currently considered healthy.</summary>
    public bool IsHealthy { get; set; } = true;

    /// <summary>Number of consecutive failed requests.</summary>
    public int ConsecutiveFailures { get; set; }
}

/// <summary>
/// Discovers Game Masters via DNS SRV records and selects one per race.
/// 
/// Configured via GAME_MASTER_DOMAINS env var (comma-separated domain names).
/// Each domain is queried with SRV record _nfmw-game._udp.{domain}.
/// The SRV response provides the UDP game port; HTTP is on standard port 80/443
/// on the same target host.
/// 
/// SRV records are re-resolved periodically (default 60s) for DNS-level changes.
/// </summary>
public class GameMasterRegistry : IDisposable
{
    private readonly List<ResolvedGameMaster> _masters = new();
    private readonly int _maxConsecutiveFailures;
    private readonly Timer _refreshTimer;
    private readonly object _lock = new();
    private int _roundRobinIndex;

    public GameMasterRegistry(
        IEnumerable<string> domains,
        int maxConsecutiveFailures = 3,
        int refreshIntervalSeconds = 60)
    {
        _maxConsecutiveFailures = maxConsecutiveFailures;

        foreach (var domain in domains)
            ResolveAndAdd(domain.Trim());

        if (_masters.Count == 0)
            throw new InvalidOperationException(
                "No Game Masters could be resolved. Check GAME_MASTER_DOMAINS configuration.");

        _refreshTimer = new Timer(_ => RefreshAll(), null,
            TimeSpan.FromSeconds(refreshIntervalSeconds),
            TimeSpan.FromSeconds(refreshIntervalSeconds));
    }

    /// <summary>Creates a registry from the GAME_MASTER_DOMAINS env var.</summary>
    public static GameMasterRegistry FromEnvironment(
        int maxConsecutiveFailures = 3,
        int refreshIntervalSeconds = 60)
    {
        var domainsStr = Environment.GetEnvironmentVariable("GAME_MASTER_DOMAINS")
                         ?? "localhost";

        var domains = domainsStr.Split(',', StringSplitOptions.RemoveEmptyEntries);
        return new GameMasterRegistry(domains, maxConsecutiveFailures, refreshIntervalSeconds);
    }

    /// <summary>Selects a healthy Game Master using round-robin.</summary>
    public ResolvedGameMaster SelectGameMaster()
    {
        lock (_lock)
        {
            var healthy = _masters.Where(m => m.IsHealthy).ToList();
            if (healthy.Count == 0)
                throw new InvalidOperationException("No healthy Game Masters available.");

            _roundRobinIndex = (_roundRobinIndex + 1) % healthy.Count;
            return healthy[_roundRobinIndex];
        }
    }

    /// <summary>Marks a GM as failed (increments failure counter, marks unhealthy if threshold reached).</summary>
    public void MarkFailure(ResolvedGameMaster master)
    {
        lock (_lock)
        {
            master.ConsecutiveFailures++;
            if (master.ConsecutiveFailures >= _maxConsecutiveFailures)
                master.IsHealthy = false;
        }
    }

    /// <summary>Marks a GM as healthy (resets failure counter).</summary>
    public void MarkSuccess(ResolvedGameMaster master)
    {
        lock (_lock)
        {
            master.ConsecutiveFailures = 0;
            master.IsHealthy = true;
        }
    }

    private void ResolveAndAdd(string domain)
    {
        var gm = ResolveDomain(domain);
        if (gm is not null)
        {
            lock (_lock) { _masters.Add(gm); }
        }
    }

    private void RefreshAll()
    {
        lock (_lock)
        {
            for (var i = _masters.Count - 1; i >= 0; i--)
            {
                var updated = ResolveDomain(_masters[i].Domain);
                if (updated is not null)
                {
                    // Preserve health state
                    updated.IsHealthy = _masters[i].IsHealthy;
                    updated.ConsecutiveFailures = _masters[i].ConsecutiveFailures;
                    _masters[i] = updated;
                }
            }
        }
    }

    private static ResolvedGameMaster? ResolveDomain(string domain)
    {
        try
        {
            // Query SRV record: _nfmw-game._udp.{domain}
            var srvQuery = $"_nfmw-game._udp.{domain}";

            // System.Net.Dns does not support SRV queries directly on all platforms.
            // Use a simple A/AAAA lookup + default port as fallback, or the DnsClient
            // NuGet package for full SRV support.
            // For now: resolve the domain to an IP, use a default UDP port.
            var addresses = Dns.GetHostAddresses(domain);
            if (addresses.Length == 0) return null;

            var ip = addresses[0]; // Prefer first resolved address
            var port = 7000;       // TODO: use actual SRV port when DnsClient is added

            // SRV target host may differ from the queried domain; use the resolved address
            return new ResolvedGameMaster
            {
                HttpEndpoint = new Uri($"http://{ip}:80"),
                GameAddress = new IpAndPort(
                    new CompactIpAddress(ip.GetAddressBytes()),
                    (ushort)port),
                Domain = domain
            };
        }
        catch
        {
            return null;
        }
    }

    public void Dispose()
    {
        _refreshTimer.Dispose();
    }
}
