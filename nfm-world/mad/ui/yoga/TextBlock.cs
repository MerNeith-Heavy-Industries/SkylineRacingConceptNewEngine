using NFMWorld.DriverInterface;
using NFMWorld.Util;

namespace NFMWorld.Mad.UI.yoga;

public class TextBlock : Node
{
    public Color Color { get; set; } = new Color(255, 255, 255);
    public Color? StrokeColor { get; set; } = null;
    public Font Font
    {
        get;
        set
        {
            field = value;
            _invalidated = true;
        }
    } = new Font(FontFamily.DroidSans, 0, 18);

    public string? Text
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
    
    public OverflowBehavior OverflowBehavior
    {
        get;
        set
        {
            field = value;
            _invalidated = true;
        }
    } = OverflowBehavior.ContinueHorizontally;
    
    public TextHorizontalAlignment HorizontalAlignment { get; set; } = TextHorizontalAlignment.Left;
    public TextVerticalAlignment VerticalAlignment { get; set; } = TextVerticalAlignment.Top;

    private bool _invalidated = true;
    private string? _formattedText;

    protected override void OnScaleChanged()
    {
        base.OnScaleChanged();
        _invalidated = true;
    }

    public override void RenderContent(Vector2 position, Vector2 size)
    {
        base.RenderContent(position, size);

        if (Text == null)
        {
            return;
        }
        
        G.SetFont(Font with { Size = Font.Size * G.Scale });
        if (HasNewLayout || _invalidated || _formattedText == null)
        {
            _formattedText = G.LayoutText(Text, size.X, size.Y, BreakType, OverflowBehavior);
            _invalidated = false;
            HasNewLayout = false;
        }
        
        if(StrokeColor != null)
        {
            G.SetColor((Color)StrokeColor);
            G.DrawStringStrokeAligned(_formattedText, (int)position.X, (int)position.Y, (int)size.X, (int)size.Y, HorizontalAlignment, VerticalAlignment);
        }

        G.SetColor(Color);
        G.DrawStringAligned(_formattedText, (int)position.X, (int)position.Y, (int)size.X, (int)size.Y, HorizontalAlignment, VerticalAlignment);
    }

    public new Action<TextBlock> Ref
    {
        set => value(this);
    }
}