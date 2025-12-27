using SoftFloat;

namespace nfm_world.mad.collision;

public readonly struct BoxRamp {
    private readonly f64Vector3 rad;
    private readonly fix64 trackersZy;
    private readonly fix64 trackersXz;
    private readonly f64Vector3 trackersPosition;
    private readonly fix64 contoXz;
    private readonly f64Vector3 contoPosition;

    public BoxRamp(f64Vector3 rad, fix64 trackersZy, fix64 trackersXz, f64Vector3 trackersPosition, fix64 contoXz, f64Vector3 contoPosition) {
        this.rad = rad;
        this.trackersZy = trackersZy;
        this.trackersXz = trackersXz;
        this.trackersPosition = trackersPosition;
        this.contoXz = contoXz;
        this.contoPosition = contoPosition;
    }

    public readonly struct Collision(fix64 zTmp, f64Vector3 newPosition)
    {
        public readonly fix64 zTmp = zTmp;
        public readonly f64Vector3 newPosition = newPosition;
    }

    public Collision? ResolveCollision(f64Vector3 position) {
        fix64 zyPlus90 = trackersZy + 90;
        f64Vector3 localPosition = ((position + (contoPosition * -1)).RotateXz(-contoXz) +
                                    (trackersPosition * -1)).RotateXz(-trackersXz);
        if (fix64.Abs(localPosition.X) > rad.X || fix64.Abs(localPosition.Y) > rad.Y || fix64.Abs(localPosition.Z) > rad.Z) { // Inside?
            return null;
        }
        localPosition = localPosition.RotateZy(zyPlus90); // technically this should be done before the check but og NFM does it like this

        fix64 zTmp = localPosition.Z;
        if (localPosition.Z > 0 && localPosition.Z < 200) {
            localPosition.Z = 0;
        }
        f64Vector3 newPosition = (localPosition.RotateZy(-zyPlus90).RotateXz(trackersXz) + trackersPosition).RotateXz(contoXz) + contoPosition;
        return new Collision(zTmp, newPosition);
    }
}