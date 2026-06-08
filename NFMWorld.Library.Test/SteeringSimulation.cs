using FixedMathSharp;
using NFMWorldLibrary;
using NFMWorldLibrary.Backend;
using NFMWorldLibrary.Backend.AI;
using NFMWorldLibrary.Collision;
using NFMWorldLibrary.FixedMath;
using NFMWorldLibrary.Gamemodes;
using NFMWorldLibrary.Rad;
using NFMWorldLibrary.Util;

// Match NFMWorld.Library's global using aliases
using fix64 = FixedMathSharp.Fixed64;
using f64Vector3 = FixedMathSharp.Vector3d;

namespace NFMWorld.Library.Test;

/// <summary>
/// Minimal car for physics simulation — implements only what ContO and Mad.Drive actually read.
/// </summary>
class SimCar : IInGameCar
{
    // ITransform
    public f64Vector3 Position { get; set; }
    public f64Euler Rotation { get; set; } = f64Euler.Identity;
    public IReadOnlyList<ITransform> ChildTransforms => [];
    public ITransform? Parent => null;

    // ICar
    public Rad3d Rad => null!;
    public CarStats Stats => CarStats.Default;
    public int GroundAt { get; }
    public int MaxRadius => 60;
    public f64Euler WheelAngle { get; set; }
    public f64Euler TurningWheelAngle { get; set; }
    public IReadOnlyList<Rad3dWheelDef> Wheels { get; }

    // IInGameCar
    public Mad Mad { get; }
    public Control Control { get; } = new Control();
    public ushort currentCheckpoint { get; set; }
    public byte currentLap { get; set; }
    public int totalCheckpoint { get; set; }
    public int lastCheckpointNode { get; set; } = -1;
    public int placement { get; set; }
    public bool Wasted => false;
    public BaseAi? Bot { get; set; }

    public PlayerParameters Player { get; } = new PlayerParameters()
    {
        IsClientPlayer = true,
        CarName = "nfmm/2000tornados",
        Color = new Color3(255, 255, 255),
        IsBot = false,
        PlayerName = "TesTPlayer"
    };

    public event DamageFunc? DamagedX;
    public event RoofDamageFunc? DamagedY;
    public event DamageFunc? DamagedZ;
    public event SparkFunc? Sparked;
    public event DustFunc? Dusted;

    public void AddDust(int wheelidx, float wheelx, float wheely, float wheelz, int scx, int scz, float simag, int tilt, bool onRoof, int wheelGround) { }
    public void Spark(float wheelx, float wheely, float wheelz, float scx, float scy, float scz, int type, int wheelGround) { }
    public void DamageX(CarStats stat, int wheelnum, fix64 amount) { }
    public void DamageY(CarStats stat, int wheelnum, fix64 amount, bool mtouch, int nbsq, int squash) { }
    public void DamageZ(CarStats stat, int wheelnum, fix64 amount) { }
    public void Drive(IStage stage) { }
    public void Collide(IInGameCar otherCar) { }
    public void ResetPosition() { }

    public SimCar()
    {
        // Ground = (int)(Position.Y + 13 * Height / 10) = (int)(70 + 13) = 83
        const int groundAt = 83;
        GroundAt = groundAt;

        // Car starts at World.Ground - GroundAt = 250 - 83 = 167
        Position = new f64Vector3(fix64.Zero, (fix64)(World.Ground - groundAt), fix64.Zero);

        // 4 wheels in local space: front-left, front-right, rear-left, rear-right
        Wheels = [
            new Rad3dWheelDef(new f64Vector3((fix64)(-25), (fix64)70, (fix64)(-45)), 1, (fix64)10, (fix64)10, null),
            new Rad3dWheelDef(new f64Vector3((fix64)(+25), (fix64)70, (fix64)(-45)), 1, (fix64)10, (fix64)10, null),
            new Rad3dWheelDef(new f64Vector3((fix64)(-25), (fix64)70, (fix64)(+45)), 1, (fix64)10, (fix64)10, null),
            new Rad3dWheelDef(new f64Vector3((fix64)(+25), (fix64)70, (fix64)(+45)), 1, (fix64)10, (fix64)10, null),
        ];

        Mad = new Mad(CarStats.Default, 0, false);
    }
}

/// <summary>
/// Stage that returns no collidables — car rolls on the flat World.Ground plane.
/// </summary>
class EmptyStage : IStage
{
    public ReadOnlySpan<CollisionShapeRef> RetrievePointCollidables(fix64 x, fix64 z) =>
        ReadOnlySpan<CollisionShapeRef>.Empty;

    public IReadOnlyList<ITransform> pieces => [];
    public IReadOnlyList<IAiNode> nodes => [];
    public IReadOnlyList<IAiNode> checkpoints => [];
    public IReadOnlyList<IAiNode> fixHoops => [];
    public ushort nlaps => 1;
    public ITransform CreateObject(string objectName, int x, int y, int z, int xz) => null!;
}

[TestClass]
public class SteeringSimulation
{
    /// <summary>
    /// Drives straight for 60 ticks then steers left for 240 ticks.
    /// </summary>
    [TestMethod]
    public void SimulateForwardThenSteerLeft()
    {
        World.IsHyperglidingEnabled = true;

        var stats = CarStats.Default; // Tornado Shark stats
        var car = new SimCar();
        car.Position = new f64Vector3(fix64.Zero, (fix64)(World.Ground - car.GroundAt) - 10000, fix64.Zero);
        car.Rotation = car.Rotation with { Xz = f64AngleSingle.FromDegrees(90) };
        var mad = new Mad(stats, 0, false);
        mad.Reseto(mad.Im, new ContO(car));
        mad.Pxy = 180;
        mad.Pzy = 90;

        // Prevent NullReferenceException in SfxPlaySkid invocation inside Mad.Drive
        mad.SfxPlaySkid += (_, _) => { };
        mad.SfxPlayCrash += (_, _) => { };
        mad.SfxPlayGscrape += (_, _) => { };
        mad.SfxPlayScrape += (_, _) => { };

        var stage = new EmptyStage();

        for (int tick = 0; tick < 50; tick++)
        {
            FrameTrace.ClearMessages();
            
            var control = new Control {};

            mad.Drive(control, new ContO(car), stage);

            Console.WriteLine(FrameTrace.GetMessageString());
        }
    }
}
