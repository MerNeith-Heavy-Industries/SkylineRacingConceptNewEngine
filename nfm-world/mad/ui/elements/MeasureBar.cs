using NFMWorld.Util;
using NFMWorld.Mad.UI.yoga;
using NFMWorld.DriverInterface;

namespace NFMWorld.Mad.UI.Elements;

public class MeasureBar : Node
{
    public Color BarColor { get; set; } = new Color(255, 255, 255);
    /// <summary>
    /// 1f = full, 0f = empty
    /// </summary>
    public float BarFillAmount { get; set; } = 0f;
    public IImage BarImage { get; set; } = null!;
    public float Scale
    {
        get;
        set
        {
            field = value;
            Width = (int)(field * Width.InternalValue.value);
            Height = (int)(field * Height.InternalValue.value);
        }
    } = 1f;

    public override void RenderBackground(Vector2 position, Vector2 size)
    {

    }
    
    public override void RenderBorder(Vector2 position, Vector2 size)
    {

    }

    public override void RenderContent(Vector2 position, Vector2 size)
    {
        G.DrawImage(BarImage, (int)position.X, (int)position.Y, (int)size.X, (int)size.Y);
        G.SetColor(BarColor);
        G.FillRect((int)(position.X + (63*G.Scale*Scale)), (int)(position.Y + (4*G.Scale*Scale) + 1), (int)(BarFillAmount * 99 * G.Scale*Scale), (int)(9*G.Scale*Scale));
    }
}