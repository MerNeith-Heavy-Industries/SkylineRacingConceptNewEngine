using MessagePack;
using NFMWorldLibrary.Collision;
using NFMWorldLibrary.FixedMath;

namespace NFMWorldLibrary.Rad;

[MessagePackObject]
[method: SerializationConstructor]
public readonly record struct SrcRad3dCollisionMesh([property: Key(0)] f64Vector3[] Vertices, [property: Key(1)] ushort[] Indices)
{
    [IgnoreMember] public (f64Vector3 min, f64Vector3 max)[] Aabb { get; } = CalculateAabb(Vertices, Indices);

    private static (f64Vector3 min, f64Vector3 max)[] CalculateAabb(f64Vector3[] vertices, ushort[] indices)
    {
        var arr = new (f64Vector3 min, f64Vector3 max)[vertices.Length / 3];
        for (int i = 0; i < vertices.Length; i += 3)
        {
            ref readonly var v0 = ref vertices[i];
            ref readonly var v1 = ref vertices[i + 1];
            ref readonly var v2 = ref vertices[i + 2];

            arr[i / 3] = TriangleMesh.ComputeAABB(v0, v1, v2);
        }
        return arr;
    }
}