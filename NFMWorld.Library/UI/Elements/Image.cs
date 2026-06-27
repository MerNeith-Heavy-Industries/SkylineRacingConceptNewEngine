using NFMWorld.Reactor;
using NFMWorldLibrary.Backend.Gamemodes;
using WorldXaml.UI.Yoga;
using Yoga;

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
            Width = Scale * value?.Width ?? 0;
            Height = Scale * value?.Height ?? 0;
        }
    }

    [ClientOnly]
    [Property]
    public float Scale
    {
        get;
        set
        {
            field = value;
            if (ImageData is { } imageData)
            {
                Width = value * imageData.Width;
                Height = value * imageData.Height;
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