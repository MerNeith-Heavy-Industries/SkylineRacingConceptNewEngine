using nfm_world.multiplayer.packets.c2s;
using nfm_world.multiplayer.packets.s2c;

namespace nfm_world.multiplayer;

public interface IMultiplayerClientTransport
{
    ClientState State { get; }
    IPacketServerToClient[] GetNewPackets();
    void SendPacketToServer<T>(T packet, bool reliable = true) where T : IPacketClientToServer<T>;
    void Stop();
}