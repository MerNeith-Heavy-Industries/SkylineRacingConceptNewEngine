using NFMWorld.Mad;
using SoftFloat;

namespace nfm_world.mad.collision;

public readonly struct BoxRoad
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

    public readonly struct Collision;

    public Collision? ResolveCollision(f64Vector3 position) {
        var localPosition = (position + (contoPosition * -1)).RotateXz(-contoXz) + (trackersPosition * -1);
        if (fix64.Abs(localPosition.X) > rad.X || fix64.Abs(localPosition.Y) > rad.Y || fix64.Abs(localPosition.Z) > rad.Z) { // Inside?
            return null;
        }

        var worldBoxPosition = trackersPosition.RotateXz(contoXz) + contoPosition;
        if (worldBoxPosition.Y == World.Ground || localPosition.Y <= -5) {
            return null;
        }

        return new Collision();
    }
}