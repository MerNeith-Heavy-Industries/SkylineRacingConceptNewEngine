using System.Text.Json.Serialization;
using nfm_world_library.SoftFloat;

namespace nfm_world_library.mad.rad;

public readonly record struct Rad3dWheelDef(
    [property: JsonPropertyName("pos")] f64Vector3 Position,
    [property: JsonPropertyName("rotates")] int Rotates,
    [property: JsonPropertyName("w")] fix64 Width,
    [property: JsonPropertyName("h")] fix64 Height
)
{
    public int Sparkat { get; } = (int) fix64.Round((Height / (fix64)10f) * (fix64)24.0F);
    public int Ground { get; } = (int) fix64.Round(Position.Y + (fix64)13.0F * (Height / (fix64)10f));
}