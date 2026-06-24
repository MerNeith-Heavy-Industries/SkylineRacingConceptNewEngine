using NFMWorldLibrary.Backend.Gamemodes;
using WorldXaml.UI.Base;
using WorldXaml.UI.Yoga;

namespace NFMWorld.DriverInterface.UI;

public partial class Image : Node
{
    [ClientOnly]
    [Property]
    public IImage? ImageData
    {
        get;
        set
        {
            field = value;
            if (Width.Unit is YgUnit.Undefined or YgUnit.Point or YgUnit.Auto)
            {
                Width = Scale * value?.Width ?? 0;
            }
            if (Height.Unit is YgUnit.Undefined or YgUnit.Point or YgUnit.Auto)
            {
                Height = Scale * value?.Height ?? 0;
            }
        }
    }

    [Property]
    public float Scale
    {
        get;
        set
        {
            field = value;
            if (Width.PointValue is {} widthValue)
            {
                Width = (int)(value * widthValue);
            }
            if (Height.PointValue is {} heightValue)
            {
                Height = (int)(value * heightValue);
            }
        }
    } = 1f;

    [ClientOnly]
    protected override void RenderContent(Vector2 position, Vector2 size)
    {
        if(ImageData != null)
        {
            G.DrawImage(ImageData, (int)position.X, (int)position.Y, (int)size.X, (int)size.Y);
        }
    }
}