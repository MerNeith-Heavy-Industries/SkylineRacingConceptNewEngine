using MessagePack;

namespace NFMWorldLibrary.Rad;

[MessagePackObject]
[method: SerializationConstructor]
public readonly record struct SrcRad3dCollisionMesh([property: Key(0)] Vector3[] Vertices, [property: Key(1)] ushort[] Indices);