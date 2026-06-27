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
        base.RenderContent(position, size);
        G.SetColor(Color);
        G.FillRect((int)(position.X + (63*G.Scale*Scale)), (int)(position.Y + (4*G.Scale*Scale) + 1), (int)(FillAmount * 99 * G.Scale*Scale), (int)(9*G.Scale*Scale));
    }
}
