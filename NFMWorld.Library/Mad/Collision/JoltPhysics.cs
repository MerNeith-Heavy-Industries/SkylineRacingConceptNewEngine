using JoltPhysicsSharp;
using Microsoft.Extensions.Logging;
using NFMWorldLibrary.FixedMath;
using SysVec3 = System.Numerics.Vector3;

namespace NFMWorldLibrary.Collision;

public static class JoltPhysics
{
    public static class Layers
    {
        public static readonly ObjectLayer NonMoving = 0;
        public static readonly ObjectLayer Moving = 1;
    }

    public static class BroadPhaseLayers
    {
        public static readonly BroadPhaseLayer NonMoving = 0;
        public static readonly BroadPhaseLayer Moving = 1;
    }

    private static readonly SysVec3 UpDir = new(0, -1, 0); // "up" in Y-down space is -Y

    private static readonly List<CollideShapeResult> results = [];

    /// <summary>
    /// Single-pass collision query that classifies each hit as ground or wall using the true
    /// surface normal (via GetWorldSpaceSurfaceNormal), matching CharacterVirtual's approach.
    /// Returns null if no contacts were found.
    /// </summary>
    public static JoltCollisionResult? ResolveCollision(IStage stage, f64Vector3 position, f64Vector3 velocity)
    {
        var physicsSystem = stage.PhysicsSystem;
        using var sphereShape = new SphereShape(75f);
        var collideShapeSettings = new CollideShapeSettings()
        {
            ActiveEdgeMode = ActiveEdgeMode.CollideWithAll,
            BackFaceMode = BackFaceMode.IgnoreBackFaces,
        };

        var posVec3 = new SysVec3((float)position.X, (float)position.Y, (float)position.Z);
        var comTransform = System.Numerics.Matrix4x4.CreateTranslation(posVec3);

        results.Clear();
        var hadHit = physicsSystem.NarrowPhaseQuery.CollideShape(
            shape: sphereShape,
            scale: SysVec3.One,
            centerOfMassTransform: comTransform,
            baseOffset: posVec3,
            settings: collideShapeSettings,
            collectorType: CollisionCollectorType.AllHitSorted,
            results: results
        );

        if (!hadHit) return null;

        var velVec3 = new SysVec3((float)velocity.X, (float)velocity.Y, (float)velocity.Z);
        var bodyLockInterface = physicsSystem.BodyLockInterfaceNoLock;

        // Accumulate ground and wall contacts separately
        var groundResolve = SysVec3.Zero;
        var wallResolve = SysVec3.Zero;
        var wallWeightedNormal = SysVec3.Zero;
        var wallTotalDepth = 0f;
        var bestGroundY = float.MaxValue; // best = lowest Y = highest surface in Y-down
        var bestGroundNormal = SysVec3.Zero;
        var bestGroundness = 0f;
        var hasGround = false;
        var hasWall = false;

        foreach (var hit in results)
        {
            if (hit.PenetrationDepth <= 0) continue;

            var contactNormal = -SysVec3.Normalize(hit.PenetrationAxis);

            // Fetch true surface normal from the body, like Jolt's sFillContactProperties
            var contactPoint = posVec3 + hit.ContactPointOn2;
            var surfaceNormal = contactNormal; // fallback
            bodyLockInterface.LockRead(hit.BodyID2, out var bodyLock);
            if (bodyLock.Succeeded)
            {
                var body = bodyLock.Body!;
                body.GetWorldSpaceSurfaceNormal(hit.SubShapeID2, contactPoint, out var sn);
                surfaceNormal = sn;
                bodyLockInterface.UnlockRead(bodyLock);
            }

            // Jolt: if contact normal points more "up" than surface normal, prefer contact normal
            if (SysVec3.Dot(contactNormal, UpDir) > SysVec3.Dot(surfaceNormal, UpDir))
                surfaceNormal = contactNormal;

            // Classify using surface normal: groundness = dot(surfaceNormal, upDir)
            var groundness = SysVec3.Dot(surfaceNormal, UpDir);

            var v = $"groundness: {groundness}, contact up: {SysVec3.Dot(contactNormal, UpDir)}, surface up: {SysVec3.Dot(surfaceNormal, UpDir)}";
            Logging.Info(v);

            if (groundness > 0.3f)
            {
                // Ground/ramp contact — use contact point Y as the surface position
                hasGround = true;
                if (contactPoint.Y < bestGroundY || groundness > bestGroundness)
                {
                    bestGroundY = contactPoint.Y;
                    bestGroundNormal = surfaceNormal;
                    bestGroundness = groundness;
                }
                // Ground penetration recovery: push along contact normal (mostly vertical)
                var resolve = contactNormal * hit.PenetrationDepth;
                groundResolve += resolve;
            }
            else
            {
                // Wall/cliff contact — horizontal push only
                hasWall = true;
                var normalizedAxis = SysVec3.Normalize(hit.PenetrationAxis);
                var resolve = normalizedAxis * hit.PenetrationDepth;
                wallResolve += -new SysVec3(resolve.X, 0, resolve.Z); // zero Y to prevent climbing

                var wallNormal = new SysVec3(normalizedAxis.X, 0, normalizedAxis.Z);
                wallWeightedNormal += wallNormal * hit.PenetrationDepth;
                wallTotalDepth += hit.PenetrationDepth;
            }
        }

        if (!hasGround && !hasWall) return null;

        // Wall impact component for velocity rebound (horizontal only)
        var wallImpact = f64Vector3.Zero;
        if (hasWall && wallTotalDepth > 0)
        {
            var avgWallNormal = SysVec3.Normalize(wallWeightedNormal);
            var impact = avgWallNormal * SysVec3.Dot(avgWallNormal, velVec3);
            wallImpact = new f64Vector3((fix64)impact.X, fix64.Zero, (fix64)impact.Z);
        }

        return new JoltCollisionResult(
            GroundDelta: new f64Vector3((fix64)groundResolve.X, (fix64)groundResolve.Y, (fix64)groundResolve.Z),
            WallDelta: new f64Vector3((fix64)wallResolve.X, (fix64)wallResolve.Y, (fix64)wallResolve.Z),
            WallImpact: wallImpact,
            HasGround: hasGround,
            HasWall: hasWall,
            GroundSurfaceY: (fix64)bestGroundY,
            GroundNormal: new f64Vector3((fix64)bestGroundNormal.X, (fix64)bestGroundNormal.Y, (fix64)bestGroundNormal.Z),
            Groundness: bestGroundness
        );
    }

    public readonly record struct JoltCollisionResult(
        f64Vector3 GroundDelta,
        f64Vector3 WallDelta,
        f64Vector3 WallImpact,
        bool HasGround,
        bool HasWall,
        fix64 GroundSurfaceY,
        f64Vector3 GroundNormal,
        float Groundness
    );
}