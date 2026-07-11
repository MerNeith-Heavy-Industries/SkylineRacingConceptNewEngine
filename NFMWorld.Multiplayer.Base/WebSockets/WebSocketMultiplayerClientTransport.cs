using System.Collections.Concurrent;
using System.Net.WebSockets;
using ENet;

namespace NFMWorldLibrary.Multiplayer;

public class WebSocketMultiplayerClientTransport : BaseMultiplayerClientTransport
{
    private readonly WebSocketSharp.WebSocket _client;

    public WebSocketMultiplayerClientTransport(string hostName, ushort port = 7000)
    {
        _client = new WebSocketSharp.WebSocket($"ws://{hostName}:{port}/game");
        _client.OnMessage += (sender, e) =>
        {
            Logging.Info($"Packet received from server - Data length: {e.RawData.Length}");
            ReceivePacket(e.RawData);
        };
        _client.Connect();
    }

    protected override void SendRawPacketToServer(ReadOnlySpan<byte> span, bool reliable)
    {
        _client.Send(span.ToArray());
    }

    public override void Stop()
    {
        _client.Close();
    }
}