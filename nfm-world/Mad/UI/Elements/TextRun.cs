using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Metadata;
using NFMWorld.DriverInterface;
using NFMWorld.Util;
using WorldXaml.UI.Base;
using WorldXaml.UI.Yoga;

namespace NFMWorld.UI;

public partial class TextRun : Node
{
    private IFontMetrics? _fontMetrics;

    /// <summary>
    /// Sets the fill color of the text. The default value is white.
    /// </summary>
    [Property(DefaultValueMember = nameof(DefaultColor))]
    public partial Color Color { get; set; }
    
    private static partial Color DefaultColor => new(255, 255, 255);
    
    /// <summary>
    /// Sets the stroke color of the text. Or set to null to disable the stroke.
    /// </summary>
    [Property]
    public partial Color? StrokeColor { get; set; }

    [Property(OnChangedMethod = nameof(OnFontChanged))]
    public partial Font Font { get; set; }
    
    private partial void OnFontChanged(Font newFont)
    {
        SetFontMetrics();
        RelayoutText();
    }
    
    /// <summary>
    /// Sets the text.
    /// </summary>
    [Content]
    [Property(DefaultValue = "", OnChangedMethod = nameof(OnTextChanged))]
    public partial string? Text { get; set; }
    
    private partial void OnTextChanged(string? newText)
    {
        RelayoutText();
    }

    [MemberNotNull(nameof(_fontMetrics))]
    private void SetFontMetrics()
    {
        G.SetFont(Font with { Size = Font.Size }); // Does not use scale here
        _fontMetrics = G.GetFontMetrics();
    }

    private void RelayoutText()
    {
        if (_fontMetrics == null)
        {
            SetFontMetrics();
        }
        Width = _fontMetrics!.StringWidth(Text ?? string.Empty);
        Height = _fontMetrics!.Height(Text ?? string.Empty);
    }

    /// <summary>
    /// Sets the horizontal alignment of the text. The default value is <see cref="TextHorizontalAlignment.Left"/>.
    /// </summary>
    [Property(DefaultValue = TextHorizontalAlignment.Left)]
    public partial TextHorizontalAlignment HorizontalAlignment { get; set; }

    /// <summary>
    /// Sets the vertical alignment of the text. The default value is <see cref="TextVerticalAlignment.Top"/>.
    /// </summary>
    [Property(DefaultValue = TextVerticalAlignment.Top)]
    public partial TextVerticalAlignment VerticalAlignment { get; set; }

    protected override void RenderContent(System.Numerics.Vector2 position, System.Numerics.Vector2 size)
    {
        base.RenderContent(position, size);

        if (string.IsNullOrEmpty(Text))
        {
            return;
        }
        
        G.SetFont(Font with { Size = Font.Size * G.Scale });
        if (StrokeColor != null)
        {
            G.SetColor((Color)StrokeColor);
            G.DrawStringStrokeAligned(Text, (int)position.X, (int)position.Y, (int)size.X, (int)size.Y, HorizontalAlignment, VerticalAlignment);
        }

        G.SetColor(Color);
        G.DrawStringAligned(Text, (int)position.X, (int)position.Y, (int)size.X, (int)size.Y, HorizontalAlignment, VerticalAlignment);
    }
}