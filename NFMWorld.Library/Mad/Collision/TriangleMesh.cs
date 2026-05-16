using System.Runtime.CompilerServices;
using FixedMathSharp;
using NFMWorldLibrary.FixedMath;

namespace NFMWorldLibrary.Collision;

public static class TriangleMesh
{
    // Groundness threshold: triangles with face normal dot(up) > this are ground/ramp, otherwise wall
    // In Y-down, "up" is -Y, so groundness = -normal.Y
    private static readonly fix64 GroundThreshold = (fix64)0.3f; // ~73° from horizontal

    // Tolerance for barycentric point-in-triangle test (prevents falling through edge seams)
    private static readonly Fixed128 EdgeTolerance = (Fixed128)(-0.04f);

    // Max distance above a ground triangle to snap (prevents snapping to far-away surfaces)
    private static readonly fix64 MaxGroundSnapDistance = (fix64)100;

    // Max penetration into a wall triangle to resolve
    private static readonly fix64 MaxWallPenetration = (fix64)50; // increasing this prevents phasing through walls at high speed, but may lead to being shot out the side of a ramp

    public readonly ref struct TriangleData(
        in f64Vector3 edge1, in f64Vector3 edge2, in f64Vector3 normalizedNormal,
        fix64 groundness, in f64Vector3 toPoint)
    {
        public readonly ref f64Vector3 Edge1 = ref Unsafe.AsRef(in edge1);
        public readonly ref f64Vector3 Edge2 = ref Unsafe.AsRef(in edge2);
        public readonly ref f64Vector3 NormalizedNormal = ref Unsafe.AsRef(in normalizedNormal);
        public readonly fix64 Groundness = groundness;
        public readonly ref f64Vector3 ToPoint = ref Unsafe.AsRef(in toPoint);
        public bool IsGround => Groundness > GroundThreshold;
    }

    public readonly struct GroundCollision(fix64 newY)
    {
        public readonly fix64 newY = newY;
    }

    public readonly struct WallCollision(f64Vector3 positionDelta, f64Vector3 impactComponent)
    {
        public readonly f64Vector3 positionDelta = positionDelta;
        public readonly f64Vector3 impactComponent = impactComponent;
    }

    /// <summary>
    /// Test a wheel point against a single world-space triangle.
    /// Returns a ground snap or wall push, depending on face normal steepness.
    /// </summary>
    public static GroundCollision? ResolveGround(
        in f64Vector3 p0, in f64Vector3 p1, in f64Vector3 p2,
        in f64Vector3 position, in TriangleData tri)
    {
        if (fix64.Abs(tri.NormalizedNormal.Y) < (fix64)1e-6) return null; // nearly vertical triangle

        // Barycentric test: is the wheel's XZ within the triangle?
        if (!PointInTriangle(tri.Edge1, tri.Edge2, tri.ToPoint)) return null;

        // Compute the Y height on the triangle plane at the wheel's XZ position
        // Plane equation: n . (P - p0) = 0  =>  y = p0.Y - (n.X*(x-p0.X) + n.Z*(z-p0.Z)) / n.Y
        var surfaceY = p0.Y - (tri.NormalizedNormal.X * (position.X - p0.X) + tri.NormalizedNormal.Z * (position.Z - p0.Z)) / tri.NormalizedNormal.Y;

        // In Y-down: smaller Y = higher up. The surface should be at or above the wheel.
        // surfaceY <= position.Y means the surface is at or above the wheel (snap up onto it).
        // Allow a small tolerance below (wheel slightly above the surface) to maintain contact.
        if (surfaceY > position.Y + (fix64)5) return null; // surface is below the wheel, skip
        if (position.Y - surfaceY > MaxGroundSnapDistance) return null; // surface is way too far above, skip

        return new GroundCollision(surfaceY);
    }

    /// <summary>
    /// Test a wheel point against a single world-space triangle for wall collision.
    /// Returns a push-back delta and velocity impact component if the wheel is inside the triangle's volume.
    /// </summary>
    public static WallCollision? ResolveWall(
        in f64Vector3 p0, in f64Vector3 p1, in f64Vector3 p2,
        in f64Vector3 position, in f64Vector3 velocity, in TriangleData tri)
    {
        // Signed distance from point to triangle plane
        var distToPlane = f64Vector3.Dot(tri.NormalizedNormal, tri.ToPoint);

        // Only collide if the wheel is behind the front face (penetrating)
        if (distToPlane > (fix64)30) return null; // in front of the wall, no collision
        if (distToPlane < -MaxWallPenetration) return null; // too deep, likely on the other side

        // // Check if moving toward the wall (dot of velocity with normal < 0 means approaching)
        // var approachSpeed = f64Vector3.Dot(velocity, tri.NormalizedNormal);
        // if (approachSpeed > 0 && distToPlane > 0) return null; // moving away and not penetrating

        // Project the position onto the plane and test if it's inside the triangle
        var projectedPoint = position - tri.NormalizedNormal * distToPlane;
        var toProjected = projectedPoint - p0;
        if (!PointInTriangle(tri.Edge1, tri.Edge2, toProjected)) return null;

        // Penetration depth: push out along the face normal
        var penetration = -distToPlane + (fix64)30; // push slightly past the surface
        if (penetration <= 0) return null;

        // Push direction: along the triangle's face normal, but zero out Y to prevent climbing
        var pushDir = new f64Vector3(tri.NormalizedNormal.X, fix64.Zero, tri.NormalizedNormal.Z);
        var pushDirLenSq = f64Vector3.Dot(pushDir, pushDir);
        if (pushDirLenSq < (fix64)1e-6) return null; // nearly horizontal normal (ceiling), skip
        pushDir = pushDir / fix64.Sqrt(pushDirLenSq); // normalize horizontal component

        var positionDelta = pushDir * penetration;

        // Velocity impact component: project velocity onto push direction
        var impactComponent = pushDir * f64Vector3.Dot(pushDir, velocity);

        return new WallCollision(positionDelta, impactComponent);
    }

    /// <summary>
    /// Barycentric point-in-triangle test using float to avoid fix64 overflow.
    /// Edge vectors of ~2000 units produce dot products of ~4M; their products (~16T) overflow fix64.
    /// </summary>
    private static bool PointInTriangle(in f64Vector3 edge1, in f64Vector3 edge2, in f64Vector3 toPoint)
    {
        var d11 = f64Vector3.Dot128(edge1, edge1);
        var d12 = f64Vector3.Dot128(edge1, edge2);
        var d22 = f64Vector3.Dot128(edge2, edge2);
        var d1p = f64Vector3.Dot128(edge1, toPoint);
        var d2p = f64Vector3.Dot128(edge2, toPoint);

        var denom = d11 * d22 - d12 * d12;
        if (denom.Abs() < (Fixed128)1e-6f) return false; // degenerate

        var u = (d22 * d1p - d12 * d2p) / denom;
        var v = (d11 * d2p - d12 * d1p) / denom;

        return u >= EdgeTolerance && v >= EdgeTolerance && (u + v) <= 1 - EdgeTolerance;
    }

    public static string DebugPointInTriangle(in f64Vector3 edge1, in f64Vector3 edge2, in f64Vector3 toPoint)
    {
        var d11 = f64Vector3.Dot128(edge1, edge1);
        var d12 = f64Vector3.Dot128(edge1, edge2);
        var d22 = f64Vector3.Dot128(edge2, edge2);
        var d1p = f64Vector3.Dot128(edge1, toPoint);
        var d2p = f64Vector3.Dot128(edge2, toPoint);

        var denom = d11 * d22 - d12 * d12;
        if (denom.Abs() < (Fixed128)1e-6f) return "degen";

        var u = (d22 * d1p - d12 * d2p) / denom;
        var v = (d11 * d2p - d12 * d1p) / denom;

        var inside = u >= EdgeTolerance && v >= EdgeTolerance && (u + v) <= 1 - EdgeTolerance;
        return $"{inside} u={u:F3} v={v:F3}";
    }
}
