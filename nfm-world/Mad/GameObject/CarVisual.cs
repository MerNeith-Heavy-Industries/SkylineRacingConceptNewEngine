using Microsoft.Xna.Framework.Graphics;
using NFMWorld.Sfx;
using NFMWorldLibrary;
using NFMWorldLibrary.Backend;
using NFMWorldLibrary.FixedMath;
using NFMWorldLibrary.Rad;

namespace NFMWorld;

/// <summary>
/// Client-side visual representation of an <see cref="IInGameCar"/>.
/// Reads position/rotation from the backend car each tick — does NOT store its own game state.
/// Owns rendering effects (flames, dust, chips, sparks), wheel meshes, and SFX.
/// </summary>
public class CarVisual : MeshedGameObject, IDisposable
{
    private readonly IInGameCar _car;

    /// <summary>
    /// Visual properties that gamemodes can modify via <see cref="IClientCarCallbacks"/>.
    /// </summary>
    public CarVisualProperties Visuals { get; } = new();

    // Stores "brokenness" phase for damageable meshes
    public readonly float[] Bfase;

    internal readonly Flames Flames;
    internal readonly Dust Dust;
    internal readonly Chips Chips;
    internal readonly Sparks Sparks;
    private readonly MeshedGameObject[] _wheels;

    public string FileName => Mesh.FileName;

    public bool VisuallyWasted { get; set; }

    public MadSfx? Sfx;

    public CarVisual(GraphicsDevice graphicsDevice, IInGameCar car)
        : base(new CarMesh(graphicsDevice, car.Rad))
    {
        Bfase = new float[Mesh.Polys.Length];

        _car = car;
        _wheels = car.Wheels
            .Select(wheel => new WheelMeshBuilder(wheel, car.Rad.Rims).BuildGameObject(graphicsDevice, this))
            .ToArray();
        Flames = new Flames(this, graphicsDevice);
        Dust = new Dust(this, graphicsDevice);
        Chips = new Chips(this, graphicsDevice);
        Sparks = new Sparks(car, this, graphicsDevice);

        Visuals.ApplyDefaultsFrom(this);

        PositionWithoutInterpolation = car.Position;
        RotationWithoutInterpolation = car.Rotation;

        // Subscribe to backend car events
        car.DamagedX += OnDamagedX;
        car.DamagedY += OnDamagedY;
        car.DamagedZ += OnDamagedZ;
        car.Sparked += OnSparked;
        car.Dusted += OnDusted;
        car.CarPhysics.Distruct += OnDistruct;

        Sfx = new MadSfx(car.CarPhysics);
    }

    #region Event handlers

    private void OnDamagedX(CarStats stat, int wheelnum, fix64 amount)
    {
        MeshDamage.DamageX(stat, _car, this, wheelnum, (float)amount);
    }

    private void OnDamagedY(CarStats stat, int wheelnum, fix64 amount, bool mtouch, int nbsq, int squash)
    {
        MeshDamage.DamageY(stat, _car, this, wheelnum, (float)amount, mtouch, ref nbsq, ref squash);
    }

    private void OnDamagedZ(CarStats stat, int wheelnum, fix64 amount)
    {
        MeshDamage.DamageZ(stat, _car, this, wheelnum, (float)amount);
    }

    private void OnSparked(float wheelx, float wheely, float wheelz, float scx, float scy, float scz, int type, int wheelGround)
    {
        Sparks.AddSpark(wheelx, wheely, wheelz, scx, scy, scz, type, wheelGround);
    }

    private void OnDusted(int wheelidx, float wheelx, float wheely, float wheelz, int scx, int scz, float simag, int tilt, bool onRoof, int wheelGround)
    {
        Dust.AddDust(wheelidx, wheelx, wheely, wheelz, scx, scz, simag, tilt, onRoof, wheelGround);
    }

    private void OnDistruct(object? sender, EventArgs e)
    {
        VisuallyWasted = true;
    }

    #endregion

    public void Chip(int polyIdx, float breakFactor)
    {
        Chips.AddChip(polyIdx, breakFactor);
    }

    public void ChipWasted()
    {
        Chips.ChipWasted();
    }

    public override void GameTick(IStage? stage = null)
    {
        // IMPORTANT: call base first to snapshot the OLD position into PreviousState
        // for interpolation. Then sync the NEW position from the backend car.
        base.GameTick(stage);

        // Sync position/rotation from backend car
        Position = _car.Position;
        Rotation = _car.Rotation;

        foreach (var wheel in _wheels)
        {
            wheel.GameTick(stage);
        }
        Flames.GameTick();
        Dust.GameTick(stage);
        Chips.GameTick();
        Sparks.GameTick();
        Sfx?.Tick(_car.Control, _car.CarPhysics, _car.Stats);
    }

    public override IEnumerable<RenderData> GetRenderData(Lighting? lighting)
    {
        if (lighting?.IsCreateShadowMap == true && !(Visuals.CastsShadow || Position.Y < World.Ground)) yield break;

        for (var i = 0; i < _wheels.Length; i++)
        {
            var wheel = _wheels[i];
            wheel.Parent = this;
            wheel.Rotation = _car.Wheels[i].Rotates == 11 ? _car.TurningWheelAngle : _car.WheelAngle;

            foreach (var renderData in wheel.GetRenderData(lighting))
            {
                yield return renderData;
            }
        }

        // Override mesh visual properties from Visuals
        CastsShadow = Visuals.CastsShadow;
        GetsShadowed = Visuals.GetsShadowed ?? GetsShadowed;
        AlphaOverride = Visuals.AlphaOverride ?? AlphaOverride;
        Glow = Visuals.Glow ?? Glow;
        Finish = Visuals.Finish ?? Finish;

        foreach (var renderData in base.GetRenderData(lighting))
        {
            yield return renderData;
        }
    }

    public override void Render(Camera camera, Lighting? lighting)
    {
        base.Render(camera, lighting);

        foreach (var wheel in _wheels)
        {
            wheel.Render(camera, lighting);
        }

        if (lighting?.IsCreateShadowMap != true)
        {
            Flames.Render(camera);
            Dust.Render(camera);
            Chips.Render(camera);
            Sparks.Render(camera);
        }
    }

    public override void OnBeforeRender(float alpha)
    {
        base.OnBeforeRender(alpha);

        foreach (var wheel in _wheels)
        {
            wheel.OnBeforeRender(alpha);
        }
    }

    #region IDisposable

    private void ReleaseUnmanagedResources()
    {
        Chips.Dispose();
        Dust.Dispose();
        Flames.Dispose();
        Sparks.Dispose();
    }

    private void Dispose(bool disposing)
    {
        ReleaseUnmanagedResources();
        if (disposing)
        {
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~CarVisual()
    {
        Dispose(false);
    }

    #endregion
}
