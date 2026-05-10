namespace NFMWorld;

public interface IImmediateRenderable
{
    void OnBeforeRender(float alpha)
    {
    }

    void Render(Camera camera, Lighting? lighting);
}