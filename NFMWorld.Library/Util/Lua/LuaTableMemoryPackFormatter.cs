using System.Buffers;
using MemoryPack;
using MemoryPack.Internal;
using NFMWorldLibrary.FixedMath;
using NuLua;
using NuLua.Luau;

namespace NFMWorldLibrary.Util;

public class LuaValueMemoryPackFormatter
{
    private const ushort TagNil = 0;
    private const ushort TagFalse = 1;
    private const ushort TagTrue = 2;
    private const ushort TagStr = 3;
    private const ushort TagNum = 4;
    private const ushort TagTab = 5;
    private const ushort TagFix64 = 6;
    private const ushort TagFix64V = 7;
    private const ushort TagFix64A = 8;
    private const ushort TagFix64E = 9;
    
    public static LuaRefValue Deserialize(LuauState luauState, Span<byte> data)
    {
        using var state = MemoryPackReaderOptionalStatePool.Rent(null);

        var reader = new MemoryPackReader(data, state);
        try
        {
            LuaRefValue value = default;
            Deserialize(luauState, ref reader, ref value);
            return value;
        }
        finally
        {
            reader.Dispose();
            state.Reset();
        }
    }

    public static byte[] Serialize(LuaRefValue value)
    {
        using var state = MemoryPackWriterOptionalStatePool.Rent(null);
        
        var bufferWriter = new ReusableLinkedArrayBufferWriter(useFirstBuffer: true, pinned: true);

        try
        {
            var writer = new MemoryPackWriter<ReusableLinkedArrayBufferWriter>(ref bufferWriter, bufferWriter.DangerousGetFirstBuffer(), state);
            Serialize(ref writer, ref value);
            return bufferWriter.ToArrayAndReset();
        }
        finally
        {
            bufferWriter.Reset();
            state.Reset();
        }
    }

    private static void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref LuaRefValue value) where TBufferWriter : IBufferWriter<byte>
    {
        WriteLuaValue(ref writer, ref value);
    }

    private static void WriteLuaValue<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref readonly LuaRefValue value) where TBufferWriter : IBufferWriter<byte>
    {
        switch (value.Type)
        {
            case LuaValueType.Nil:
                writer.WriteUnionHeader(TagNil);
                break;
            case LuaValueType.Boolean:
                if (value.Read<bool>())
                {
                    writer.WriteUnionHeader(TagTrue);
                }
                else
                {
                    writer.WriteUnionHeader(TagFalse);
                }
                break;
            case LuaValueType.String:
                var str = value.ToString();
                writer.WriteUnionHeader(TagStr);
                writer.WriteString(str);
                break;
            case LuaValueType.Number:
                var num = value.Read<double>();
                writer.WriteUnionHeader(TagNum);
                writer.WriteUnmanaged(num);
                break;
            case LuaValueType.Function:
                throw new InvalidOperationException("Type function not serializable!");
            case LuaValueType.Thread:
                throw new InvalidOperationException("Type thread not serializable!");
            case LuaValueType.LightUserData:
                throw new InvalidOperationException("Type lightuserdata not serializable!");
            case LuaValueType.UserData:
                throw new InvalidOperationException("Type userdata not serializable!");
            case LuaValueType.Table:
                var t = value.Read<LuaTableRef>();
                var len = t.Length;
                writer.WriteUnionHeader(TagTab);
                writer.WriteCollectionHeader(len);
                foreach (var (k, v) in t)
                {
                    WriteLuaValue(ref writer, in k);
                    WriteLuaValue(ref writer, in v);
                }
                break;
            case LuaValueType.Primitive:
                if (value.TryGetPrimitiveId(out var id))
                {
                    if (id == fix64.PrimitiveId)
                    {
                        var fixed64 = value.ReadPrimitive<fix64>();
                        writer.WriteUnionHeader(TagFix64);
                        writer.WriteUnmanaged(fixed64);
                    }
                    else if (id == f64Vector3.PrimitiveId)
                    {
                        var fixed64Vec3 = value.ReadPrimitive<f64Vector3>();
                        writer.WriteUnionHeader(TagFix64V);
                        writer.WriteUnmanaged(fixed64Vec3);
                    }
                    else if (id == f64AngleSingle.PrimitiveId)
                    {
                        var fixed64Ang = value.Read<f64AngleSingle>();
                        writer.WriteUnionHeader(TagFix64A);
                        writer.WriteUnmanaged(fixed64Ang);
                    }
                    else if (id == f64Euler.PrimitiveId)
                    {
                        var fixed64Eul = value.Read<f64Euler>();
                        writer.WriteUnionHeader(TagFix64E);
                        writer.WriteUnmanaged(fixed64Eul);
                    }
                    else
                    {
                        throw new InvalidOperationException("Type not serializable!");
                    }
                }
                else
                {
                    throw new InvalidOperationException("Type not serializable!");
                }
                break;
            default:
                throw new InvalidOperationException("Type not serializable!");
        }
    }

    public static void Deserialize(LuauState state, ref MemoryPackReader reader, scoped ref LuaRefValue value)
    {
        ReadLuaValue(state, ref reader, ref value);
    }

    private static void ReadLuaValue(LuauState state, ref MemoryPackReader reader, scoped ref LuaRefValue value)
    {
        if (!reader.TryReadUnionHeader(out var tag))
            throw new InvalidOperationException("Type not deserializable!");

        switch (tag)
        {
            case TagNil:
                value = LuaRefValue.Nil;
                break;
            case TagFalse:
                value = LuaRefValue.FromBoolean(false);
                break;
            case TagTrue:
                value = LuaRefValue.FromBoolean(true);
                break;
            case TagStr:
                value = reader.ReadString()!;
                break;
            case TagNum:
                value = reader.ReadUnmanaged<double>();
                break;
            case TagTab:
                if (!reader.TryReadCollectionHeader(out var len))
                {
                    throw new InvalidOperationException("Type not deserializable!");
                }

                var t = state.CreateTable();

                for (var i = 0; i < len; i++)
                {
                    LuaRefValue k = default;
                    LuaRefValue v = default;
                    ReadLuaValue(state, ref reader, ref k);
                    ReadLuaValue(state, ref reader, ref v);

                    t[k] = v;
                }
                
                value = t;
                break;
            case TagFix64:
                value = LuaRefValue.FromPrimitive(reader.ReadUnmanaged<fix64>());
                break;
            case TagFix64V:
                value = LuaRefValue.FromPrimitive(reader.ReadUnmanaged<f64Vector3>());
                break;
            case TagFix64A:
                value = LuaRefValue.FromPrimitive(reader.ReadUnmanaged<f64AngleSingle>());
                break;
            case TagFix64E:
                value = LuaRefValue.FromPrimitive(reader.ReadUnmanaged<f64Euler>());
                break;
            default:
                throw new InvalidOperationException("Type not deserializable!");
        }
    }
}
