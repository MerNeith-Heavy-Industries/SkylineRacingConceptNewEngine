using SoftFloat;

namespace nfm_world.mad.collision;

public readonly struct CollisionBox(
    f64Vector3 rad,
    f64Vector3 trackersPosition,
    fix64 contoXz,
    f64Vector3 contoPosition)
{
    public readonly struct Collision;

    public Collision? ResolveCollision(f64Vector3 position) {
        f64Vector3 localPosition = ((position + (contoPosition * -1))
            .RotateXz(-contoXz)) + (trackersPosition * -1);
        if (fix64.Abs(localPosition.X) > rad.X || fix64.Abs(localPosition.Y) > rad.Y || fix64.Abs(localPosition.Z) > rad.Z) { // Inside?
            return null;
        }

        return new Collision();
    }
}