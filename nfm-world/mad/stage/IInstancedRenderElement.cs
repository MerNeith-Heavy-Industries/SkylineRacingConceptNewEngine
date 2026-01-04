using Microsoft.Xna.Framework.Graphics;

namespace nfm_world.stage;

public interface IInstancedRenderElement
{
    void Render(Camera camera, Lighting? lighting, VertexBuffer instanceBuffer, int instanceCount);
}