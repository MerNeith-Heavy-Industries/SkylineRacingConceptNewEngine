namespace Microsoft.Xna.Framework;

public partial struct Vector4
{
    public static implicit operator System.Numerics.Vector4(Vector4 v)
    {
        return new System.Numerics.Vector4(v.X, v.Y, v.Z, v.W);
    }

    public static implicit operator Vector4(System.Numerics.Vector4 v)
    {
        return new Vector4(v.X, v.Y, v.Z, v.W);
    }
}