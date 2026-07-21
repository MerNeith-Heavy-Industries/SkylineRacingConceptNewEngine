namespace NFMWorld.UI.Hud;

public interface IHud
{
    void LayoutAndRender(Vector2 availableSize, Vector2? origin = null);
}