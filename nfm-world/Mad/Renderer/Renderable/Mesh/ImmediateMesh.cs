using Microsoft.Xna.Framework.Graphics;
using NFMWorldLibrary.Rad;

namespace NFMWorld;

/// <summary>
/// DO NOT USE THIS EXCEPT FOR EDITOR STUFF!! IT IS SLOW AS FUCK!!!!!!!!!!
/// </summary>
public class ImmediateMesh : Mesh, IRenderable
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
            vertexBuffer.SetDataEXT((ReadOnlySpan<InstanceData>)[new InstanceData(Matrix.Identity)], SetDataOptions.Discard);
            element.Render(camera, lighting, vertexBuffer, 1);
        }
    }

    public void SubmitDraws(RenderQueue queue, Camera camera, Lighting? lighting, RenderPass pass)
    {
        foreach (var (element, renderOrder) in GetRenderables(lighting, false).OrderBy(x => x.RenderOrder))
        {
            queue.AddInstanced(element, new InstanceData(Matrix.Identity), renderOrder);
        }
    }
}