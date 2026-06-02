using MessagePack;
using NFMWorldLibrary.Collision;
using NFMWorldLibrary.FixedMath;

namespace NFMWorldLibrary.Rad;

[MessagePackObject]
[method: SerializationConstructor]
public readonly record struct SrcRad3dCollisionMesh([property: Key(0)] f64Vector3[] Vertices, [property: Key(1)] ushort[] Indices)
{
    [IgnoreMember] public (f64Vector3 min, f64Vector3 max)[] Aabb { get; } = CalculateAabb(Vertices, Indices);

    private static (f64Vector3 min, f64Vector3 max)[] CalculateAabb(ReadOnlySpan<f64Vector3> vertices, ReadOnlySpan<ushort> indices)
    {
        var aabbs = new (f64Vector3 min, f64Vector3 max)[indices.Length / 3];
        for (var i = 0; i < indices.Length; i += 3)
        {
            ref readonly var v0 = ref vertices[indices[i]];
            ref readonly var v1 = ref vertices[indices[i + 1]];
            ref readonly var v2 = ref vertices[indices[i + 2]];

            aabbs[i / 3] = TriangleMesh.ComputeAABB(v0, v1, v2);
        }

        return aabbs;
    }
}