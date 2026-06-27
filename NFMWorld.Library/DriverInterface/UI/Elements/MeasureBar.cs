using Microsoft.Xna.Framework;
using WorldXaml.UI.Base;

namespace NFMWorld.DriverInterface.UI;

public partial class MeasureBar : Image
{
    [Property]
    public Color Color { get; set; } = new Color(255, 255, 255);
    
    /// <summary>
    /// 1f = full, 0f = empty
    /// </summary>
    [Property]
    public float FillAmount { get; set; }

    protected override void RenderContent(Vector2 position, Vector2 size)
    {
        // positions of bar in image file
        const int barX = 486;
        const int barY = 28;
        const int barWidth = 824;
        const int barHeight = 78;

        // image file size
        const int imgWidth = 1398;
        const int imgHeight = 135;

        // layout size of the element
        var renderedWidth = size.X;
        var renderedHeight = size.Y;
        
        base.RenderContent(position, size);
        G.SetColor(Color);
        G.FillRect(
            (int)MathF.Round(position.X + (barX * (renderedWidth / imgWidth)) * Scale),
            (int)MathF.Round(position.Y + (barY * (renderedHeight / imgHeight)) * Scale),
            (int)MathF.Round(FillAmount * (barWidth * (renderedWidth / imgWidth)) * Scale),
            (int)MathF.Round(barHeight * (renderedHeight / imgHeight) * Scale)
        );
    }
}