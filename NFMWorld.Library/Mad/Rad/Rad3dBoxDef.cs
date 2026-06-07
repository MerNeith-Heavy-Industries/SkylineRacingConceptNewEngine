using System.Text.Json.Serialization;
using MemoryPack;
using NFMWorldLibrary.FixedMath;

namespace NFMWorldLibrary.Rad;

[MemoryPackable]
public readonly partial record struct Rad3dBoxDef(
    [property: JsonPropertyName("xy"), MemoryPackOrder(0)] int Xy,
    [property: JsonPropertyName("zy"), MemoryPackOrder(1)] int Zy,
    [property: JsonPropertyName("rad"), MemoryPackOrder(2)] f64Vector3 Radius,
    [property: JsonPropertyName("t"), MemoryPackOrder(3)] f64Vector3 Translation,
    [property: JsonPropertyName("skid"), MemoryPackOrder(4)] int Skid,
    [property: JsonPropertyName("damage"), MemoryPackOrder(5)] int Damage,
    [property: JsonPropertyName("notwall"), MemoryPackOrder(6)] bool NotWall,
    [property: JsonPropertyName("c"), MemoryPackOrder(7)] Color3 Color
);