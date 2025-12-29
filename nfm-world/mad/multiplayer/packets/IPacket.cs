using System.Buffers;
using System.Runtime.InteropServices;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using Microsoft.Xna.Framework;
using SoftFloat;
using Color = NFMWorld.Util.Color;

namespace NFMWorld.Mad.packets;

public interface IPacket;

public static class MsgPackHelpers
{
    public static MessagePackSerializerOptions Options { get; } = MessagePackSerializerOptions.Standard
        .WithSecurity(MessagePackSecurity.UntrustedData)
        .WithResolver(CompositeResolver.Create([
            new UnsafeUnmanagedStructFormatter<PlayerState>(100),
            new UnsafeUnmanagedStructFormatter<Vector2>(101),
            new UnsafeUnmanagedStructFormatter<Vector3>(102),
            new UnsafeUnmanagedStructFormatter<Vector4>(103),
            new UnsafeUnmanagedStructFormatter<Quaternion>(104),
            new UnsafeUnmanagedStructFormatter<Matrix>(105),
            new UnsafeUnmanagedStructFormatter<Color>(106),
            new UnsafeUnmanagedStructFormatter<Color3>(107),
            new UnsafeUnmanagedStructFormatter<AngleSingle>(108),
            new UnsafeUnmanagedStructFormatter<fix64>(109),
            new UnsafeUnmanagedStructFormatter<f64Vector3>(110),
            new UnsafeUnmanagedStructFormatter<DemoEntry>(111),
            new UnsafeUnmanagedStructListFormatter<DemoEntry>(112),
            new UnsafeUnmanagedStructFormatter<f64AngleSingle>(113),
            new UnsafeUnmanagedStructFormatter<f64Euler>(114),
        ], [
            StandardResolver.Instance,
            MsgPackResolver.Instance
        ]));
}

file sealed unsafe class UnsafeUnmanagedStructListFormatter<T>(sbyte typeCode) : IMessagePackFormatter<List<T>?>
    where T : unmanaged
{
    public readonly sbyte TypeCode = typeCode;

    public void Serialize(ref MessagePackWriter writer, List<T>? value, MessagePackSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNil();
            return;
        }

        var byteCount = sizeof(T) * value.Count;
        writer.WriteExtensionFormatHeader(new ExtensionHeader(TypeCode, byteCount));
        if (byteCount == 0)
        {
            return;
        }

        var destinationSpan = writer.GetSpan(byteCount);
        fixed (void* destination = &destinationSpan[0])
        fixed (void* source = &CollectionsMarshal.AsSpan(value)[0])
        {
            Buffer.MemoryCopy(source, destination, byteCount, byteCount);
        }

        writer.Advance(byteCount);
    }

    public List<T>? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            return null;
        }

        var header = reader.ReadExtensionFormatHeader();
        if (header.TypeCode != TypeCode)
        {
            throw new MessagePackSerializationException("Extension TypeCode is invalid. typeCode: " + header.TypeCode);
        }

        if (header.Length == 0)
        {
            return [];
        }

        var elementCount = header.Length / sizeof(T);
        if (elementCount * sizeof(T) != header.Length)
        {
            throw new MessagePackSerializationException("Extension Length is invalid. actual: " + header.Length + ", element size: " + sizeof(T));
        }
        if (elementCount > int.MaxValue)
        {
            throw new MessagePackSerializationException("Extension Length is too large. element count: " + elementCount);
        }

        var answer = new List<T>((int)elementCount);
        CollectionsMarshal.SetCount(answer, (int)elementCount);
        reader.ReadRaw(header.Length).CopyTo(MemoryMarshal.AsBytes(CollectionsMarshal.AsSpan(answer)));
        return answer;
    }
}

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