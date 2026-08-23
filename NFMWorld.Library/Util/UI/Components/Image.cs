using NFMWorld.DriverInterface.DriverInterface;
using NFMWorld.Reactor;
using NFMWorldLibrary.Backend.Gamemodes;
using NFMWorldLibrary.Util;

namespace NFMWorld.DriverInterface.UI;

public partial class Image : Component
{
    public override bool DebugIsContentfulNode => true;

    private View _imageSlot;
    private Node[] _children;

    public Image()
    {
        _imageSlot = new View();
        _children = [_imageSlot];
        VisualChildren = new ReadOnlyLuaArray<Node>(_children);
        NodeInternal.InsertChild(_imageSlot.NodeInternal, 0);
    }

    public override ReadOnlyLuaArray<Node> VisualChildren { get; }

    [ClientOnly]
    public IImage? ImageData
    {
        get;
        set
        {
            field = value;
            _imageSlot.Styles = _imageSlot.Styles with
            {
                Width = Scale * value?.Width ?? 0,
                Height = Scale * value?.Height ?? 0
            };
        }
    }

    [ClientOnly]
    public float Scale
    {
        get;
        set
        {
            field = value;
            if (ImageData is { } imageData)
            {
                _imageSlot.Styles = _imageSlot.Styles with
                {
                    Width = value * imageData.Width,
                    Height = value * imageData.Height
                };
            }
        }
    } = 1f;

    [ClientOnly]
    protected override void RenderContent(LuaVector2 position, LuaVector2 size)
    {
        if (ImageData != null)
        {
            G.DrawImage(ImageData, (int)position.X, (int)position.Y, (int)size.X, (int)size.Y);
        }
    }
}