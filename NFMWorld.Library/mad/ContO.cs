using System.Runtime.CompilerServices;
using SoftFloat;

namespace NFMWorld.Mad;

public interface ICar : ITransform
{
    CarStats Stats { get; }
    int GroundAt { get; }
    int MaxRadius { get; }
    bool VisuallyWasted { get; set; }
    f64Euler WheelAngle { get; set; }
    f64Euler TurningWheelAngle { get; set; }
    IReadOnlyList<Rad3dWheelDef> Wheels { get; }
    void AddDust(int wheelidx, float wheelx, float wheely, float wheelz, int scx, int scz, float simag, int tilt, bool onRoof, int wheelGround);
    void Spark(float wheelx, float wheely, float wheelz, float scx, float scy, float scz, int type, int wheelGround);
    void DamageX(CarStats stat, int wheelnum, fix64 amount);
    void DamageY(CarStats stat, int wheelnum, fix64 amount, bool mtouch, int nbsq, int squash);
    void DamageZ(CarStats stat, int wheelnum, fix64 amount);
}

public interface IInGameCar : ICar
{
    public Mad Mad { get; }
    public Control Control { get; }
    public ushort currentCheckpoint { get; set; }
    public byte currentLap { get; set; } // mad.nlaps
    public int totalCheckpoint { get; set; } // mad.clear
    public int lastCheckpointNode { get; set; } // resets on new lap
    public int placement { get; set; } // cp.pos
    public bool Wasted { get; }

    void Drive(IStage stage);
    void Collide(IInGameCar otherCar);
    void ResetPosition();
}

// temp conto for nfmm compatibility
public readonly struct ContO
{
    private readonly ICar _car;
        
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
    public readonly InlineArray4<int> Keyx;
    public readonly InlineArray4<int> Keyz;
    
    public bool Wasted
    {
        get => _car.VisuallyWasted;
        set => _car.VisuallyWasted = value;
    }

    public int Fcnt
    {
        get => 0;
        set { }
    } // TODO car fixed ticks
    public int MaxR => _car.MaxRadius;

    public ContO(ICar car)
    {
        _car = car;

        for (var i = 0; i < 4; i++)
        {
            Keyx[i] = (int)car.Wheels[i].Position.X;
            Keyz[i] = (int)car.Wheels[i].Position.Z;
        }
    }

    public void DamageX(CarStats stat, int wheelnum, fix64 amount)
    {
        _car.DamageX(stat, wheelnum, amount);
    }
    public void DamageY(CarStats stat, int wheelnum, fix64 amount, bool mtouch, int nbsq, int squash)
    {
        _car.DamageY(stat, wheelnum, amount, mtouch, nbsq, squash);
    }
    public void DamageZ(CarStats stat, int wheelnum, fix64 amount)
    {
        _car.DamageZ(stat, wheelnum, amount);
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