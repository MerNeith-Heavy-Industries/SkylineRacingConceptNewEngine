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
    /// <summary>
    /// Sets the background color of the text.
    /// </summary>
    public StyledProperty<Color?> Background;
    
    /// <summary>
    /// Sets the fill color of the text. The default value is white.
    /// </summary>
    public StyledProperty<Color> Foreground;
    
    /// <summary>
    /// Sets the stroke color of the text. Or set to null to disable the stroke.
    /// </summary>
    public StyledProperty<Color?> Stroke;
    
    /// <summary>
    /// Gets or sets the font family.
    /// </summary>
    public StyledProperty<FontFamily> FontFamily;
    
    /// <summary>
    /// Gets or sets the font size.
    /// </summary>
    public StyledProperty<float> FontSize;
    
    /// <summary>
    /// Gets or sets the font style.
    /// </summary>
    public StyledProperty<FontStyle> FontStyle;
    public StyledProperty<BreakType> BreakType;
    public StyledProperty<OverflowBehavior> OverflowBehavior;
    
    /// <summary>
    /// Sets the horizontal alignment of the text. The default value is <see cref="TextHorizontalAlignment.Left"/>.
    /// </summary>
    public StyledProperty<TextHorizontalAlignment> HorizontalAlignment;
    
    /// <summary>
    /// Sets the vertical alignment of the text. The default value is <see cref="TextVerticalAlignment.Top"/>.
    /// </summary>
    public StyledProperty<TextVerticalAlignment> VerticalAlignment;

    protected bool Invalidated { get; private set; }= true;
    public ComplexTextMetrics.RichTextContainer? LaidOutComplexText;

    public TextRun()
    {
        Background = new(null);
        Foreground = new(new Color(255, 255, 255));
        Stroke = new(null);
        FontFamily = new(
            DriverInterface.FontFamily.DroidSans,
            this,
            static (ctx, o, n) => ((TextRun)ctx!).Invalidate()
        );
        FontSize = new(
            12f,
            this,
            static (ctx, o, n) => ((TextRun)ctx!).Invalidate()
        );
        FontStyle = new(
            DriverInterface.FontStyle.Plain,
            this,
            static (ctx, o, n) => ((TextRun)ctx!).Invalidate()
        );
        BreakType = new(
            Reactor.BreakType.Word,
            this,
            static (ctx, o, n) => ((TextRun)ctx!).Invalidate()
        );
        OverflowBehavior = new(
            Reactor.OverflowBehavior.Stretch,
            this,
            static (ctx, o, n) => ((TextRun)ctx!).Invalidate()
        );
        HorizontalAlignment = new(TextHorizontalAlignment.Left);
        VerticalAlignment = new(TextVerticalAlignment.Top);
    }

    public override bool DebugIsContentfulNode => true;
    
    [Property]
    public TextElement[] Elements
    {
        get;
        set
        {
            field = value;
            Invalidate();
        }
    } = [];

    public bool HasComplexContent => Elements.Length > 0;

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

    protected override void UpdateStyles(StyleSheetStyles? oldStyleSheet, StyleSheetStyles? newStyleSheet)
    {
        base.UpdateStyles(oldStyleSheet, newStyleSheet);
        
        if (oldStyleSheet is { } oldStyleSheetValue)
        {
            if (oldStyleSheetValue.Background is not null) Background.ClearStyleValue();
            if (oldStyleSheetValue.Foreground is not null) Foreground.ClearStyleValue();
            if (oldStyleSheetValue.Stroke is not null) Stroke.ClearStyleValue();
            if (oldStyleSheetValue.FontFamily is not null) FontFamily.ClearStyleValue();
            if (oldStyleSheetValue.FontSize is not null) FontSize.ClearStyleValue();
            if (oldStyleSheetValue.FontStyle is not null) FontStyle.ClearStyleValue();
            if (oldStyleSheetValue.BreakType is not null) BreakType.ClearStyleValue();
            if (oldStyleSheetValue.OverflowBehavior is not null) OverflowBehavior.ClearStyleValue();
            if (oldStyleSheetValue.HorizontalAlignment is not null) HorizontalAlignment.ClearStyleValue();
            if (oldStyleSheetValue.VerticalAlignment is not null) VerticalAlignment.ClearStyleValue();
        }
        
        if (newStyleSheet is { } newStyleSheetValue)
        {
            if (newStyleSheetValue.Background is {} background) Background.StyleValue = background;
            if (newStyleSheetValue.Foreground is {} foreground) Foreground.StyleValue = foreground;
            if (newStyleSheetValue.Stroke is {} stroke) Stroke.StyleValue = stroke;
            if (newStyleSheetValue.FontFamily is {} fontFamily) FontFamily.StyleValue = fontFamily;
            if (newStyleSheetValue.FontSize is {} fontSize) FontSize.StyleValue = fontSize;
            if (newStyleSheetValue.FontStyle is {} fontStyle) FontStyle.StyleValue = fontStyle;
            if (newStyleSheetValue.BreakType is {} breakType) BreakType.StyleValue = breakType;
            if (newStyleSheetValue.OverflowBehavior is {} overflowBehavior) OverflowBehavior.StyleValue = overflowBehavior;
            if (newStyleSheetValue.HorizontalAlignment is {} horizontalAlignment) HorizontalAlignment.StyleValue = horizontalAlignment;
            if (newStyleSheetValue.VerticalAlignment is {} verticalAlignment) VerticalAlignment.StyleValue = verticalAlignment;
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
        
        var font = new Font(FontFamily.ComputedValue, FontStyle.ComputedValue, FontSize.ComputedValue);
        if (OverflowBehavior.ComputedValue is not Reactor.OverflowBehavior.Stretch and not Reactor.OverflowBehavior.None && BreakType.ComputedValue is not Reactor.BreakType.None)
        {
            flattened = ComplexTextMetrics.LayoutText(font, flattened, new Vector2(size.X, size.Y), BreakType.ComputedValue, OverflowBehavior.ComputedValue);
        }
        var measurements = ComplexTextMetrics.MeasureRichText(flattened, font);

        if (OverflowBehavior.ComputedValue is Reactor.OverflowBehavior.Stretch)
        {
            Width.OverrideValue = measurements.Size.X;
            Height.OverrideValue = measurements.Size.Y;
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
        
        if (HasNewLayout && OverflowBehavior.ComputedValue is not Reactor.OverflowBehavior.Stretch and not Reactor.OverflowBehavior.None && BreakType.ComputedValue is not Reactor.BreakType.None)
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
        ComplexTextMetrics.AlignBounds(LaidOutComplexText.Value.Size, (int)size.X, (int)size.Y, HorizontalAlignment.ComputedValue, VerticalAlignment.ComputedValue, ref basePosition.X, ref basePosition.Y);

        foreach (var element in LaidOutComplexText.Value.Elements)
        {
            G.SetFont(element.Font with { Size = (element.FontSize ?? FontSize.ComputedValue) * G.Scale });
            if ((element.Background ?? Background.ComputedValue) is { } background)
            {
                G.SetColor(background);
                G.FillRect((int)basePosition.X, (int)basePosition.Y, (int)element.Size.X, (int)element.Size.Y);
            }

            float yOff = 0;
            if (VerticalAlignment.ComputedValue == TextVerticalAlignment.Center)
            {
                yOff = (G.GetFontMetrics().LineHeight / 2.0f);
            }
            else if (VerticalAlignment.ComputedValue == TextVerticalAlignment.Top)
            {
                yOff = G.GetFontMetrics().LineHeight;
            }

            int x = (int)(basePosition.X + (element.Position.X * G.Scale));
            int y = (int)(basePosition.Y + (element.Position.Y * G.Scale) + yOff);

            if ((element.Stroke ?? Stroke.ComputedValue) is { } stroke)
            {
                G.SetColor(stroke);
                G.DrawStringStroke(element.Text, x, y);
            }
            
            G.SetColor(element.Foreground ?? Foreground.ComputedValue);
            G.DrawString(element.Text, x, y);
        }
    }


    public void Invalidate()
    {
        Invalidated = true;
    }
}