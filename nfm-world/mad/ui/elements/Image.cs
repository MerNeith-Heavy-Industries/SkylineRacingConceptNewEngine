using NFMWorld.DriverInterface;
using NFMWorld.Mad;
using NFMWorld.Mad.UI.yoga;

public class Image : Node
{
    public IImage? ImageData
    {
        get;
        set
        {
            field = value;
            Width = field?.Width ?? 0;
            Height = field?.Height ?? 0;
        }
    }

    public override void RenderContent(Vector2 position, Vector2 size)
    {
        if(ImageData != null)
        {
            G.DrawImage(ImageData, (int)position.X, (int)position.Y, (int)size.X, (int)size.Y);
        }
    }
}