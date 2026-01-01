using System.Text.Json.Serialization;
using SoftFloat;

namespace NFMWorld.Mad;

public readonly record struct Rad3dBoxDef(
    [property: JsonPropertyName("xy")] int Xy,
    [property: JsonPropertyName("zy")] int Zy,
    [property: JsonPropertyName("rad")] f64Vector3 Radius,
    [property: JsonPropertyName("t")] f64Vector3 Translation,
    [property: JsonPropertyName("skid")] int Skid,
    [property: JsonPropertyName("damage")] int Damage,
    [property: JsonPropertyName("notwall")] bool NotWall,
    [property: JsonPropertyName("c")] Color3 Color
);