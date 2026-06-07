using MemoryPack;

namespace NFMWorldLibrary.Util;

public static class MemoryPackHelpers
{
    public static MemoryPackSerializerOptions Options = new MemoryPackSerializerOptions
    {
        StringEncoding = StringEncoding.Utf8
    };

    static MemoryPackHelpers()
    {
        MemoryPackFormatterProvider.RegisterGenericType(typeof(UnlimitedArray<>), typeof(UnlimitedArrayFormatter<>));
    }
}