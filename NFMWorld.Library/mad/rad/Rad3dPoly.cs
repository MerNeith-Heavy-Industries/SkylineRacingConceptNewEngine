using System.Text.Json.Serialization;
using MessagePack;

namespace nfm_world_library.mad.rad;

[MessagePackObject]
public readonly record struct Rad3dPoly(
    [property: JsonPropertyName("c"), Key(0)] Color3 Color,
    [property: JsonPropertyName("colnum"), Key(1)] int? ColNum,
    [property: JsonPropertyName("polyType"), Key(2)] PolyType PolyType,
    [property: JsonPropertyName("lineType"), Key(3)] LineType? LineType,
    [property: JsonPropertyName("decalOffset"), Key(4)] float DecalOffset,
    [property: JsonPropertyName("p"), Key(5)] Vector3[] Points
);