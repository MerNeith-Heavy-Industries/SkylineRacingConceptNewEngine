using Microsoft.Xna.Framework;
using NFMWorldLibrary.Backend.Gamemodes;
using WorldXaml.UI.Base;
using WorldXaml.UI.Yoga;

namespace NFMWorld.DriverInterface.UI;

/// <summary>
/// Represents a box element with solid colors for border, background, and content.
/// </summary>
public class SolidBox : FlexPanel
{
    [Property]
    public Color BorderColor { get; set; } = new Color(0, 0, 0, 255);
    [Property]
    public Color BackgroundColor { get; set; } = new Color(150, 255, 150, 255);

    [Property]
    public int BorderTopLeftRadius { get; set; }
    [Property]
    public int BorderTopRightRadius { get; set; }
    [Property]
    public int BorderBottomLeftRadius { get; set; }
    [Property]
    public int BorderBottomRightRadius { get; set; }

    [ClientOnly]
    protected override void RenderBackground(Vector2 position, Vector2 size)
    {
        var avgBorder = (BorderTop ?? 0) + (BorderLeft ?? 0) + (BorderBottom ?? 0) + (BorderRight ?? 0) / 4f;
        
        G.SetColor(BackgroundColor);
        var radTopLeft = Math.Max(0, BorderTopLeftRadius - ((BorderTop ?? 0) + (BorderLeft ?? 0) / 2f));
        var radTopRight = Math.Max(0, BorderTopRightRadius - ((BorderTop ?? 0) + (BorderRight ?? 0) / 2f));
        var radBottomRight = Math.Max(0, BorderBottomRightRadius - ((BorderBottom ?? 0) + (BorderRight ?? 0) / 2f));
        var radBottomLeft = Math.Max(0, BorderBottomLeftRadius - ((BorderBottom ?? 0) + (BorderLeft ?? 0) / 2f));
        G.FillRoundedRect((int) position.X, (int) position.Y, (int) (size.X + avgBorder), (int) (size.Y + avgBorder), radTopLeft * G.Scale, radTopRight * G.Scale, radBottomRight * G.Scale, radBottomLeft * G.Scale);
    }
    
    [ClientOnly]
    protected override void RenderBorder(Vector2 position, Vector2 size)
    {
        G.SetColor(BorderColor);
        var avgBorder = (BorderTop ?? 0) + (BorderLeft ?? 0) + (BorderBottom ?? 0) + (BorderRight ?? 0) / 4f;
        G.SetStrokeWidth(avgBorder);
        var radTopLeft = BorderTopLeftRadius;
        var radTopRight = BorderTopRightRadius;
        var radBottomRight = BorderBottomRightRadius;
        var radBottomLeft = BorderBottomLeftRadius;
        G.DrawRoundedRect((int) (position.X + avgBorder / 2), (int) (position.Y + avgBorder / 2), (int) size.X, (int) size.Y, radTopLeft * G.Scale, radTopRight * G.Scale, radBottomRight * G.Scale, radBottomLeft * G.Scale);
        G.SetStrokeWidth();
    }
}
