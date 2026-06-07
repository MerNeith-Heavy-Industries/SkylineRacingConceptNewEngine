using System.Text.Json.Serialization;
using MemoryPack;

namespace NFMWorldLibrary.Rad;

[MemoryPackable(GenerateType.CircularReference)]
public readonly partial record struct Rad3dPoly(
    [property: JsonPropertyName("c"), MemoryPackOrder(0)] Color3 Color,
    [property: JsonPropertyName("colnum"), MemoryPackOrder(1)] int? ColNum,
    [property: JsonPropertyName("polyType"), MemoryPackOrder(2)] PolyType PolyType,
    [property: JsonPropertyName("lineType"), MemoryPackOrder(3)] LineType? LineType,
    [property: JsonPropertyName("decalOffset"), MemoryPackOrder(4)] float DecalOffset,
    [property: JsonPropertyName("p"), MemoryPackOrder(5)] Vector3[] Points,
    [property: JsonPropertyName("tri"), MemoryPackOrder(6)] uint[]? Triangles = null
)
{
    private readonly int _hashCode = CalculateHashCode(Color, ColNum, PolyType, LineType, DecalOffset, Points, Triangles);

    [MemoryPackConstructor]
    public Rad3dPoly() : this(default, null, default, null, 0, [])
    {
    }

    public bool Equals(Rad3dPoly other)
    {
        if (!Color.Equals(other.Color)) return false;
        if (ColNum != other.ColNum) return false;
        if (PolyType != other.PolyType) return false;
        if (LineType != other.LineType) return false;
        if (!DecalOffset.Equals(other.DecalOffset)) return false;
        return Points.SequenceEqual(other.Points);
    }

    private static int CalculateHashCode(Color3 color, int? colNum, PolyType polyType, LineType? lineType, float decalOffset, ReadOnlySpan<Vector3> points, ReadOnlySpan<uint> triangles)
    {
        var hashCode = new HashCode();
        hashCode.Add(color);
        hashCode.Add(colNum);
        hashCode.Add(polyType);
        hashCode.Add(lineType);
        hashCode.Add(decalOffset);
        hashCode.Add(points.Length);
        foreach (var point in points)
        {
            hashCode.Add(point);
        }
        hashCode.Add(triangles.Length);
        foreach (var triangle in triangles)
        {
            hashCode.Add(triangle);
        }
        return hashCode.ToHashCode();
    }

    public override int GetHashCode()
    {
        return _hashCode;
    }
}