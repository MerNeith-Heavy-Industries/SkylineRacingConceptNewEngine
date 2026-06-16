using NFMWorldLibrary.Backend.AI;
using NFMWorldLibrary.Backend.Gamemodes;
using NFMWorldLibrary.FixedMath;
using NFMWorldLibrary.Gamemodes;
using NFMWorldLibrary.Rad;

namespace NFMWorldLibrary.Backend;

public class BackendCar : BackendGameObject, IInGameCar
{
    public int GroundAt { get; }
    public int MaxRadius { get; }
    public f64Euler WheelAngle { get; set; }
    public f64Euler TurningWheelAngle { get; set; }
    public IReadOnlyList<Rad3dWheelDef> Wheels { get; }

    public CarPhysics CarPhysics { get; }
    public Control Control { get; }
    public ushort currentCheckpoint { get; set; }
    public byte currentLap { get; set; } // mad.nlaps
    public int totalCheckpoint { get; set; } // mad.clear
    public int lastCheckpointNode { get; set; } = -1; // resets on new lap
    public int placement { get; set; } // cp.pos
    public Rad3d Rad { get; }
    public CarStats Stats { get; }
    public bool Wasted => CarPhysics.Wasted;

    public BaseAi? Bot { get; set; }

    public event DamageFunc? DamagedX;
    public event RoofDamageFunc? DamagedY;
    public event DamageFunc? DamagedZ;
    public event SparkFunc? Sparked;
    public event DustFunc? Dusted;
    
    public PlayerParameters Player { get; }

    public BackendCar(
        IInGameCar other,
        int im,
        bool isClientPlayer
    ) : this(
        other.Rad,
        im,
        other.Position.X,
        other.Position.Z,
        isClientPlayer
    )
    {
    }

    public BackendCar(PlayerParameters player, int im, fix64 x, fix64 z) : this(BackendGameSparker.GetCar(player.CarName).Rad!, im, x, z, player.IsClientPlayer)
    {
        Player = player;
    }

    public BackendCar(Rad3d rad, int im, fix64 x, fix64 z, bool isClientPlayer)
    {
        Rad = rad;
        Stats = CarStats.ValidateStats(rad.Stats, "hogan rewish");

        GroundAt = rad.Wheels.FirstOrDefault().Ground;
        MaxRadius = rad.MaxRadius;
        Wheels = rad.Wheels;
        
        CarPhysics = new CarPhysics(Stats, im, isClientPlayer);
        CarPhysics.Reseto(CarPhysics.Im, this);
        Control = new Control();
        
        Position = new f64Vector3(x, World.Ground - GroundAt, z);
        Rotation = f64Euler.Identity;
        
        Player = new PlayerParameters
        {
            CarName = rad.FileName,
            IsClientPlayer = false,
            Color = new Color3(255, 255, 255),
            IsBot = false,
            PlayerName = "hogan rewish"
        };
    }

    public void Drive(IStage stage)
    {
        var transaction = SentrySdk.StartTransaction("BackendCar.Drive", "drive-car");
        CarPhysics.Drive(Control, this, stage);
        transaction.Finish();
    }
    
    public void Collide(IInGameCar otherCar)
    {
        var transaction = SentrySdk.StartTransaction("BackendCar.Collide", "car-collide");
        CarPhysics.Collide(this, otherCar.CarPhysics, new ContO(otherCar));
        transaction.Finish();
    }

    public void ResetPosition()
    {
        CarPhysics.Reseto(CarPhysics.Im, this);
        Position = new f64Vector3(fix64.Zero, World.Ground - GroundAt, fix64.Zero);
        Rotation = f64Euler.Identity;
    }

    public void AddDust(int wheelidx, float wheelx, float wheely, float wheelz, int scx, int scz, float simag, int tilt,
        bool onRoof, int wheelGround)
    {
        Dusted?.Invoke(wheelidx, wheelx, wheely, wheelz, scx, scz, simag, tilt, onRoof, wheelGround);
    }

    public void Spark(float wheelx, float wheely, float wheelz, float scx, float scy, float scz, int type, int wheelGround)
    {
        Sparked?.Invoke(wheelx, wheely, wheelz, scx, scy, scz, type, wheelGround);
    }

    public void DamageX(CarStats stat, int wheelnum, fix64 amount)
    {
        DamagedX?.Invoke(stat, wheelnum, amount);
    }

    public void DamageY(CarStats stat, int wheelnum, fix64 amount, bool mtouch, int nbsq, int squash)
    {
        DamagedY?.Invoke(stat, wheelnum, amount, mtouch, nbsq, squash);
    }

    public void DamageZ(CarStats stat, int wheelnum, fix64 amount)
    {
        DamagedZ?.Invoke(stat, wheelnum, amount);
    }
    
    public static implicit operator ContO(BackendCar car) => new(car);
}

public delegate void DamageFunc(CarStats stat, int wheelnum, fix64 amount);
public delegate void RoofDamageFunc(CarStats stat, int wheelnum, fix64 amount, bool mtouch, int nbsq, int squash);
public delegate void SparkFunc(float wheelx, float wheely, float wheelz, float scx, float scy, float scz, int type, int wheelGround);
public delegate void DustFunc(int wheelidx, float wheelx, float wheely, float wheelz, int scx, int scz, float simag, int tilt, bool onRoof, int wheelGround);