using Microsoft.Xna.Framework;
using NFMWorldLibrary.Backend.Gamemodes;
using WorldXaml.UI.Base;
using WorldXaml.UI.Yoga;

namespace NFMWorld.DriverInterface.UI;

/// <summary>
/// Represents a box element with solid colors for border, background, and content.
/// </summary>
public partial class SolidBox : FlexPanel
{
    [Property(DefaultValueMember = nameof(DefaultBorderColor))]
    public partial Color BorderColor { get; set; }
    private static partial Color DefaultBorderColor => new Color(0, 0, 0, 255);
    [Property(DefaultValueMember = nameof(DefaultBackgroundColor))]
    public partial Color BackgroundColor { get; set; }
    private static partial Color DefaultBackgroundColor => new Color(150, 255, 150, 255);
    [Property(DefaultValueMember = nameof(DefaultContentColor))]
    public partial Color ContentColor { get; set; }
    private static partial Color DefaultContentColor => new Color(0, 0, 0, 255);

    [Property]
    public partial int BorderTopLeftRadius { get; set; }
    [Property]
    public partial int BorderTopRightRadius { get; set; }
    [Property]
    public partial int BorderBottomLeftRadius { get; set; }
    [Property]
    public partial int BorderBottomRightRadius { get; set; }

    [ClientOnly]
    protected override void RenderBackground(System.Numerics.Vector2 position, System.Numerics.Vector2 size)
    {
        G.SetColor(BackgroundColor);
        G.FillRect((int) position.X, (int) position.Y, (int) size.X, (int) size.Y);
    }
    
    [ClientOnly]
    protected override void RenderBorder(System.Numerics.Vector2 position, System.Numerics.Vector2 size)
    {
        G.SetColor(BorderColor);
        var radTopLeft = BorderTopLeftRadius;
        var radTopRight = BorderTopRightRadius;
        var radBottomRight = BorderBottomRightRadius;
        var radBottomLeft = BorderBottomLeftRadius;
        G.FillRoundedRect((int) position.X, (int) position.Y, (int) size.X, (int) size.Y, radTopLeft * G.Scale, radTopRight * G.Scale, radBottomRight * G.Scale, radBottomLeft * G.Scale);
    }

    [ClientOnly]
    protected override void RenderContent(System.Numerics.Vector2 position, System.Numerics.Vector2 size)
    {
        G.SetColor(ContentColor);
        var radTopLeft = Math.Max(0, BorderTopLeftRadius - ((BorderTop ?? 0) + (BorderLeft ?? 0) / 2f));
        var radTopRight = Math.Max(0, BorderTopRightRadius - ((BorderTop ?? 0) + (BorderRight ?? 0) / 2f));
        var radBottomRight = Math.Max(0, BorderBottomRightRadius - ((BorderBottom ?? 0) + (BorderRight ?? 0) / 2f));
        var radBottomLeft = Math.Max(0, BorderBottomLeftRadius - ((BorderBottom ?? 0) + (BorderLeft ?? 0) / 2f));
        G.FillRoundedRect((int) position.X, (int) position.Y, (int) size.X, (int) size.Y, radTopLeft * G.Scale, radTopRight * G.Scale, radBottomRight * G.Scale, radBottomLeft * G.Scale);
    }
}