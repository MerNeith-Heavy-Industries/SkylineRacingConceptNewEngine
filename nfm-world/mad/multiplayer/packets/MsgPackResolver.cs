using MessagePack;
using Microsoft.Xna.Framework;
using nfm_world_library.SoftFloat;
using nfm_world.files.demo;
using nfm_world.multiplayer;

[assembly: MessagePackAssumedFormattable(typeof(PlayerState))]
[assembly: MessagePackAssumedFormattable(typeof(Vector2))]
[assembly: MessagePackAssumedFormattable(typeof(Vector3))]
[assembly: MessagePackAssumedFormattable(typeof(Vector4))]
[assembly: MessagePackAssumedFormattable(typeof(Quaternion))]
[assembly: MessagePackAssumedFormattable(typeof(Matrix))]
[assembly: MessagePackAssumedFormattable(typeof(Color))]
[assembly: MessagePackAssumedFormattable(typeof(Color3))]
[assembly: MessagePackAssumedFormattable(typeof(AngleSingle))]
[assembly: MessagePackAssumedFormattable(typeof(fix64))]
[assembly: MessagePackAssumedFormattable(typeof(f64Vector3))]
[assembly: MessagePackAssumedFormattable(typeof(DemoEntry))]
[assembly: MessagePackAssumedFormattable(typeof(List<DemoEntry>))]

namespace nfm_world.multiplayer.packets;

[GeneratedMessagePackResolver]
internal partial class MsgPackResolver;