using System.Collections.Concurrent;
using NFMWorldLibrary.Multiplayer.Packets.S2C;
using NFMWorldLibrary.Util;

namespace NFMWorldLibrary.Multiplayer;

/// <summary>
/// Tracks connected players: identity (name/vehicle/color), connection state,
/// session membership, and in-game status.
/// </summary>
public class PlayerRegistry
{
    private readonly ConcurrentDictionary<uint, ClientInfo> _clients = new();

    public ClientInfo GetOrAdd(uint clientId, ClientState state = ClientState.Connecting)
    {
        return _clients.GetOrAdd(clientId, _ => new ClientInfo { State = state });
    }

    public ClientInfo? Get(uint clientId)
    {
        _clients.TryGetValue(clientId, out var client);
        return client;
    }

    public bool TryRemove(uint clientId, out ClientInfo? client)
    {
        return _clients.TryRemove(clientId, out client);
    }

    public IEnumerable<KeyValuePair<uint, ClientInfo>> All => _clients;

    public int Count => _clients.Count;

    /// <summary>
    /// Inner types must match the original GameOrchestrator inner classes
    /// for binary compatibility with live sessions.
    /// </summary>
    public class ClientInfo
    {
        public ClientState State { get; set; }
        public string Name { get; set; } = "hogan rewish";
        public string Vehicle { get; set; } = "nfmm/radicalone";
        public Color3 Color { get; set; }
        public (byte PlayerIndex, uint SessionIndex)? InSession { get; set; }
        public bool IsInGame { get; set; }
    }
}
