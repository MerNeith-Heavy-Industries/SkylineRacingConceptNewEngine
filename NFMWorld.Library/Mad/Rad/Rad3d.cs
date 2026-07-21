using System.Text.Json.Serialization;
using FixedMathSharp;
using MemoryPack;
using NFMWorldLibrary.FixedMath;

namespace NFMWorldLibrary.Rad;

// init properties aren't compatible with CircularReference, so can't use record
[MemoryPackable(GenerateType.CircularReference)]
public sealed partial class Rad3d(
    Color3[] Colors,
    CarStats Stats,
    Rad3dWheelDef[] Wheels,
    Rad3dRimsDef? Rims,
    Rad3dBoxDef[] Boxes,
    Rad3dPoly[] Polys,
    bool CastsShadow,
    Vector2[] Atp,
    string FileName = "hogan rewish",
    SrcRad3dCollisionMesh? CollisionMesh = null,
    SrcRad3dCollisionHull? CollisionHull = null,
    Rad3dAttachmentLine[]? AtLines = null
)
{
    [MemoryPackIgnore] public int MaxRadius { get; } = CalculateMaxRadius(Polys);

    [JsonPropertyName("colors"), MemoryPackOrder(0)]
    public Color3[] Colors { get; set; } = Colors;

    [JsonPropertyName("stats"), MemoryPackOrder(1)]
    public CarStats Stats { get; set; } = Stats;

    [JsonPropertyName("wheels"), MemoryPackOrder(2)]
    public Rad3dWheelDef[] Wheels { get; set; } = Wheels;

    [JsonPropertyName("rims"), MemoryPackOrder(3)]
    public Rad3dRimsDef? Rims { get; set; } = Rims;

    [JsonPropertyName("boxes"), MemoryPackOrder(4)]
    public Rad3dBoxDef[] Boxes { get; set; } = Boxes;

    [JsonPropertyName("polys"), MemoryPackOrder(5)]
    public Rad3dPoly[] Polys { get; set; } = Polys;

    [JsonPropertyName("shadow"), MemoryPackOrder(6)]
    public bool CastsShadow { get; set; } = CastsShadow;

    [JsonPropertyName("atp"), MemoryPackOrder(7)]
    public Vector2[] Atp { get; set; } = Atp;

    [JsonPropertyName("fileName"), MemoryPackOrder(8)]
    public string FileName { get; set; } = FileName;

    [JsonPropertyName("collisionMesh"), MemoryPackOrder(9)]
    public SrcRad3dCollisionMesh? CollisionMesh { get; set; } = CollisionMesh;

    [JsonPropertyName("collisionHull"), MemoryPackOrder(10)]
    public SrcRad3dCollisionHull? CollisionHull { get; set; } = CollisionHull;

    [JsonPropertyName("atLines"), MemoryPackOrder(11)]
    public Rad3dAttachmentLine[]? AtLines { get; set; } = AtLines;

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

    public void Deconstruct(out Color3[] Colors, out CarStats Stats, out Rad3dWheelDef[] Wheels, out Rad3dRimsDef? Rims, out Rad3dBoxDef[] Boxes, out Rad3dPoly[] Polys, out bool CastsShadow, out Vector2[] Atp, out string FileName, out SrcRad3dCollisionMesh? CollisionMesh, out SrcRad3dCollisionHull? CollisionHull, out Rad3dAttachmentLine[]? AtLines)
    {
        Colors = this.Colors;
        Stats = this.Stats;
        Wheels = this.Wheels;
        Rims = this.Rims;
        Boxes = this.Boxes;
        Polys = this.Polys;
        CastsShadow = this.CastsShadow;
        Atp = this.Atp;
        FileName = this.FileName;
        CollisionMesh = this.CollisionMesh;
        CollisionHull = this.CollisionHull;
        AtLines = this.AtLines;
    }
}