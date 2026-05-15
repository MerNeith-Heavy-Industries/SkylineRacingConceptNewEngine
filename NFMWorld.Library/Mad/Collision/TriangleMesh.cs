using System.Runtime.CompilerServices;
using NFMWorldLibrary.FixedMath;

namespace NFMWorldLibrary.Collision;

public static class TriangleMesh
{
    // Groundness threshold: triangles with face normal dot(up) > this are ground/ramp, otherwise wall
    // In Y-down, "up" is -Y, so groundness = -normal.Y
    private const float GroundThreshold = 0.3f; // ~73° from horizontal

    // Tolerance for barycentric point-in-triangle test (prevents falling through edge seams)
    private static readonly fix64 EdgeTolerance = (fix64)(-0.02f);

    // Max distance above a ground triangle to snap (prevents snapping to far-away surfaces)
    private static readonly fix64 MaxGroundSnapDistance = (fix64)100;

    // Max penetration into a wall triangle to resolve
    private static readonly fix64 MaxWallPenetration = (fix64)200;

    public readonly ref struct TriangleData(
        in f64Vector3 edge1, in f64Vector3 edge2, in f64Vector3 normal,
        fix64 lengthSq, fix64 length, float groundness, in f64Vector3 toPoint)
    {
        public readonly ref f64Vector3 Edge1 = ref Unsafe.AsRef(in edge1);
        public readonly ref f64Vector3 Edge2 = ref Unsafe.AsRef(in edge2);
        public readonly ref f64Vector3 Normal = ref Unsafe.AsRef(in normal);
        public readonly fix64 LengthSq = lengthSq;
        public readonly fix64 Length = length;
        public readonly float Groundness = groundness;
        public readonly ref f64Vector3 ToPoint = ref Unsafe.AsRef(in toPoint);
        public f64Vector3 NormalizedNormal => Normal / Length;
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
        // Signed distance from the point to the triangle plane (positive = above in normal direction)
        var distToPlane = f64Vector3.Dot(tri.Normal, tri.ToPoint) / tri.Length;

        // In Y-down with upward-facing normals (-Y), the normal points "up" (negative Y).
        // If the wheel is above the surface, distToPlane is positive (same direction as normal).
        // If the wheel is below the surface, distToPlane is negative.
        // We want the wheel to be slightly above or at the surface — NOT way below it.
        if (distToPlane < (fix64)(-5)) return null; // wheel is way below the surface, skip
        if (distToPlane > MaxGroundSnapDistance) return null; // too far above, skip

        // Barycentric test (using the method that works for arbitrary 3D triangles)
        if (!PointInTriangle(tri.Edge1, tri.Edge2, tri.ToPoint)) return null;

        // Compute the Y at the wheel's XZ position on the triangle plane
        // Plane equation: normal . (P - p0) = 0
        // Solve for Y: normal.X*(x-p0.X) + normal.Y*(y-p0.Y) + normal.Z*(z-p0.Z) = 0
        // y = p0.Y - (normal.X*(x-p0.X) + normal.Z*(z-p0.Z)) / normal.Y
        if (fix64.Abs(tri.Normal.Y) < (fix64)1e-6) return null; // nearly vertical triangle
        var surfaceY = p0.Y - (tri.Normal.X * (position.X - p0.X) + tri.Normal.Z * (position.Z - p0.Z)) / tri.Normal.Y;

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
        var normalizedNormal = tri.NormalizedNormal;

        // Signed distance from point to triangle plane
        var distToPlane = f64Vector3.Dot(normalizedNormal, tri.ToPoint);

        // Only collide if the wheel is behind the front face (penetrating)
        if (distToPlane > (fix64)5) return null; // in front of the wall, no collision
        if (distToPlane < -MaxWallPenetration) return null; // too deep, likely on the other side

        // Check if moving toward the wall (dot of velocity with normal < 0 means approaching)
        var approachSpeed = f64Vector3.Dot(velocity, normalizedNormal);
        if (approachSpeed > 0 && distToPlane > 0) return null; // moving away and not penetrating

        // Project the position onto the plane and test if it's inside the triangle
        var projectedPoint = position - normalizedNormal * distToPlane;
        var toProjected = projectedPoint - p0;
        if (!PointInTriangle(tri.Edge1, tri.Edge2, toProjected)) return null;

        // Penetration depth: push out along the face normal
        var penetration = -distToPlane + (fix64)5; // push slightly past the surface
        if (penetration <= 0) return null;

        // Push direction: along the triangle's face normal, but zero out Y to prevent climbing
        var pushDir = new f64Vector3(normalizedNormal.X, fix64.Zero, normalizedNormal.Z);
        var pushDirLenSq = f64Vector3.Dot(pushDir, pushDir);
        if (pushDirLenSq < (fix64)1e-6) return null; // nearly horizontal normal (ceiling), skip
        pushDir = pushDir / fix64.Sqrt(pushDirLenSq); // normalize horizontal component

        var positionDelta = pushDir * penetration;

        // Velocity impact component: project velocity onto push direction
        var impactComponent = pushDir * f64Vector3.Dot(pushDir, velocity);

        return new WallCollision(positionDelta, impactComponent);
    }

    /// <summary>
    /// Barycentric point-in-triangle test for a 3D point projected onto the triangle plane.
    /// Uses the pre-computed edge vectors and the vector from p0 to the test point.
    /// Includes edge tolerance to prevent gaps at triangle seams.
    /// </summary>
    private static bool PointInTriangle(in f64Vector3 edge1, in f64Vector3 edge2, in f64Vector3 toPoint)
    {
        // Compute barycentric coordinates using the dot-product method
        var d11 = f64Vector3.Dot(edge1, edge1);
        var d12 = f64Vector3.Dot(edge1, edge2);
        var d22 = f64Vector3.Dot(edge2, edge2);
        var d1p = f64Vector3.Dot(edge1, toPoint);
        var d2p = f64Vector3.Dot(edge2, toPoint);

        var denom = d11 * d22 - d12 * d12;
        if (fix64.Abs(denom) < (fix64)1e-9) return false; // degenerate

        var u = (d22 * d1p - d12 * d2p) / denom; // barycentric coord for edge1
        var v = (d11 * d2p - d12 * d1p) / denom; // barycentric coord for edge2

        // Point is inside if u >= 0, v >= 0, u + v <= 1 (with tolerance for edge seams)
        return u >= EdgeTolerance && v >= EdgeTolerance && (u + v) <= (fix64)1 - EdgeTolerance;
    }
}
