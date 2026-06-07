using System.Text.Json.Serialization;
using MemoryPack;

namespace NFMWorldLibrary.Rad;

[MemoryPackable]
public readonly partial record struct Rad3dRimsDef(
    [property: JsonPropertyName("color"), MemoryPackOrder(0)] Color3 Color,
    [property: JsonPropertyName("size"), MemoryPackOrder(1)] float Size,
    [property: JsonPropertyName("depth"), MemoryPackOrder(2)] float Depth
);