using Microsoft.Xna.Framework.Graphics;
using NFMWorldLibrary.Rad;

namespace NFMWorld;

/// <summary>
/// DO NOT USE THIS EXCEPT FOR EDITOR STUFF!! IT IS SLOW AS FUCK!!!!!!!!!!
/// </summary>
public class ImmediateMesh : Mesh, IImmediateRenderable
{
    public ImmediateMesh(GraphicsDevice graphicsDevice, Rad3d rad) : base(graphicsDevice, rad)
    {
    }

    public ImmediateMesh(Mesh baseMesh) : base(baseMesh)
    {
    }

    public void Render(Camera camera, Lighting? lighting)
    {
        var vertexBuffer = new DynamicVertexBuffer(GraphicsDevice, InstanceData.InstanceDeclaration, 1, BufferUsage.WriteOnly);

        foreach (var (element, renderOrder) in GetRenderables(lighting, false).OrderBy(x => x.RenderOrder))
        {
            var renderData = new RenderData(element, Matrix.Identity);
            vertexBuffer.SetDataEXT((ReadOnlySpan<InstanceData>)[renderData.ToInstanceData()], SetDataOptions.Discard);
            element.Render(camera, lighting, vertexBuffer, 1);
        }
    }
}