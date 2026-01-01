using System.Text.Json.Serialization;

namespace NFMWorld.Mad;

public record Rad3d(
    [property: JsonPropertyName("colors")] Color3[] Colors,
    [property: JsonPropertyName("stats")] CarStats Stats,
    [property: JsonPropertyName("wheels")] Rad3dWheelDef[] Wheels,
    [property: JsonPropertyName("rims")] Rad3dRimsDef? Rims,
    [property: JsonPropertyName("boxes")] Rad3dBoxDef[] Boxes,
    [property: JsonPropertyName("polys")] Rad3dPoly[] Polys,
    [property: JsonPropertyName("shadow")] bool CastsShadow,
    [property: JsonPropertyName("atp")] Vector2[] Atp,
    [property: JsonPropertyName("fileName")] string FileName = "hogan rewish"
)
{
    public int MaxRadius { get; } = CalculateMaxRadius(Polys);

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

    public Rad3d(Rad3dPoly[] polys, bool castsShadow) : this([], new CarStats(), [], null, [], polys, castsShadow, [])
    {
    }
}