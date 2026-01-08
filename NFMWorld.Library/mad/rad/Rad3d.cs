using System.Text.Json.Serialization;
using MessagePack;

namespace nfm_world_library.mad.rad;

[MessagePackObject]
[method: SerializationConstructor]
public record Rad3d(
    [property: JsonPropertyName("colors"), Key(0)] Color3[] Colors,
    [property: JsonPropertyName("stats"), Key(1)] CarStats Stats,
    [property: JsonPropertyName("wheels"), Key(2)] Rad3dWheelDef[] Wheels,
    [property: JsonPropertyName("rims"), Key(3)] Rad3dRimsDef? Rims,
    [property: JsonPropertyName("boxes"), Key(4)] Rad3dBoxDef[] Boxes,
    [property: JsonPropertyName("polys"), Key(5)] Rad3dPoly[] Polys,
    [property: JsonPropertyName("shadow"), Key(6)] bool CastsShadow,
    [property: JsonPropertyName("atp"), Key(7)] Vector2[] Atp,
    [property: JsonPropertyName("fileName"), Key(8)] string FileName = "hogan rewish"
)
{
    [IgnoreMember] public int MaxRadius { get; } = CalculateMaxRadius(Polys);

    private static int CalculateMaxRadius(Rad3dPoly[] polys)
    {
        var maxR = 0;
        foreach (var poly in polys)
        foreach (var point in poly.Points)
        {
            var rad = (int) float.Sqrt(point.X * point.X + point.Y * point.Y + point.Z * point.Z);
            if (rad > maxR)
            {
                maxR = rad;
            }
        }

        return maxR;
    }

    public Rad3d(Rad3dPoly[] polys, bool castsShadow, string fileName) : this([], new CarStats(), [], null, [], polys, castsShadow, [], fileName)
    {
    }
}