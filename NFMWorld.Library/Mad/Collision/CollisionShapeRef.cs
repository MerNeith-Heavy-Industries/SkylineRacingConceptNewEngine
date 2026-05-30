using System.Runtime.InteropServices;
using Maxine.Extensions.UnionGen;
using NFMWorldLibrary.FixedMath;
using NFMWorldLibrary.Rad;

namespace NFMWorldLibrary.Collision;

[UnmanagedUnion(typeof(ShapeRoad), typeof(ShapeWall), typeof(ShapeRamp), typeof(ShapeMesh), typeof(ShapeHull))]
public readonly partial struct CollisionShapeUnion;

// NB: FieldOffset here makes it so the object-containing collisionmesh/collisionhull instances do not overlap in memory with the other collision shape types, which would cause a CLR error.
// The 128 offset is arbitrary, just needs to be large enough to not overlap with the other fields.
[StructLayout(LayoutKind.Explicit)]
public readonly record struct ShapeMesh([field: FieldOffset(0)] f64Vector3 GameObjectPosition, [field: FieldOffset(24)] fix64 GameObjectXz, [field: FieldOffset(128)] SrcRad3dCollisionMesh CollisionMesh);
[StructLayout(LayoutKind.Explicit)]
public readonly record struct ShapeHull([field: FieldOffset(0)] f64Vector3 GameObjectPosition, [field: FieldOffset(24)] fix64 GameObjectXz, [field: FieldOffset(128)] SrcRad3dCollisionHull CollisionHull);

public readonly struct CollisionShapeRef : IQuadObject
{
    public readonly int Index;

    public readonly int Skid;
    public readonly int Damage;
    public readonly bool NotWall;
    public readonly Color3 DustColor;
        
    public readonly CollisionShapeUnion Box;
    
    public f64Bounds Bounds { get; }

    public CollisionShapeRef(
        fix64 gameObjectX,
        fix64 gameObjectY,
        fix64 gameObjectZ,
        fix64 gameObjectRotXz,
        SrcRad3dCollisionMesh colMesh,
        fix64 radius,
        int index)
    {
        Index = index;
        
        Box = new CollisionShapeUnion(new ShapeMesh(new f64Vector3(gameObjectX, gameObjectY, gameObjectZ), gameObjectRotXz, colMesh));
        
        Bounds = new f64Bounds(
            gameObjectX - radius,
            gameObjectZ - radius,
            radius * 2,
            radius * 2
        );
    }

    public CollisionShapeRef(
        fix64 gameObjectX,
        fix64 gameObjectY,
        fix64 gameObjectZ,
        fix64 gameObjectRotXz,
        SrcRad3dCollisionHull colHull,
        fix64 radius,
        int index)
    {
        Index = index;
        
        Box = new CollisionShapeUnion(new ShapeHull(new f64Vector3(gameObjectX, gameObjectY, gameObjectZ), gameObjectRotXz, colHull));
        
        Bounds = new f64Bounds(
            gameObjectX - radius,
            gameObjectZ - radius,
            radius * 2,
            radius * 2
        );
    }

    public CollisionShapeRef(
        fix64 gameObjectX,
        fix64 gameObjectY,
        fix64 gameObjectZ,
        fix64 gameObjectRotXz,
        Rad3dBoxDef box,
        fix64 radius,
        int index)
    {
        Index = index;
        var gameObjectPosition = new f64Vector3(gameObjectX, gameObjectY, gameObjectZ);

        Skid = box.Skid;
        Damage = box.Damage;
        NotWall = box.NotWall;
        DustColor = box.Color;

        var rad = box.Radius;
        var radFlipped = new f64Vector3(rad.Z, rad.Y, rad.X);
        var trackersPosition = box.Translation;

        if (box is { Xy: 0, Zy: 0 })
        {
            Box = new CollisionShapeUnion(new ShapeRoad(rad, trackersPosition, gameObjectRotXz, gameObjectPosition));
        }
        else if (box.Zy == 90 || box.Zy == -90 || box.Xy == 90 || box.Xy == -90)
        {
            if (box.Zy == -90)
            {
                Box = new CollisionShapeUnion(new ShapeWall(rad, 0, trackersPosition, gameObjectRotXz, gameObjectPosition));
            }
            else if (box.Xy == 90)
            {
                Box = new CollisionShapeUnion(new ShapeWall(radFlipped, 90, trackersPosition, gameObjectRotXz, gameObjectPosition));
            }
            else if (box.Zy == 90)
            {
                Box = new CollisionShapeUnion(new ShapeWall(rad, 180, trackersPosition, gameObjectRotXz, gameObjectPosition));
            }
            else
            {
                Box = new CollisionShapeUnion(new ShapeWall(radFlipped, -90, trackersPosition, gameObjectRotXz, gameObjectPosition));
            }
        }
        else if ((box.Zy != 0 && box.Zy != 90 && box.Zy != -90) || (box.Xy != 0 && box.Xy != 90 && box.Xy != -90))
        {
            if (box.Zy != 0)
            {
                Box = new CollisionShapeUnion(new ShapeRamp(rad, box.Zy, 0, trackersPosition, gameObjectRotXz, gameObjectPosition));
            }
            else
            {
                Box = new CollisionShapeUnion(new ShapeRamp(radFlipped, box.Xy, -90, trackersPosition, gameObjectRotXz, gameObjectPosition));
            }
        }

        Bounds = new f64Bounds(
            gameObjectX - radius,
            gameObjectZ - radius,
            radius * 2,
            radius * 2
        );
    }

    public bool TryGetValue(out ShapeMesh collisionMesh) => Box.TryGetValue(out collisionMesh);
    public bool TryGetValue(out ShapeHull collisionHull) => Box.TryGetValue(out collisionHull);
    public bool TryGetValue(out ShapeRoad boxRoad) => Box.TryGetValue(out boxRoad);
    public bool TryGetValue(out ShapeRamp boxRamp) => Box.TryGetValue(out boxRamp);
    public bool TryGetValue(out ShapeWall boxWall) => Box.TryGetValue(out boxWall);
}