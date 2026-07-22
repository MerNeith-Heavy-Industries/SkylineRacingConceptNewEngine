namespace NFMWorld;

public interface IImmediateRenderElement
{
    void Render(Camera camera, Lighting? lighting);
}