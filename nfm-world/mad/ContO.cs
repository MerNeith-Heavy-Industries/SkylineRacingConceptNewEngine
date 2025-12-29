using SoftFloat;

namespace NFMWorld.Mad;

// temp conto for nfmm compatibility
public class ContO
{
    private readonly Car _car;
        
    public fix64 X 
    {
        get => (fix64) _car.Position.X;
        set => _car.Position = _car.Position with { X = value };
    }
    public fix64 Y 
    {
        get => (fix64) _car.Position.Y;
        set => _car.Position = _car.Position with { Y = value };
    }
    public fix64 Z 
    {
        get => (fix64) _car.Position.Z;
        set => _car.Position = _car.Position with { Z = value };
    }
    public fix64 Xz 
    {
        get => _car.Rotation.Xz.Degrees;
        set => _car.Rotation = _car.Rotation with { Xz = f64AngleSingle.FromDegrees(value) };
    }
    public fix64 Xy 
    {
        get => _car.Rotation.Xy.Degrees;
        set => _car.Rotation = _car.Rotation with { Xy = f64AngleSingle.FromDegrees(value) };
    }
    public fix64 Zy 
    {
        get => _car.Rotation.Zy.Degrees;
        set => _car.Rotation = _car.Rotation with { Zy = f64AngleSingle.FromDegrees(value) };
    }

    public int Grat => _car.GroundAt;
    
    // wheel rotation
    public fix64 Wzy
    {
        get => _car.TurningWheelAngle.Zy.Degrees;
        set
        {
            _car.TurningWheelAngle = _car.TurningWheelAngle with { Zy = f64AngleSingle.FromDegrees(value) };
            _car.WheelAngle = _car.WheelAngle with { Zy = f64AngleSingle.FromDegrees(value) };
        }
    }

    public fix64 Wxz
    {
        get => _car.TurningWheelAngle.Xz.Degrees;
        set => _car.TurningWheelAngle = _car.TurningWheelAngle with { Xz = f64AngleSingle.FromDegrees(value) };
    }
    
    // wheel position
    public int[] Keyx { get; }
    public int[] Keyz { get; }
    
    public bool Wasted
    {
        get => _car.Wasted;
        set => _car.Wasted = value;
    }
    
    public int Fcnt { get; set; } // TODO car fixed ticks
    public int MaxR => _car.MaxRadius;

    public ContO(Car car)
    {
        _car = car;

        Keyx = Array.ConvertAll(car.Wheels, static e => (int)e.Position.X);
        Keyz = Array.ConvertAll(car.Wheels, static e => (int)e.Position.Z);
    }

    public static implicit operator ContO(Car car) => new ContO(car);

    public void DamageX(CarStats stat, int wheelnum, fix64 amount)
    {
        MeshDamage.DamageX(stat, _car, wheelnum, (float)amount);
    }
    public void DamageY(CarStats stat, int wheelnum, fix64 amount, bool mtouch, int nbsq, int squash)
    {
        MeshDamage.DamageY(stat, _car, wheelnum, (float)amount, mtouch, ref nbsq, ref squash);
    }
    public void DamageZ(CarStats stat, int wheelnum, fix64 amount)
    {
        MeshDamage.DamageZ(stat, _car, wheelnum, (float)amount);
    }

    public void Dust(int wheelidx, fix64 wheelx, fix64 wheely, fix64 wheelz, int scx, int scz, fix64 simag, int tilt, bool onRoof, int wheelGround)
    {
        _car.AddDust(wheelidx, (float)wheelx, (float)wheely, (float)wheelz, scx, scz, (float)simag, tilt, onRoof, wheelGround);
	}

    public void Spark(fix64 wheelx, fix64 wheely, fix64 wheelz, fix64 scx, fix64 scy, fix64 scz, int type, int wheelGround)
    {
        _car.Spark((float)wheelx, (float)wheely, (float)wheelz, (float)scx, (float)scy, (float)scz, type, wheelGround);
    }
}