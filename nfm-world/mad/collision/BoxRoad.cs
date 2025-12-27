using SoftFloat;

namespace nfm_world.mad.collision;

public struct BoxRoad
{
    private readonly f64Vector3 rad;
    private readonly f64Vector3 trackersPosition;
    private readonly fix64 contoXz;
    private readonly f64Vector3 contoPosition;

    public BoxRoad(f64Vector3 rad, f64Vector3 trackersPosition, fix64 contoXz, f64Vector3 contoPosition) {
        this.rad = rad;
        this.trackersPosition = trackersPosition;
        this.contoXz = contoXz;
        this.contoPosition = contoPosition;
    }

    public enum Collision
    {
        Colliding,
        NotColliding
    }

    public Collision resolveCollision(f64Vector3 position) {
        Vector3 localPosition = (position + (contoPosition * -1))
            .RotateXz(-contoXz)
            .translate(trackersPosition.scale(-1));
        if (fix64.Abs(localPosition.X) > rad.X || Math.abs(localPosition.y) > rad.y || Math.abs(localPosition.z) > rad.z) { // Inside?
            return Collision.NotColliding;
        }

        Vector3 worldBoxPosition = trackersPosition.RotateXz(contoXz)
            .translate(contoPosition);
        if (worldBoxPosition.y == 250 || localPosition.y <= -5) {
            return Collision.NotColliding;
        }

        return Collision.Colliding;
    }
}