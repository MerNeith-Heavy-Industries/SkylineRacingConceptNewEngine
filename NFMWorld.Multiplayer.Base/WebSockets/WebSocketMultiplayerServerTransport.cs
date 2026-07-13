using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using ENet;
using WebSocketSharp;
using WebSocketSharp.Server;
using ErrorEventArgs = WebSocketSharp.ErrorEventArgs;

namespace NFMWorldLibrary.Multiplayer;

public class WebSocketMultiplayerServerTransport : BaseMultiplayerServerTransport
{
    private uint _lastId = 0;
    
    private readonly ConcurrentDictionary<uint, WebSocketSession> _connectedClients = [];
    private readonly HttpServer _server;
    private readonly ConcurrentQueue<(uint Peer, Packet Packet)> _sendPacketQueue = [];

    public override IReadOnlyCollection<uint> Connections { get; }
    
    public override event EventHandler<uint>? ClientConnecting;
    public override event EventHandler<uint>? ClientConnected;
    public override event EventHandler<uint>? ClientDisconnected;
    
    private class ConnectionsList(WebSocketMultiplayerServerTransport parent) : IReadOnlyCollection<uint>
    {
        public IEnumerator<uint> GetEnumerator()
        {
            foreach (var client in parent._connectedClients)
            {
                yield return client.Key;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count => parent._connectedClients.Count;
    }
    
    private class WebSocketSession : WebSocketBehavior
    {
        private uint? _id;
        public uint ClientId
        {
            get
            {
                CheckTransport();
                return _id ??= Transport._lastId++;
            }
        }

        public WebSocketMultiplayerServerTransport? Transport { get; set; }

        [MemberNotNull(nameof(Transport))]
        private void CheckTransport()
        {
            if (Transport == null) throw new InvalidOperationException();
        }

        protected override void OnOpen()
        {
            CheckTransport();
            base.OnOpen();
            Logging.Info($"Client connected - ID: {ID}, IP: {UserEndPoint}");
            Transport.Connected(this);
        }

        protected override void OnClose(CloseEventArgs e)
        {
            CheckTransport();
            base.OnClose(e);
            Logging.Info($"Client disconnected - ID: {ID}, IP: {UserEndPoint}");
            Transport.Disconnected(this);
        }

        protected override void OnError(ErrorEventArgs e)
        {
            CheckTransport();
            base.OnError(e);
            Logging.Error($"WebSocket error for client {ID} ({UserEndPoint}): {e.Message}", exception: e.Exception);
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            CheckTransport();
            base.OnMessage(e);

            Logging.Info($"Packet received from - ID: {ID}, IP: {UserEndPoint}, Data length: {e.RawData.Length}");
            var messageData = e.RawData;
            Transport.ReceivePacket(ClientId, messageData);
        }

        public void SendData(byte[] data)
        {
            Send(data);
        }
    }

    private void Disconnected(WebSocketSession session)
    {
        ClientDisconnected?.Invoke(this, session.ClientId);
        _connectedClients.TryRemove(session.ClientId, out _);
    }

    private void Connected(WebSocketSession session)
    {
        ClientConnecting?.Invoke(this, session.ClientId);
        ClientConnected?.Invoke(this, session.ClientId);
        _connectedClients.TryAdd(session.ClientId, session);
    }

    public WebSocketMultiplayerServerTransport(HttpServer httpServer)
    {
        Connections = new ConnectionsList(this);
        
        _server = httpServer;
        _server.AddWebSocketService<WebSocketSession>("/game", behavior => behavior.Transport = this);
        _server.KeepClean = true;
    }

    public override void SendRawPacketToClients(ReadOnlySpan<uint> clientIndices, ReadOnlySpan<byte> span, bool reliable)
    {
        var data = span.ToArray();
        foreach (var clientIndex in clientIndices)
        {
            if (_connectedClients.TryGetValue(clientIndex, out var session))
            {
                session.SendData(data);
            }
        }
    }

    public override void Stop()
    {
    }

    public override void Start()
    {
    }
}
