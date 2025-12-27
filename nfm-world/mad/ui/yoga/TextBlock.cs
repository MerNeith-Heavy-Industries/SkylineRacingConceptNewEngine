using NFMWorld.DriverInterface;
using NFMWorld.Util;

namespace NFMWorld.Mad.UI.yoga;

public class TextBlock : Node
{
    public Font Font
    {
        get;
        set
        {
            field = value;
            _invalidated = true;
        }
    }

    public string Text
    {
        get;
        set
        {
            field = value;
            _invalidated = true;
        }
    }
    
    public BreakType BreakType
    {
        get;
        set
        {
            field = value;
            _invalidated = true;
        }
    } = BreakType.Word;
    
    public TextHorizontalAlignment HorizontalAlignment { get; set; } = TextHorizontalAlignment.Left;
    public TextVerticalAlignment VerticalAlignment { get; set; } = TextVerticalAlignment.Top;

    private bool _invalidated = true;
    private string? _formattedText;
    
    public override void RenderContent(Vector2 position, Vector2 size)
    {
        base.RenderContent(position, size);
        
        if (HasNewLayout || _invalidated || _formattedText == null)
        {
            _formattedText = G.LayoutText(Text, size.X, size.Y, BreakType);
            _invalidated = false;
            HasNewLayout = false;
        }
        
        G.SetFont(Font);
        G.DrawStringAligned(_formattedText, (int)size.X, (int)size.Y, HorizontalAlignment, VerticalAlignment);
    }
}