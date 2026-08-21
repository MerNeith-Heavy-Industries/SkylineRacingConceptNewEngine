using System.Diagnostics;
using System.Numerics;
using Microsoft.Xna.Framework;
using NFMWorld.DriverInterface;
using NFMWorld.DriverInterface.DriverInterface;
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

public class TextRun : Component
{
    protected bool Invalidated { get; private set; }= true;
    public ComplexTextMetrics.RichTextContainer? LaidOutComplexText;

    public override bool DebugIsContentfulNode => true;
    
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

    public TextStyles TextStyles
    {
        get;
        set
        {
            field = value;
            Invalidate();
        }
    }

    protected override void OnStylesChanged()
    {
        base.OnStylesChanged();
        Invalidate();
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
        
        var font = new Font(TextStyles.FontFamily, TextStyles.FontStyle, TextStyles.FontSize);
        if (TextStyles.OverflowBehavior is not OverflowBehavior.Stretch and not OverflowBehavior.None && TextStyles.BreakType is not BreakType.None)
        {
            flattened = ComplexTextMetrics.LayoutText(font, flattened, new Vector2(size.X, size.Y), TextStyles.BreakType, TextStyles.OverflowBehavior);
        }
        var measurements = ComplexTextMetrics.MeasureRichText(flattened, font);

        if (TextStyles.OverflowBehavior is OverflowBehavior.Stretch)
        {
            Styles = Styles with { Width = measurements.Size.X, Height = measurements.Size.Y };
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
        
        if (HasNewLayout && TextStyles.OverflowBehavior is not Reactor.OverflowBehavior.Stretch and not Reactor.OverflowBehavior.None && TextStyles.BreakType is not Reactor.BreakType.None)
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
        ComplexTextMetrics.AlignBounds(LaidOutComplexText.Value.Size, (int)size.X, (int)size.Y, TextStyles.HorizontalAlignment, TextStyles.VerticalAlignment, ref basePosition.X, ref basePosition.Y);

        foreach (var element in LaidOutComplexText.Value.Elements)
        {
            G.SetFont(element.Font with { Size = (element.FontSize ?? TextStyles.FontSize) * G.Scale });
            if ((element.Background ?? Styles.BackgroundColor) is { } background)
            {
                G.SetColor(background);
                G.FillRect((int)basePosition.X, (int)basePosition.Y, (int)element.Size.X, (int)element.Size.Y);
            }

            float yOff = 0;
            if (TextStyles.VerticalAlignment == TextVerticalAlignment.Center)
            {
                yOff = (G.GetFontMetrics().LineHeight / 2.0f);
            }
            else if (TextStyles.VerticalAlignment == TextVerticalAlignment.Top)
            {
                yOff = G.GetFontMetrics().LineHeight;
            }

            int x = (int)(basePosition.X + (element.Position.X * G.Scale));
            int y = (int)(basePosition.Y + (element.Position.Y * G.Scale) + yOff);

            if ((element.Stroke ?? TextStyles.StrokeColor) is { } stroke)
            {
                G.SetColor(stroke);
                G.DrawStringStroke(element.Text, x, y);
            }
            
            G.SetColor(element.Foreground ?? TextStyles.ForegroundColor);
            G.DrawString(element.Text, x, y);
        }
    }

    public void Invalidate()
    {
        Invalidated = true;
    }
}