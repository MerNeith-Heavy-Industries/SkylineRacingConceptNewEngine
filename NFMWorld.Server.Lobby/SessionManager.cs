using System.Collections.Concurrent;
using NFMWorldLibrary.Multiplayer.Packets.S2C;

namespace NFMWorldLibrary.Multiplayer;

/// <summary>
/// Manages game sessions: create, join, leave, player-ready tracking, and timeout.
/// </summary>
public class SessionManager
{
    private readonly ConcurrentDictionary<uint, GameSession> _sessions = new();
    private uint _maxSessionId;
    private readonly IMultiplayerServerTransport _transport;
    private readonly PlayerRegistry _players;

    public SessionManager(IMultiplayerServerTransport transport, PlayerRegistry players)
    {
        _transport = transport;
        _players = players;
    }

    public IEnumerable<KeyValuePair<uint, GameSession>> All => _sessions;

    public GameSession? Get(uint sessionId)
    {
        _sessions.TryGetValue(sessionId, out var session);
        return session;
    }

    /// <summary>Creates a new session. Returns the created session.</summary>
    public GameSession CreateSession(uint creatorClientId, string stageName, byte maxPlayers, GameModes gamemode = GameModes.Sandbox)
    {
        var creator = _players.Get(creatorClientId);
        var session = new GameSession
        {
            Id = Interlocked.Increment(ref _maxSessionId),
            CreatorId = creatorClientId,
            CreatorName = creator?.Name ?? "Unknown",
            StageName = stageName,
            MaxPlayers = maxPlayers,
            Gamemode = gamemode,
            PlayerClientIds = new ConcurrentDictionary<byte, uint> { [0] = creatorClientId }
        };

        if (creator is not null)
            creator.InSession = (0, session.Id);

        _sessions.TryAdd(session.Id, session);
        return session;
    }

    /// <summary>
    /// Attempts to join a session. Handles auto-leave from any existing session.
    /// Returns (sessionJoined, oldSessionLeft).
    /// </summary>
    public (GameSession? Joined, GameSession? Left) JoinSession(uint clientId, uint sessionId)
    {
        var player = _players.Get(clientId);
        if (player is null) return (null, null);

        GameSession? leftSession = null;

        // Leave current session if in one
        if (player.InSession is { } current &&
            _sessions.TryGetValue(current.SessionIndex, out var leaving))
        {
            leaving.PlayerClientIds.TryRemove(
                KeyValuePair.Create(current.PlayerIndex, clientId));
            player.InSession = null;
            leftSession = leaving;
        }

        // Join new session
        if (_sessions.TryGetValue(sessionId, out var target) &&
            target.PlayerClientIds.Count < target.MaxPlayers)
        {
            byte playerIndex = 0;
            while (target.PlayerClientIds.ContainsKey(playerIndex))
                playerIndex++;

            target.PlayerClientIds[playerIndex] = clientId;
            player.InSession = (playerIndex, target.Id);
            return (target, leftSession);
        }

        return (null, leftSession);
    }

    /// <summary>Leaves the player's current session. Returns the session left, if any.</summary>
    public GameSession? LeaveSession(uint clientId, uint sessionId)
    {
        var player = _players.Get(clientId);
        if (player?.InSession is { } current &&
            current.SessionIndex == sessionId &&
            _sessions.TryGetValue(sessionId, out var session))
        {
            session.PlayerClientIds.TryRemove(
                KeyValuePair.Create(current.PlayerIndex, clientId));
            player.InSession = null;
            return session;
        }

        return null;
    }

    /// <summary>Marks a session as started/loading and sets the load timeout.</summary>
    public bool StartRace(uint clientId, uint sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var session) &&
            session.PlayerClientIds.Any(e => e.Value == clientId) &&
            session.State == SessionState.NotStarted)
        {
            session.State = SessionState.WaitingToLoad;
            session.StartTime = DateTimeOffset.Now.AddSeconds(20);

            foreach (var (_, id) in session.PlayerClientIds)
            {
                var player = _players.Get(id);
                if (player is not null)
                    player.IsInGame = true;
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks for timed-out sessions (WaitingToLoad past StartTime).
    /// Returns sessions that have timed out. Caller must handle cleanup + notification.
    /// </summary>
    public List<GameSession> CheckTimeouts()
    {
        var timedOut = new List<GameSession>();
        foreach (var (_, session) in _sessions)
        {
            if (session.State == SessionState.WaitingToLoad &&
                session.StartTime is { } startTime &&
                DateTimeOffset.Now >= startTime)
            {
                session.State = SessionState.Finished;
                timedOut.Add(session);

                foreach (var (_, clientId) in session.PlayerClientIds)
                {
                    var player = _players.Get(clientId);
                    if (player is not null)
                    {
                        player.InSession = null;
                        player.IsInGame = false;
                    }
                }
            }
        }

        return timedOut;
    }

    /// <summary>Marks a player as ready/unready in their session.</summary>
    public bool SetPlayerReady(uint clientId, uint sessionId, bool isReady)
    {
        var player = _players.Get(clientId);
        if (player?.InSession is { } current &&
            current.SessionIndex == sessionId &&
            _sessions.TryGetValue(sessionId, out var session) &&
            session.State == SessionState.NotStarted)
        {
            // Readiness is tracked implicitly — all players must be ready before start.
            // For v1 we just validate that the player is in the session.
            // A future ReadyState field on ClientInfo could be added here.
            return true;
        }

        return false;
    }

    public class GameSession
    {
        public required uint Id { get; set; }
        public required uint CreatorId { get; set; }
        public required string CreatorName { get; set; }
        public required string StageName { get; set; }
        public int MaxPlayers { get; set; }
        public ConcurrentDictionary<byte, uint> PlayerClientIds { get; set; } = [];
        public DateTimeOffset? StartTime { get; set; }
        public SessionState State { get; set; } = SessionState.NotStarted;
        public GameModes Gamemode { get; set; } = GameModes.Sandbox;
    }
}
