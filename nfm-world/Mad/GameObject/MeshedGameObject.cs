using NFMWorldLibrary;
using NFMWorldLibrary.FixedMath;

namespace NFMWorld;

public class MeshedGameObject(Mesh mesh) : GameObject
{
    public Mesh Mesh = mesh;

    public MeshedGameObject(Mesh mesh, f64Vector3 position, f64Euler rotation) : this(mesh)
    {
        PositionWithoutInterpolation = position;
        RotationWithoutInterpolation = rotation;
    }

    public bool CastsShadow { get; set; } = mesh.CastsShadow;

    public bool? GetsShadowed
    {
        get => field ?? (Parent is MeshedGameObject parent ? parent.GetsShadowed : null);
        set;
    }

    public float? AlphaOverride
    {
        get => field ?? (Parent is MeshedGameObject parent ? parent.AlphaOverride : null);
        set;
    }

    public bool? Glow
    {
        get => field ?? (Parent is MeshedGameObject parent ? parent.Glow : null);
        set;
    }

    public bool? Finish
    {
        get => field ?? (Parent is MeshedGameObject parent ? parent.Finish : null);
        set;
    }

    /// <summary>
    /// Offset added to the base render order from <see cref="Mesh.GetRenderables"/>.
    /// Default 0 for stage pieces; set to 1 on cars and their wheels so they render after stage pieces.
    /// </summary>
    public int RenderOrderOffset { get; set; }

    public override void SubmitDraws(RenderQueue queue, Camera camera, Lighting? lighting, RenderPass pass)
    {
        if (pass.IsShadow && !(CastsShadow || Position.Y < World.Ground))
        {
            base.SubmitDraws(queue, camera, lighting, pass);
            return;
        }

        foreach (var (element, renderOrder) in Mesh.GetRenderables(lighting, Finish ?? false))
        {
            var actualRenderOrder = renderOrder + RenderOrderOffset;
            // Alpha-overridden objects always go to the highest (transparent) bucket
            if (AlphaOverride is {} alpha and < 1.0f)
                actualRenderOrder = 2;
            queue.AddInstanced(element,
                new InstanceData(MatrixWorld, GetsShadowed ?? true, AlphaOverride ?? 1.0f, Glow ?? false, Glow ?? false),
                actualRenderOrder);
        }

        base.SubmitDraws(queue, camera, lighting, pass);
    }
}