using SoftFloat;

namespace nfm_world.mad.collision;

public static class CollisionExtensions
{
    extension(f64Vector3 vec)
    {
        public f64Vector3 RotateZy(fix64 zy) {
            var a = zy * fix64.DegToRad;
            return new f64Vector3(
                vec.X,
                vec.Y * fix64.Cos(a) + vec.Z * -fix64.Sin(a),
                vec.Y * fix64.Sin(a) + vec.Z * fix64.Cos(a)
            );
        }

        public f64Vector3 RotateXz(fix64 xz) {
            var a = -xz * fix64.DegToRad;
            return new f64Vector3(
                vec.X * fix64.Cos(a) + vec.Z * fix64.Sin(a),
                vec.Y,
                vec.X * -fix64.Sin(a) + vec.Z * fix64.Cos(a)
            );
        }

        public (fix64 x1, fix64 x2) coefficients(f64Vector3 v1, f64Vector3 v2) {
            // A^T A terms
            var a11 = f64Vector3.Dot(v1, v1);
            var a12 = v1.dot(v2);
            var a22 = v2.dot(v2);

            // A^T b terms
            var b1 = v1.dot(this);
            var b2 = v2.dot(this);

            var det = a11 * a22 - a12 * a12;
            if (Math.abs(det) < 1e-9) {
                throw new IllegalArgumentException("v1 and v2 are linearly dependent");
            }

            var x1 = ( a22 * b1 - a12 * b2) / det;
            var x2 = (-a12 * b1 + a11 * b2) / det;

            return (x1, x2);
        }
    }
}