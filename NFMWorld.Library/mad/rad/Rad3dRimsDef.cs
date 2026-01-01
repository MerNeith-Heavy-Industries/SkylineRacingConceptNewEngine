using System.Text.Json.Serialization;

namespace NFMWorld.Mad;

public readonly record struct Rad3dRimsDef(
    [property: JsonPropertyName("color")] Color3 Color,
    [property: JsonPropertyName("size")] float Size,
    [property: JsonPropertyName("depth")] float Depth
);