using System.Text.Json.Serialization;
using FixedMathSharp;
using MemoryPack;
using NFMWorldLibrary.FixedMath;

namespace NFMWorldLibrary.Rad;

[MemoryPackable(GenerateType.CircularReference)]
public sealed partial record Rad3d(
    [property: JsonPropertyName("colors"), MemoryPackOrder(0)] Color3[] Colors,
    [property: JsonPropertyName("stats"), MemoryPackOrder(1)] CarStats Stats,
    [property: JsonPropertyName("wheels"), MemoryPackOrder(2)] Rad3dWheelDef[] Wheels,
    [property: JsonPropertyName("rims"), MemoryPackOrder(3)] Rad3dRimsDef? Rims,
    [property: JsonPropertyName("boxes"), MemoryPackOrder(4)] Rad3dBoxDef[] Boxes,
    [property: JsonPropertyName("polys"), MemoryPackOrder(5)] Rad3dPoly[] Polys,
    [property: JsonPropertyName("shadow"), MemoryPackOrder(6)] bool CastsShadow,
    [property: JsonPropertyName("atp"), MemoryPackOrder(7)] Vector2[] Atp,
    [property: JsonPropertyName("fileName"), MemoryPackOrder(8)] string FileName = "hogan rewish",
    [property: JsonPropertyName("collisionMesh"), MemoryPackOrder(9)] SrcRad3dCollisionMesh? CollisionMesh = null,
    [property: JsonPropertyName("collisionHull"), MemoryPackOrder(10)] SrcRad3dCollisionHull? CollisionHull = null,
    [property: JsonPropertyName("atLines"), MemoryPackOrder(11)] Rad3dAttachmentLine[]? AtLines = null
)
{
    [MemoryPackIgnore] public int MaxRadius { get; } = CalculateMaxRadius(Polys);

    private readonly int _hashCode = CalculateHashCode(Colors, Stats, Wheels, Rims, Boxes, Polys, CastsShadow, Atp, CollisionMesh, CollisionHull, AtLines);
    private readonly int _visualHashCode = CalculateVisualHashCode(Colors, Wheels, Rims, Polys, CastsShadow);

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

    [MemoryPackConstructor]
    private Rad3d() : this([], default, [], null, [], [], false, [])
    {
    }

    public bool Equals(Rad3d? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (!Colors.SequenceEqual(other.Colors)) return false;
        if (!Stats.Equals(other.Stats)) return false;
        if (!Wheels.SequenceEqual(other.Wheels)) return false;
        if (!Nullable.Equals(Rims, other.Rims)) return false;
        if (!Boxes.SequenceEqual(other.Boxes)) return false;
        if (!Polys.SequenceEqual(other.Polys)) return false;
        if (CastsShadow != other.CastsShadow) return false;
        if (!Atp.SequenceEqual(other.Atp)) return false;
        if (CollisionMesh != null && !CollisionMesh.Equals(other.CollisionMesh)) return false;
        if (CollisionMesh == null && other.CollisionMesh != null) return false;
        if (CollisionHull != null && !CollisionHull.Equals(other.CollisionHull)) return false;
        if (CollisionHull == null && other.CollisionHull != null) return false;
        if (AtLines != null && !AtLines.SequenceEqual(other.AtLines)) return false;
        if (AtLines == null && other.AtLines != null) return false;
        return true;
    }

    private static int CalculateHashCode(
        Color3[] colors,
        CarStats stats,
        Rad3dWheelDef[] wheels,
        Rad3dRimsDef? rims,
        Rad3dBoxDef[] boxes,
        Rad3dPoly[] polys,
        bool castsShadow,
        Vector2[] atp,
        SrcRad3dCollisionMesh? colMesh,
        SrcRad3dCollisionHull? colHull,
        Rad3dAttachmentLine[]? atLines
    )
    {
        var hashCode = new HashCode();
        hashCode.Add(colors.Length);
        foreach (var color in colors)
        {
            hashCode.Add(color);
        }
        hashCode.Add(stats);
        hashCode.Add(wheels.Length);
        foreach (var wheel in wheels)
        {
            hashCode.Add(wheel);
        }
        hashCode.Add(rims);
        hashCode.Add(boxes.Length);
        foreach (var box in boxes)
        {
            hashCode.Add(box);
        }
        hashCode.Add(polys.Length);
        foreach (var poly in polys)
        {
            hashCode.Add(poly);
        }
        hashCode.Add(castsShadow);
        hashCode.Add(atp.Length);
        foreach (var at in atp)
        {
            hashCode.Add(at);
        }

        if (colMesh != null)
        {
            hashCode.Add(colMesh);
        }
        if (colHull != null)
        {
            hashCode.Add(colHull);
        }
        if (atLines != null)
        {
            hashCode.Add(atLines.Length);
            foreach (var atLine in atLines)
            {
                hashCode.Add(atLine);
            }
        }
        return hashCode.ToHashCode();
    }
    
    private static int CalculateVisualHashCode(Color3[] colors, Rad3dWheelDef[] wheels, Rad3dRimsDef? rims, Rad3dPoly[] polys, bool castsShadow)
    {
        var hashCode = new HashCode();
        hashCode.Add(colors.Length);
        foreach (var color in colors)
        {
            hashCode.Add(color);
        }
        hashCode.Add(wheels.Length);
        foreach (var wheel in wheels)
        {
            hashCode.Add(wheel);
        }
        hashCode.Add(rims);
        hashCode.Add(polys.Length);
        foreach (var poly in polys)
        {
            hashCode.Add(poly);
        }
        hashCode.Add(castsShadow);
        return hashCode.ToHashCode();
    }

    public override int GetHashCode()
    {
        return _hashCode;
    }

    public Rad3d(Rad3dPoly[] polys, bool castsShadow, string fileName) : this([], new CarStats(), [], null, [], polys, castsShadow, [], fileName)
    {
    }

    public class VisualEqualityComparer : IEqualityComparer<Rad3d>
    {
        public static VisualEqualityComparer Instance { get; } = new();
        
        public bool Equals(Rad3d? x, Rad3d? y)
        {
            if (x is null && y is null) return true;
            if (x is null || y is null) return false;
            if (!x.Colors.SequenceEqual(y.Colors)) return false;
            if (!x.Wheels.SequenceEqual(y.Wheels)) return false;
            if (!Nullable.Equals(x.Rims, y.Rims)) return false;
            if (!x.Polys.SequenceEqual(y.Polys)) return false;
            if (x.CastsShadow != y.CastsShadow) return false;
            return true;
        }

        public int GetHashCode(Rad3d obj)
        {
            return obj._visualHashCode;
        }
    }
}