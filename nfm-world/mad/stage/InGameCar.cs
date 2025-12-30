using NFMWorld.Mad.ai;
using SoftFloat;

namespace NFMWorld.Mad;

public class InGameCar : Car
{
    public CarInfo ClonedCarInfo;
    public Mad Mad;
    public Control Control;
    public MadSfx Sfx;
    public ushort currentCheckpoint;
    public byte currentLap; // mad.nlaps
    public int totalCheckpoint; // mad.clear
    public int lastCheckpointNode = -1; // resets on new lap
    public BaseAi? Bot;
    public int placement; // cp.pos
    
    public bool Wasted => Mad.Wasted;

    public InGameCar(InGameCar copy, int im, bool isClientPlayer)
        : base(copy.CarInfo, new f64Vector3(fix64.Zero, World.Ground - copy.GroundAt, fix64.Zero), f64Euler.Identity)
    {
        ClonedCarInfo = copy.CarInfo;
        Mad = new Mad(copy.Stats, im, isClientPlayer);
        Mad.Reseto(im, this);
        Control = new Control();
        Sfx = new MadSfx(Mad);
    }

    public InGameCar(int im, CarInfo carInfo, int x, int z, bool isClientPlayer)
        : base(carInfo, new f64Vector3(x, World.Ground - carInfo.GroundAt, z), f64Euler.Identity)
    {
        ClonedCarInfo = carInfo;
        Mad = new Mad(Stats, im, isClientPlayer);
        Mad.Reseto(im, this);
        Control = new Control();
        Sfx = new MadSfx(Mad);
    }

    public void Drive(Stage stage)
    {
        Mad.Drive(Control, this, stage);
        Sfx.Tick(Control, Mad, Stats);
    }
    
    public void Collide(InGameCar otherCar)
    {
        Mad.Colide(this, otherCar.Mad, otherCar);
    }

    public void ResetPosition()
    {
        Mad.Reseto(Mad.Im, this);
        Position = new f64Vector3(fix64.Zero, World.Ground - GroundAt, fix64.Zero);
        Rotation = f64Euler.Identity;
    }
}