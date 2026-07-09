using Microsoft.Xna.Framework.Graphics;
using NFMWorldLibrary;
using NFMWorldLibrary.Rad;

namespace NFMWorld;

public class GroundPolys : Transform, IRenderable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly VertexBuffer _vertexBuffer;
    private readonly IndexBuffer _indexBuffer;
    private readonly int _triangleCount;
    private readonly int _vertexCount;

    public override IReadOnlyList<ITransform> ChildTransforms => [];

    public GroundPolys(GraphicsDevice graphicsDevice, Rad3dPoly[] polys)
    {
        _graphicsDevice = graphicsDevice;
        
        var data = new List<VertexPositionColor>();
        var indices = new List<uint>();
        
        for (var i = 0; i < polys.Length; i++)
        {
            var poly = polys[i];

            var baseIndex = (uint)data.Count;
            foreach (var point in poly.Points)
            {
                var color = poly.Color;
                data.Add(new VertexPositionColor(point, color));
            }

            for (var index = 0; index < poly.Triangles.Length; index += 3)
            {
                var i0 = poly.Triangles[index];
                var i1 = poly.Triangles[index + 1];
                var i2 = poly.Triangles[index + 2];

                indices.AddRange(i0 + baseIndex, i1 + baseIndex, i2 + baseIndex);
            }
        }

        _vertexBuffer = new VertexBuffer(graphicsDevice, typeof(VertexPositionColor), data.Count, BufferUsage.None)
        {
            Name = "Ground Polys Vertex Buffer",
            Tag = this
        };
        _vertexBuffer.SetDataEXT(data);

        _indexBuffer = new IndexBuffer(graphicsDevice, IndexElementSize.ThirtyTwoBits, indices.Count, BufferUsage.None)
        {
            Name = "Ground Polys Index Buffer",
            Tag = this
        };
        _indexBuffer.SetDataEXT(indices);
        _triangleCount = indices.Count / 3;
        _vertexCount = data.Count;
    }
    
    ~GroundPolys()
    {
        _vertexBuffer.Dispose();
        _indexBuffer.Dispose();
    }

    public void SubmitDraws(RenderQueue queue, Camera camera, Lighting? lighting, RenderPass pass)
    {
        if (pass.IsShadow) return;

        var gd = _graphicsDevice;
        var vb = _vertexBuffer;
        var ib = _indexBuffer;
        var triCount = _triangleCount;
        var vertCount = _vertexCount;

        queue.AddImmediate(SortKey.ForOpaque(materialHash: 2), (cam, lt) =>
        {
            gd.SetVertexBuffer(vb);
            gd.Indices = ib;
            gd.DepthStencilState = DepthStencilState.DepthRead;
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
                gd.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, vertCount, 0, triCount);
            }
            gd.DepthStencilState = DepthStencilState.Default;
        });
    }
}