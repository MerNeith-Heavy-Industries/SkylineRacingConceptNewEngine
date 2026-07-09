using Microsoft.Xna.Framework.Graphics;
using NFMWorldLibrary.FixedMath;
using NFMWorldLibrary.Rad;

// This duplicates some code from CollisionObject, no workaround
namespace NFMWorld;

public class EditorObject : StaticMeshObject
{
    public Rad3dBoxDef[] Boxes { get; }

    private readonly CollisionDebugMesh? _collisionDebugMesh;

    private readonly MeshedGameObject[] _wheels;

    public IReadOnlyList<MeshedGameObject> WheelObjects => _wheels;

    public Rad3dWheelDef[] Wheels { get; }

    public EditorObject(GraphicsDevice graphicsDevice, Rad3d rad) : base(graphicsDevice, rad)
    {
        Boxes = rad.Boxes;
        Wheels = rad.Wheels;
        if (rad.Boxes.Length > 0)
        {
            _collisionDebugMesh = new CollisionDebugMesh(rad.Boxes)
            {
                Parent = this
            };
        }

        Wheels = rad.Wheels;
        _wheels = rad.Wheels
            .Select(wheel => new WheelMeshBuilder(wheel, rad.Rims).BuildGameObject(graphicsDevice, this))
            .ToArray();
    }

    public EditorObject(GraphicsDevice graphicsDevice, Rad3d rad, f64Vector3 position, f64Euler rotation) : this(graphicsDevice, rad)
    {
        Position = position;
        Rotation = rotation;
    }

    public override void OnBeforeRender(float alpha)
    {
        base.OnBeforeRender(alpha);
        for (var i = 0; i < _wheels.Length; i++)
        {
            _wheels[i].Parent = this;
            _wheels[i].OnBeforeRender(alpha);
        }
    }

    public override void SubmitDraws(RenderQueue queue, Camera camera, Lighting? lighting, RenderPass pass)
    {
        base.SubmitDraws(queue, camera, lighting, pass);

        foreach (var wheel in _wheels)
        {
            wheel.SubmitDraws(queue, camera, lighting, pass);
        }

        _collisionDebugMesh?.SubmitDraws(queue, camera, lighting, pass);
    }
}