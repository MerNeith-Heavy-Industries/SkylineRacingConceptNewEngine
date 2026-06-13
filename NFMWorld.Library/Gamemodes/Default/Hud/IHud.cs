namespace NFMWorld.UI.Hud;

public interface IHud
{
    HudViewModel DataContext { get; set; }
    
    void LayoutAndRender(Vector2 availableSize, Vector2? origin = null);
}