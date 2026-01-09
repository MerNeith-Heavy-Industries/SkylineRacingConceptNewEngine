using System.Buffers;
using MessagePack;

namespace nfm_world_library.util;

public interface IReadableWritable<out TSelf>
{
    void Write<T>(T writer) where T : IBufferWriter<byte>
    {
        MessagePackSerializer.Serialize<TSelf>(writer, (TSelf)this, MsgPackHelpers.Options);
    }

    public static virtual TSelf Read(ReadOnlyMemory<byte> data)
    {
        return MessagePackSerializer.Deserialize<TSelf>(data, MsgPackHelpers.Options);
    }
}