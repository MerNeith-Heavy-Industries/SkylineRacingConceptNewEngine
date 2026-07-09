using Microsoft.Xna.Framework.Graphics;
using NFMWorldLibrary;

namespace NFMWorld;

public class Ground : Transform, IRenderable, IImmediateRenderElement
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly VertexBuffer _vertexBuffer;
    private readonly int _triangleCount;

    public override IReadOnlyList<ITransform> ChildTransforms => [];

    public Ground(GraphicsDevice graphicsDevice)
    {
        // Generate a quad on World.Ground extending infinitely in X and Z
        _graphicsDevice = graphicsDevice;
        const int size = 1_000_000;
        var color = World.GroundColor.Snap(World.Snap);
        Span<VertexPositionColor> data =
        [
            new(new Vector3(-size, World.Ground, -size), color),
            new(new Vector3(size, World.Ground, -size), color),
            new(new Vector3(-size, World.Ground, size), color),
            new(new Vector3(size, World.Ground, -size), color),
            new(new Vector3(-size, World.Ground, size), color),
            new(new Vector3(size, World.Ground, size), color)
        ];

        _vertexBuffer = new VertexBuffer(graphicsDevice, typeof(VertexPositionColor), data.Length, BufferUsage.None)
        {
            Name = "Ground Vertex Buffer",
            Tag = this
        };
        _vertexBuffer.SetDataEXT(data);
        _triangleCount = data.Length / 3;
    }
    
    ~Ground()
    {
        _vertexBuffer.Dispose();
    }

    public void SubmitDraws(RenderQueue queue, Camera camera, Lighting? lighting, RenderPass pass)
    {
        if (pass.IsShadow) return;

        queue.AddImmediate(SortKey.ForOpaque(RenderMaterial.Ground), this);
    }

    public void Render(Camera cam, Lighting? lt)
    {
        _graphicsDevice.SetVertexBuffer(_vertexBuffer);
        _graphicsDevice.DepthStencilState = DepthStencilState.DepthRead;
        Effects.Ground.Parameters["WorldView"]?.SetValue(cam.ViewMatrix);
        Effects.Ground.Parameters["WorldViewProj"]?.SetValue(cam.ViewMatrix * cam.ProjectionMatrix);

        Effects.Ground.Parameters["DepthBias"]?.SetValue(0.00005f);
        Effects.Ground.Parameters["FogColor"]?.SetValue((Vector3)World.Fog.Snap(World.Snap));
        Effects.Ground.Parameters["FogDistance"]?.SetValue(World.FadeFrom);
        Effects.Ground.Parameters["FogDensity"]?.SetValue(World.FogDensity / (World.FogDensity + 1f));

        lt?.SetShadowMapParameters(Effects.Ground.UnderlyingEffect);

        foreach (var pass in Effects.Ground.CurrentTechnique.Passes)
        {
            pass.Apply();
            _graphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, _triangleCount);
        }

        _graphicsDevice.DepthStencilState = DepthStencilState.Default;
    }
}