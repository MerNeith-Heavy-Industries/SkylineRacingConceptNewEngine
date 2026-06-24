using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework;
using NFMWorldLibrary.Backend.Gamemodes;
using WorldXaml.UI.Base;
using WorldXaml.UI.Metadata;
using WorldXaml.UI.Yoga;

namespace NFMWorld.DriverInterface.UI;

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
    private bool _invalidated = true;
    protected ComplexTextMetrics.RichTextContainer? LaidOutComplexText;

    /// <summary>
    /// Sets the background color of the text.
    /// </summary>
    [Property]
    public Color? Background { get; set; }

    /// <summary>
    /// Sets the fill color of the text. The default value is white.
    /// </summary>
    [Property]
    public Color Foreground { get; set; } = new(255, 255, 255);
    
    /// <summary>
    /// Sets the stroke color of the text. Or set to null to disable the stroke.
    /// </summary>
    [Property]
    public Color? Stroke { get; set; }

    /// <summary>
    /// Gets or sets the font family.
    /// </summary>
    [Property]
    public FontFamily FontFamily
    {
        get;
        set
        {
            field = value;
            Invalidate();
        }
    } = FontFamily.DroidSans;

    /// <summary>
    /// Gets or sets the font size.
    /// </summary>
    [Property]
    public float FontSize
    {
        get;
        set
        {
            field = value;
            Invalidate();
        }
    } = 12;

    /// <summary>
    /// Gets or sets the font style.
    /// </summary>
    [Property]
    public FontStyle FontStyle
    {
        get;
        set
        {
            field = value;
            Invalidate();
        }
    } = FontStyle.Plain;

    [Property]
    public TextElement[] Elements { get; set; } = [];

    public bool HasComplexContent => Elements.Length > 0;

    [Property]
    public BreakType BreakType
    {
        get;
        set
        {
            field = value;
            Invalidate();
        }
    } = BreakType.Word;

    [Property]
    public OverflowBehavior OverflowBehavior
    {
        get;
        set
        {
            field = value;
            Invalidate();
        }
    } = OverflowBehavior.Stretch;

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
    [Property]
    public TextHorizontalAlignment HorizontalAlignment { get; set; } = TextHorizontalAlignment.Left;

    /// <summary>
    /// Sets the vertical alignment of the text. The default value is <see cref="TextVerticalAlignment.Top"/>.
    /// </summary>
    [Property]
    public TextVerticalAlignment VerticalAlignment { get; set; } = TextVerticalAlignment.Top;

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
            Width = measurements.Size.X;
            Height = measurements.Size.Y;
        }

        LaidOutComplexText = measurements;

        _invalidated = false;
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
            _invalidated = true;
            HasNewLayout = false;
        }
        
        if (_invalidated)
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
            G.SetFont(element.Font with { Size = element.FontSize ?? FontSize * G.Scale });
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
        _invalidated = true;
    }
}