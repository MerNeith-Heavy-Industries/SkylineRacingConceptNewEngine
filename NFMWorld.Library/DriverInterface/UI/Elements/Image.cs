using NFMWorldLibrary.Backend.Gamemodes;
using WorldXaml.UI.Base;
using WorldXaml.UI.Yoga;

namespace NFMWorld.DriverInterface.UI;

public partial class Image : Node
{
    [Property(OnChangedMethod = nameof(OnImageDataChanged))]
    [ClientOnly]
    public partial IImage? ImageData { get; set; }

    [ClientOnly]
    private partial void OnImageDataChanged(IImage? value)
    {
        if (Width.Unit is YgUnit.Undefined or YgUnit.Point or YgUnit.Auto)
        {
            Width = Scale * value?.Width ?? 0;
        }
        if (Height.Unit is YgUnit.Undefined or YgUnit.Point or YgUnit.Auto)
        {
            Height = Scale * value?.Height ?? 0;
        }
    }
    
    [Property(OnChangedMethod = nameof(OnScaleChanged), DefaultValue = 1f)]
    public partial float Scale { get; set; }

    private partial void OnScaleChanged(float value)
    {
        if (Width.PointValue is {} widthValue)
        {
            Width = (int)(value * widthValue);
        }
        if (Height.PointValue is {} heightValue)
        {
            Height = (int)(value * heightValue);
        }
    }

    [ClientOnly]
    protected override void RenderContent(Vector2 position, Vector2 size)
    {
        if(ImageData != null)
        {
            G.DrawImage(ImageData, (int)position.X, (int)position.Y, (int)size.X, (int)size.Y);
        }
    }
}