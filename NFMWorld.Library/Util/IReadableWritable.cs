using System.Buffers;
using MemoryPack;

namespace NFMWorldLibrary.Util;

public interface IReadableWritable<out TSelf>
{
    void Write<T>(T writer) where T : IBufferWriter<byte>
    {
        MemoryPackSerializer.Serialize<TSelf, T>(writer, (TSelf)this, MemoryPackHelpers.Options);
    }

    public static virtual TSelf? Read(ReadOnlyMemory<byte> data)
    {
        return MemoryPackSerializer.Deserialize<TSelf>(data.Span, MemoryPackHelpers.Options);
    }
}