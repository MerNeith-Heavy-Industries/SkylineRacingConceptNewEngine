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
        const int barX = 304;
        const int barY = 18;
        const int barWidth = 543;
        const int barHeight = 49;

        // image file size
        const int imgWidth = 905;
        const int imgHeight = 87;

        // layout size of the element
        var renderedWidth = size.X;
        var renderedHeight = size.Y;
        
        base.RenderContent(position, size);
        G.SetColor(Color);
        G.FillRect(
            (int)(position.X + (barX * (renderedWidth / imgWidth)) * Scale),
            (int)(position.Y + (barY * (renderedHeight / imgHeight)) * Scale),
            (int)(FillAmount * (barWidth * (renderedWidth / imgWidth)) * Scale),
            (int)(barHeight * (renderedHeight / imgHeight) * Scale)
        );
    }
}