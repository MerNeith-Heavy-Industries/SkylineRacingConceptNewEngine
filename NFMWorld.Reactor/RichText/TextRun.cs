using System.Diagnostics;
using System.Numerics;
using Microsoft.Xna.Framework;
using NFMWorld.DriverInterface;
using NFMWorldLibrary.Backend.Gamemodes;

namespace NFMWorld.Reactor;

public enum BreakType
{
    None,
    Word,
    Character
}

public enum OverflowBehavior
{
    None,
    Stretch,
    ContinueVertically,
    ContinueHorizontally
}

public partial class TextRun : Node
{
    internal Property<Color?> _background;
    internal Property<Color> _foreground;
    internal Property<Color?> _stroke;
    internal Property<FontFamily> _fontFamily;
    internal Property<float> _fontSize;
    internal Property<FontStyle> _fontStyle;
    internal Property<BreakType> _breakType;
    internal Property<OverflowBehavior> _overflowBehavior;
    internal Property<TextHorizontalAlignment> _horizontalAlignment;
    internal Property<TextVerticalAlignment> _verticalAlignment;

    protected bool Invalidated { get; private set; }= true;
    public ComplexTextMetrics.RichTextContainer? LaidOutComplexText;

    public TextRun()
    {
        _background = null;
        _foreground = new Color(255, 255, 255);
        _stroke = null;
        _fontFamily = new(FontFamily.DroidSans, this, static (ctx, o, n) => ((TextRun)ctx!).Invalidate());
        _fontSize = new(12f, this, static (ctx, o, n) => ((TextRun)ctx!).Invalidate());
        _fontStyle = new(FontStyle.Plain, this, static (ctx, o, n) => ((TextRun)ctx!).Invalidate());
        _breakType = new(BreakType.Word, this, static (ctx, o, n) => ((TextRun)ctx!).Invalidate());
        _overflowBehavior = new(OverflowBehavior.Stretch, this, static (ctx, o, n) => ((TextRun)ctx!).Invalidate());
        _horizontalAlignment = TextHorizontalAlignment.Left;
        _verticalAlignment = TextVerticalAlignment.Top;
    }

    public override bool DebugIsContentfulNode => true;
    
    /// <summary>
    /// Sets the background color of the text.
    /// </summary>
    public Color? Background
    {
        get => _background.ComputedValue;
        set => _background.SetOverrideValue(value);
    }

    /// <summary>
    /// Sets the fill color of the text. The default value is white.
    /// </summary>
    public Color Foreground
    {
        get => _foreground.ComputedValue;
        set => _foreground.SetOverrideValue(value);
    }
    
    /// <summary>
    /// Sets the stroke color of the text. Or set to null to disable the stroke.
    /// </summary>
    public Color? Stroke
    {
        get => _stroke.ComputedValue;
        set => _stroke.SetOverrideValue(value);
    }

    /// <summary>
    /// Gets or sets the font family.
    /// </summary>
    public FontFamily FontFamily
    {
        get => _fontFamily.ComputedValue;
        set => _fontFamily.SetOverrideValue(value);
    }

    /// <summary>
    /// Gets or sets the font size.
    /// </summary>
    public float FontSize
    {
        get => _fontSize.ComputedValue;
        set => _fontSize.SetOverrideValue(value);
    }

    /// <summary>
    /// Gets or sets the font style.
    /// </summary>
    public FontStyle FontStyle
    {
        get => _fontStyle.ComputedValue;
        set => _fontStyle.SetOverrideValue(value);
    }

    [Property]
    public TextElement[] Elements { get; set; } = [];

    public bool HasComplexContent => Elements.Length > 0;

    public BreakType BreakType
    {
        get => _breakType.ComputedValue;
        set => _breakType.SetOverrideValue(value);
    }

    public OverflowBehavior OverflowBehavior
    {
        get => _overflowBehavior.ComputedValue;
        set => _overflowBehavior.SetOverrideValue(value);
    }

    /// <summary>
    /// Sets the text.
    /// </summary>
    [Property]
    public string? Text
    {
        get;
        set
        {
            field = value;
            
            if (HasComplexContent)
            {
                Elements = [];
            }

            Invalidate();
        }
    } = "";

    /// <summary>
    /// Sets the horizontal alignment of the text. The default value is <see cref="TextHorizontalAlignment.Left"/>.
    /// </summary>
    public TextHorizontalAlignment HorizontalAlignment
    {
        get => _horizontalAlignment.ComputedValue;
        set => _horizontalAlignment.SetOverrideValue(value);
    }

    /// <summary>
    /// Sets the vertical alignment of the text. The default value is <see cref="TextVerticalAlignment.Top"/>.
    /// </summary>
    public TextVerticalAlignment VerticalAlignment
    {
        get => _verticalAlignment.ComputedValue;
        set => _verticalAlignment.SetOverrideValue(value);
    }

    protected override void UpdateStyles(StyleSheetStyles? oldStyleSheet, StyleSheetStyles? newStyleSheet)
    {
        base.UpdateStyles(oldStyleSheet, newStyleSheet);
        
        if (oldStyleSheet is { } oldStyleSheetValue)
        {
            if (oldStyleSheetValue.Background is not null) _background.ClearStyleValue();
            if (oldStyleSheetValue.Foreground is not null) _foreground.ClearStyleValue();
            if (oldStyleSheetValue.Stroke is not null) _stroke.ClearStyleValue();
            if (oldStyleSheetValue.FontFamily is not null) _fontFamily.ClearStyleValue();
            if (oldStyleSheetValue.FontSize is not null) _fontSize.ClearStyleValue();
            if (oldStyleSheetValue.FontStyle is not null) _fontStyle.ClearStyleValue();
            if (oldStyleSheetValue.BreakType is not null) _breakType.ClearStyleValue();
            if (oldStyleSheetValue.OverflowBehavior is not null) _overflowBehavior.ClearStyleValue();
            if (oldStyleSheetValue.HorizontalAlignment is not null) _horizontalAlignment.ClearStyleValue();
            if (oldStyleSheetValue.VerticalAlignment is not null) _verticalAlignment.ClearStyleValue();
        }
        
        if (newStyleSheet is { } newStyleSheetValue)
        {
            if (newStyleSheetValue.Background is {} background) _background.SetStyleValue(background);
            if (newStyleSheetValue.Foreground is {} foreground) _foreground.SetStyleValue(foreground);
            if (newStyleSheetValue.Stroke is {} stroke) _stroke.SetStyleValue(stroke);
            if (newStyleSheetValue.FontFamily is {} fontFamily) _fontFamily.SetStyleValue(fontFamily);
            if (newStyleSheetValue.FontSize is {} fontSize) _fontSize.SetStyleValue(fontSize);
            if (newStyleSheetValue.FontStyle is {} fontStyle) _fontStyle.SetStyleValue(fontStyle);
            if (newStyleSheetValue.BreakType is {} breakType) _breakType.SetStyleValue(breakType);
            if (newStyleSheetValue.OverflowBehavior is {} overflowBehavior) _overflowBehavior.SetStyleValue(overflowBehavior);
            if (newStyleSheetValue.HorizontalAlignment is {} horizontalAlignment) _horizontalAlignment.SetStyleValue(horizontalAlignment);
            if (newStyleSheetValue.VerticalAlignment is {} verticalAlignment) _verticalAlignment.SetStyleValue(verticalAlignment);
        }
    }

    [ClientOnly]
    protected void RelayoutText(Vector2 size)
    {
        IEnumerable<ComplexTextMetrics.FlattenedRichText> flattened;
        if (!HasComplexContent)
        {
            if (!string.IsNullOrEmpty(Text))
            {
                flattened = [Text];
            }
            else
            {
                LaidOutComplexText = new ComplexTextMetrics.RichTextContainer([], Vector2.Zero);
                return;
            }
        }
        else
        {
            flattened = ComplexTextMetrics.FlattenText(Elements.OfType<IRichTextElement>());
        }
        
        var font = new Font(FontFamily, FontStyle, FontSize);
        if (OverflowBehavior is not OverflowBehavior.Stretch and not OverflowBehavior.None && BreakType is not BreakType.None)
        {
            flattened = ComplexTextMetrics.LayoutText(font, flattened, new Vector2(size.X, size.Y), BreakType, OverflowBehavior);
        }
        var measurements = ComplexTextMetrics.MeasureRichText(flattened, font);

        if (OverflowBehavior is OverflowBehavior.Stretch)
        {
            _width.SetOverrideValue(measurements.Size.X);
            _height.SetOverrideValue(measurements.Size.Y);
        }

        LaidOutComplexText = measurements;

        Invalidated = false;
    }

    protected virtual void OnInvalidated()
    {
    }

    [ClientOnly]
    protected override void RenderContent(Vector2 position, Vector2 size)
    {
        base.RenderContent(position, size);
        
        if (HasNewLayout && OverflowBehavior is not OverflowBehavior.Stretch and not OverflowBehavior.None && BreakType is not BreakType.None)
        {
            Invalidated = true;
            HasNewLayout = false;
        }
        
        if (Invalidated)
        {
            OnInvalidated();
            RelayoutText(size);
        }

        Debug.Assert(LaidOutComplexText != null, "Complex text layout should have been calculated in RelayoutText method.");

        if (LaidOutComplexText.Value.Elements.Count == 0)
        {
            return;
        }

        var basePosition = position;
        ComplexTextMetrics.AlignBounds(LaidOutComplexText.Value.Size, (int)size.X, (int)size.Y, HorizontalAlignment, VerticalAlignment, ref basePosition.X, ref basePosition.Y);

        foreach (var element in LaidOutComplexText.Value.Elements)
        {
            G.SetFont(element.Font with { Size = (element.FontSize ?? FontSize) * G.Scale });
            if ((element.Background ?? Background) is { } background)
            {
                G.SetColor(background);
                G.FillRect((int)basePosition.X, (int)basePosition.Y, (int)element.Size.X, (int)element.Size.Y);
            }

            float yOff = 0;
            if (VerticalAlignment == TextVerticalAlignment.Center)
            {
                yOff = (G.GetFontMetrics().LineHeight / 2.0f);
            }
            else if (VerticalAlignment == TextVerticalAlignment.Top)
            {
                yOff = G.GetFontMetrics().LineHeight;
            }

            int x = (int)(basePosition.X + (element.Position.X * G.Scale));
            int y = (int)(basePosition.Y + (element.Position.Y * G.Scale) + yOff);

            if ((element.Stroke ?? Stroke) is { } stroke)
            {
                G.SetColor(stroke);
                G.DrawStringStroke(element.Text, x, y);
            }
            
            G.SetColor(element.Foreground ?? Foreground);
            G.DrawString(element.Text, x, y);
        }
    }


    public void Invalidate()
    {
        Invalidated = true;
    }
}