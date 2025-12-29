using NFMWorld.Mad.ai;
using SoftFloat;

namespace NFMWorld.Mad;

public class InGameCar : GameObject
{
    public CarInfo ClonedCarInfo;
    public Car CarRef;
    public Mad Mad;
    public Control Control;
    public MadSfx Sfx;
    public ushort currentCheckpoint;
    public byte currentLap; // mad.nlaps
    public int totalCheckpoint; // mad.clear
    public int currentCheckpointNode; // mad.pcleared
    public int closestNode; // mad.point
    public BaseAi? Bot;
    public int placement; // cp.pos

    public CarStats Stats => CarRef.Stats;
    public bool Wasted => Mad.Wasted;

    public InGameCar(InGameCar copy, int im, bool isClientPlayer)
    {
        ClonedCarInfo = copy.CarRef.CarInfo;
        CarRef = new Car(copy.CarRef.CarInfo, new f64Vector3(fix64.Zero, World.Ground - copy.CarRef.GroundAt, fix64.Zero), f64Euler.Identity)
        {
            Parent = this
        };
        Mad = new Mad(copy.Stats, im, isClientPlayer);
        Mad.Reseto(im, CarRef);
        Control = new Control();
        Sfx = new MadSfx(Mad);
    }

    public InGameCar(int im, CarInfo carInfo, int x, int z, bool isClientPlayer)
    {
        ClonedCarInfo = carInfo;
        CarRef = new Car(carInfo, new f64Vector3(x, World.Ground - carInfo.GroundAt, z), f64Euler.Identity)
        {
            Parent = this
        };
        Mad = new Mad(CarRef.Stats, im, isClientPlayer);
        Mad.Reseto(im, CarRef);
        Control = new Control();
        Sfx = new MadSfx(Mad);
    }

    public override void GameTick(Stage? stage = null)
    {
        base.GameTick(stage);
        CarRef.GameTick(stage);
    }

    public void Drive(Stage stage)
    {
        Mad.Drive(Control, CarRef, stage);
        Sfx.Tick(Control, Mad, CarRef.Stats);
    }
    
    public void Collide(InGameCar otherCar)
    {
        Mad.Colide(CarRef, otherCar.Mad, otherCar.CarRef);
    }

    public override void OnBeforeRender()
    {
        base.OnBeforeRender();
        CarRef.OnBeforeRender();
    }

    public override IEnumerable<RenderData> GetRenderData(Lighting? lighting)
    {
        foreach (var renderData in base.GetRenderData(lighting))
        {
            yield return renderData;
        }
        foreach (var renderData in CarRef.GetRenderData(lighting))
        {
            yield return renderData;
        }
    }

    public override void Render(Camera camera, Lighting? lighting)
    {
        base.Render(camera, lighting);
        CarRef.Render(camera, lighting);
    }

    public void ResetPosition()
    {
        Mad.Reseto(Mad.Im, CarRef);
        CarRef.Position = new f64Vector3(fix64.Zero, World.Ground - CarRef.GroundAt, fix64.Zero);
        CarRef.Rotation = f64Euler.Identity;
    }
}