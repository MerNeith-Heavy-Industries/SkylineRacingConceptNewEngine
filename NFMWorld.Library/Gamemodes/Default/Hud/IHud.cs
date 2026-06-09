namespace NFMWorld.UI.Hud;

public interface IHud
{
    HudViewModel DataContext { get; set; }
    
    void LayoutAndRender(System.Numerics.Vector2 availableSize, System.Numerics.Vector2? origin = null);
}