using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls.Documents;
using Avalonia.Metadata;
using NFMWorld.DriverInterface;
using NFMWorld.Util;
using WorldXaml.UI.Base;
using WorldXaml.UI.Yoga;

namespace NFMWorld.UI;

public partial class TextRun : Node, IInlineHost, IAddChild<Inline>
{
    private IFontMetrics? _fontMetrics;
    private ComplexTextMetrics.RichTextContainer? _laidOutComplexText;

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
    
    [Content]
    public InlineCollection Inlines { get; }

    [MemberNotNullWhen(true, nameof(Inlines))]
    public bool HasComplexContent => Inlines is { Count: > 0 };

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
    [Property(DefaultValue = "", OnChangedMethod = nameof(OnTextChanged))]
    public partial string? Text { get; set; }

    public TextRun()
    {
        Inlines = new InlineCollection(this);
    }
    
    private partial void OnTextChanged(string? newText)
    {
        if (HasComplexContent && !_clearTextInternal)
        {
            Inlines.Clear();
        }

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

        if (!HasComplexContent)
        {
            var measurements = _fontMetrics.MeasureText(Text ?? string.Empty);
            Width = measurements.X;
            Height = measurements.Y;
        }
        else
        {
            var measurements = ComplexTextMetrics.MeasureRichText(Inlines.OfType<IRichTextElement>(), Font);
            Width = measurements.Size.X;
            Height = measurements.Size.Y;
            _laidOutComplexText = measurements;
        }
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

        if (!HasComplexContent)
        {
            if (string.IsNullOrEmpty(Text))
            {
                return;
            }

            G.SetFont(Font with { Size = Font.Size * G.Scale });
            if (StrokeColor != null)
            {
                G.SetColor((Color)StrokeColor);
                G.DrawStringStrokeAligned(Text, (int)position.X, (int)position.Y, (int)size.X, (int)size.Y,
                    HorizontalAlignment, VerticalAlignment);
            }

            G.SetColor(Color);
            G.DrawStringAligned(Text, (int)position.X, (int)position.Y, (int)size.X, (int)size.Y, HorizontalAlignment, VerticalAlignment);
        }
        else
        {
            Debug.Assert(_laidOutComplexText != null, "Complex text layout should have been calculated in RelayoutText method.");

            if (_laidOutComplexText.Value.Elements.Count == 0)
            {
                return;
            }

            var basePosition = position;
            ComplexTextMetrics.AlignBounds(_laidOutComplexText.Value.Size, (int)size.X, (int)size.Y, HorizontalAlignment, VerticalAlignment, ref basePosition.X, ref basePosition.Y);

            foreach (var element in _laidOutComplexText.Value.Elements)
            {
                G.SetFont(element.Font with { Size = Font.Size * G.Scale });
                if (element.Background is { } background)
                {
                    G.SetColor(background);
                    G.FillRect((int)basePosition.X, (int)basePosition.Y, (int)element.Size.X, (int)element.Size.Y);
                }

                float yOff = 0;
                if (VerticalAlignment == TextVerticalAlignment.Center)
                {
                    yOff = -(G.GetFontMetrics(element.Font).LineHeight / 2.0f);
                }
                else if (VerticalAlignment == TextVerticalAlignment.Bottom)
                {
                    yOff = -G.GetFontMetrics(element.Font).LineHeight;
                }

                int x = (int)(basePosition.X + element.Position.X);
                int y = (int)(basePosition.Y + element.Position.Y + yOff);

                if ((element.Stroke ?? StrokeColor) is { } stroke)
                {
                    G.SetColor(stroke);
                    G.DrawStringStroke(element.Text, x, y);
                }
                
                G.SetColor(element.Foreground ?? Color);
                G.DrawString(element.Text, x, y);
            }
        }
    }
        
    void IAddChild<Inline>.AddChild(Inline child)
    {
        Inlines?.Add(child);
    }

    void IAddChild.AddChild(object child)
    {
        if (child is Inline node)
        {
            Inlines.Add(node);
        }
    }

    public void Invalidate()
    {
        RelayoutText();
    }


    private bool _clearTextInternal;
    internal void ClearTextInternal()
    {
        _clearTextInternal = true;
        try
        {
            Text = null;
        }
        finally
        {
            _clearTextInternal = false;
        }
    }
}