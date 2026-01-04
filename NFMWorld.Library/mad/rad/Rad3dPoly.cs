using System.Text.Json.Serialization;

namespace NFMWorld.Mad;

public readonly record struct Rad3dPoly(
    [property: JsonPropertyName("c")] Color3 Color,
    [property: JsonPropertyName("colnum")] int? ColNum,
    [property: JsonPropertyName("polyType")] PolyType PolyType,
    [property: JsonPropertyName("lineType")] LineType? LineType,
    [property: JsonPropertyName("decalOffset")] float DecalOffset,
    [property: JsonPropertyName("p")] Vector3[] Points
);