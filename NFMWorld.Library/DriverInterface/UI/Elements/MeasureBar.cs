using Microsoft.Xna.Framework;
using WorldXaml.UI.Base;

namespace NFMWorld.DriverInterface.UI;

public partial class MeasureBar : Image
{
    [Property(DefaultValueMember = nameof(DefaultColor))]
    public partial Color Color { get; set; }
    private static partial Color DefaultColor => new Color(255, 255, 255);
    
    /// <summary>
    /// 1f = full, 0f = empty
    /// </summary>
    [Property]
    public partial float FillAmount { get; set; }

    protected override void RenderContent(System.Numerics.Vector2 position, System.Numerics.Vector2 size)
    {
        base.RenderContent(position, size);
        G.SetColor(Color);
        G.FillRect((int)(position.X + (63*G.Scale*Scale)), (int)(position.Y + (4*G.Scale*Scale) + 1), (int)(FillAmount * 99 * G.Scale*Scale), (int)(9*G.Scale*Scale));
    }
}