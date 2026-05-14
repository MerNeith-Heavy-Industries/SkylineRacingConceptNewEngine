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
        using var sphereShape = new SphereShape(100f);
        var collideShapeSettings = new CollideShapeSettings()
        {
            ActiveEdgeMode = ActiveEdgeMode.CollideWithAll,
            BackFaceMode = BackFaceMode.CollideWithBackFaces,
        };

        var posVec3 = new System.Numerics.Vector3((float)position.X, (float)position.Y, (float)position.Z);
        var comTransform = System.Numerics.Matrix4x4.CreateTranslation(posVec3);
        
        results.Clear();
        var hadHit = physicsSystem.NarrowPhaseQuery.CollideShape(
            shape: sphereShape,
            scale: System.Numerics.Vector3.One,
            centerOfMassTransform: comTransform,
            baseOffset: posVec3,
            settings: collideShapeSettings,
            collectorType: CollisionCollectorType.AllHitSorted,
            results: results
        );

        if (!hadHit) return null;
        Logging.Info("Hit");
        
        var velVec3 = new Vector3((float)velocity.X, (float)velocity.Y, (float)velocity.Z);

        var totalResolve = Vector3.Zero;
        var totalDirection = Vector3.Zero;
        foreach (var hit in results)
        {
            if (hit.PenetrationDepth < 0) continue;
            var resolve = System.Numerics.Vector3.Normalize(hit.PenetrationAxis) * hit.PenetrationDepth;
            totalResolve += -new Vector3(resolve.X, resolve.Y, resolve.Z);

            var normalizedAxis = System.Numerics.Vector3.Normalize(hit.PenetrationAxis);
            var penetration = new Vector3(normalizedAxis.X, normalizedAxis.Y, normalizedAxis.Z);
            totalDirection += penetration;
        }

        totalDirection = Vector3.Normalize(totalDirection);
        var impactComponent = totalDirection * Vector3.Dot(totalDirection, velVec3);

        return new JoltCollision(
            PositionDelta: new f64Vector3((fix64)totalResolve.X, (fix64)totalResolve.Y, (fix64)totalResolve.Z),
            ImpactComponent: new f64Vector3((fix64)impactComponent.X, (fix64)impactComponent.Y, (fix64)impactComponent.Z)
        );
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="PositionDelta">The difference between the input position and the position after the intersections is represented by this vector.</param>
    /// <param name="ImpactComponent">The component of the impact force applied during the collision.</param>
    public readonly record struct JoltCollision(f64Vector3 PositionDelta, f64Vector3 ImpactComponent);
}