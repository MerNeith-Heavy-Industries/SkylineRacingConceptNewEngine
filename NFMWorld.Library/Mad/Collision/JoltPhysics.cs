using JoltPhysicsSharp;
using NFMWorldLibrary.FixedMath;

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

    private static readonly List<CollideShapeResult> results = [];
    public static JoltCollision? ResolveCollision(IStage stage, f64Vector3 position, f64Vector3 velocity)
    {
        
        var physicsSystem = stage.PhysicsSystem;
        using var sphereShape = new SphereShape(75f);
        var collideShapeSettings = new CollideShapeSettings()
        {
            ActiveEdgeMode = ActiveEdgeMode.CollideWithAll,
            BackFaceMode = BackFaceMode.IgnoreBackFaces,
        };

        var posVec3 = new System.Numerics.Vector3((float)position.X, (float)position.Y, (float)position.Z);
        var comTransform = System.Numerics.Matrix4x4.CreateTranslation(posVec3);
        
        results.Clear();
        var hadHit = physicsSystem.NarrowPhaseQuery.CollideShape( // TODO add bindings for CollideShapeWithInternalEdgeRemoval
            shape: sphereShape,
            scale: System.Numerics.Vector3.One,
            centerOfMassTransform: comTransform,
            baseOffset: posVec3,
            settings: collideShapeSettings,
            collectorType: CollisionCollectorType.AllHitSorted,
            results: results
        );

        if (!hadHit) return null;
        
        var velVec3 = new Vector3((float)velocity.X, (float)velocity.Y, (float)velocity.Z);

        var totalResolve = Vector3.Zero;
        var weightedNormal = Vector3.Zero;
        var totalDepth = 0f;
        foreach (var hit in results)
        {
            if (hit.PenetrationDepth < 0) continue;
            var normalizedAxis = System.Numerics.Vector3.Normalize(hit.PenetrationAxis);
            var resolve = normalizedAxis * hit.PenetrationDepth;
            totalResolve += -new Vector3(resolve.X, resolve.Y, resolve.Z);

            var normal = new Vector3(normalizedAxis.X, normalizedAxis.Y, normalizedAxis.Z);
            weightedNormal += normal * hit.PenetrationDepth;
            totalDepth += hit.PenetrationDepth;
        }

        if (totalDepth <= 0) return null;

        var avgNormal = Vector3.Normalize(weightedNormal);
        // In Y-down, "up" is -Y. Dot with -Y gives how ground-like the surface is.
        // 1.0 = flat ground, 0.0 = vertical wall, -1.0 = ceiling
        var groundness = Vector3.Dot(avgNormal, new Vector3(0, -1, 0));
        const float groundThreshold = 0.5f; // ~60° from horizontal ground
        var isGround = groundness > groundThreshold;
        
        
        Logging.Info($"Hit {groundness}");

        var impactComponent = avgNormal * Vector3.Dot(avgNormal, velVec3);

        if (!isGround)
        {
            // Wall/cliff: zero out the vertical resolve to prevent climbing
            totalResolve = new Vector3(totalResolve.X, 0, totalResolve.Z);
            impactComponent = new Vector3(impactComponent.X, 0, impactComponent.Z);
        }

        return new JoltCollision(
            PositionDelta: new f64Vector3((fix64)totalResolve.X, (fix64)totalResolve.Y, (fix64)totalResolve.Z),
            ImpactComponent: new f64Vector3((fix64)impactComponent.X, (fix64)impactComponent.Y, (fix64)impactComponent.Z),
            IsGround: isGround,
            Groundness: groundness
        );
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="PositionDelta">The difference between the input position and the position after the intersections is represented by this vector.</param>
    /// <param name="ImpactComponent">The component of the impact force applied during the collision.</param>
    /// <param name="IsGround">True if the average collision surface is ground-like (within ~60° of horizontal).</param>
    /// <param name="Groundness">How ground-like the surface is: 1.0 = flat ground, 0.0 = vertical wall, -1.0 = ceiling.</param>
    public readonly record struct JoltCollision(f64Vector3 PositionDelta, f64Vector3 ImpactComponent, bool IsGround, float Groundness);

    /// <summary>
    /// Casts a ray downward (+Y in Y-down space) from the given position to find the ground/ramp surface.
    /// Returns the Y position of the surface hit and the surface normal, or null if no ground found.
    /// </summary>
    public static JoltGroundHit? RaycastGround(IStage stage, f64Vector3 position, float maxDistance = 500f)
    {
        var physicsSystem = stage.PhysicsSystem;

        // Start the ray above the wheel position to avoid starting inside a mesh surface.
        // In Y-down, "above" is -Y.
        const float upwardOffset = 100f;
        var origin = new System.Numerics.Vector3((float)position.X, (float)position.Y - upwardOffset, (float)position.Z);
        // Cast downward (+Y in Y-down coordinate system) for the full range
        var direction = new System.Numerics.Vector3(0, upwardOffset + maxDistance, 0);

        var ray = new Ray(origin, direction);

        if (!physicsSystem.NarrowPhaseQuery.CastRay(ray, out var hit))
            return null;

        // hit.Fraction is 0..1 along the ray direction
        var hitPoint = origin + direction * hit.Fraction;
        
        Logging.Info($"RaycastGround: origin.Y={origin.Y}, dir.Y={direction.Y}, fraction={hit.Fraction}, hitPoint.Y={hitPoint.Y}, posY={(float)position.Y}");

        // Get the surface normal via body lock
        var bodyLockInterface = physicsSystem.BodyLockInterfaceNoLock;
        bodyLockInterface.LockRead(hit.BodyID, out var bodyLock);
        if (!bodyLock.Succeeded)
            return null;

        var body = bodyLock.Body!;
        body.GetWorldSpaceSurfaceNormal(new SubShapeID(hit.subShapeID2), hitPoint, out var hitNormal);
        bodyLockInterface.UnlockRead(bodyLock);

        var normal = new Vector3(hitNormal.X, hitNormal.Y, hitNormal.Z);

        // groundness: dot with -Y (up direction in Y-down space)
        var groundness = -normal.Y;

        return new JoltGroundHit(
            SurfaceY: (fix64)hitPoint.Y,
            Normal: new f64Vector3((fix64)normal.X, (fix64)normal.Y, (fix64)normal.Z),
            Groundness: groundness,
            IsGround: groundness > 0.3f
        );
    }

    /// <param name="SurfaceY">The Y position of the ground/ramp surface hit.</param>
    /// <param name="Normal">The surface normal at the hit point.</param>
    /// <param name="Groundness">How ground-like the surface is: 1.0 = flat ground, 0.0 = vertical wall.</param>
    /// <param name="IsGround">True if the surface is ground-like (within ~60° of horizontal).</param>
    public readonly record struct JoltGroundHit(fix64 SurfaceY, f64Vector3 Normal, float Groundness, bool IsGround);
}