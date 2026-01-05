using nfm_world_library.util;
using nfm_world.ui.yoga;

namespace nfm_world.ui.elements;

public class Box : Node
{
    public Color BorderColor { get; set; } = new Color(0, 0, 0, 255);
    public Color BackgroundColor { get; set; } = new Color(150, 255, 150, 255);
    public Color ContentColor { get; set; } = new Color(0, 0, 0, 255);

    public override void RenderBackground(Vector2 position, Vector2 size)
    {
        G.SetColor(BackgroundColor);
        G.FillRect((int) position.X, (int) position.Y, (int) size.X, (int) size.Y);
    }
    
    public override void RenderBorder(Vector2 position, Vector2 size)
    {
        G.SetColor(BorderColor);
        G.DrawRect((int) position.X, (int) position.Y, (int) size.X, (int) size.Y);
    }

    public override void RenderContent(Vector2 position, Vector2 size)
    {
        G.SetColor(ContentColor);
        G.FillRect((int) position.X, (int) position.Y, (int) size.X, (int) size.Y);
    }
}