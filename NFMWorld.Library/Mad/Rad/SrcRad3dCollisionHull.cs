using System.Diagnostics;
using MessagePack;
using NFMWorldLibrary.Collision;

namespace NFMWorldLibrary.Rad;

[MessagePackObject]
[method: SerializationConstructor]
public readonly record struct SrcRad3dCollisionHull([property: Key(0)] f64Vector3[] Vertices, [property: Key(1)] ushort[] Indices)
{
    private static readonly ConvexHullCalculator _calculator = new();
    
    // ReSharper disable once ConditionalTernaryEqualBranch
    public SrcRad3dCollisionHull(ReadOnlySpan<f64Vector3> hullVerts) : this(Parse(hullVerts) is var v ? v.Vertices : v.Vertices, v.Indices)
    {
    }

    private static (f64Vector3[] Vertices, ushort[] Indices) Parse(ReadOnlySpan<f64Vector3> hullVerts)
    {
        var stopwatch = Stopwatch.StartNew();
        var verts = new List<f64Vector3>(hullVerts.Length);
        var indices = new List<ushort>(hullVerts.Length / 3);
        _calculator.GenerateHull(hullVerts, false, verts, indices, null);
        
        Logging.Debug($"Convex hull generated with {verts.Count} vertices and {indices.Count / 3} triangles in {stopwatch.Elapsed.TotalSeconds:F2} seconds");
        
        return (verts.ToArray(), indices.ToArray());
    }
}