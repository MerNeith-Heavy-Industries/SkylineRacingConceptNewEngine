using System.Text;
using NFMWorld.DriverInterface;
using NFMWorld.Util;

namespace NFMWorld.UI;

public enum BreakType
{
    None,
    Word,
    Character
}

public enum OverflowBehavior
{
    ContinueVertically,
    ContinueHorizontally
}

public static class ComplexTextMetrics
{
    public static string LayoutText(IFontMetrics font, string text, Vector2 bounds, BreakType breakType = BreakType.Word, OverflowBehavior overflowBehavior = OverflowBehavior.ContinueHorizontally)
    {
        if (breakType == BreakType.None)
        {
            return text;
        }
        
        var sb = new StringBuilder(text.Length);
        var spaceWidth = font.MeasureText(" ").X;
        var lineWidth = 0.0f;
        
        var textHeight = 0f;

        foreach (var wordRange in text.AsSpan().Split(' '))
        {
            var word = text.AsSpan(wordRange);
            
            var wordSize = font.MeasureText(word);

            if (lineWidth + wordSize.X > bounds.X &&
                (textHeight + (wordSize.Y * 2) < bounds.Y || overflowBehavior == OverflowBehavior.ContinueVertically))
            {
                if (breakType == BreakType.Word)
                {
                    sb.Append('\n');
                    textHeight += wordSize.Y;
                    lineWidth = 0.0f;
                }
                else if (breakType == BreakType.Character)
                {
                    foreach (var ch in word)
                    {
                        var charWidth = font.MeasureText([ch]).X;
                        if (lineWidth + charWidth > bounds.X)
                        {
                            sb.Append('\n');
                            textHeight += wordSize.Y;
                            lineWidth = 0.0f;
                        }
                        sb.Append(ch);
                        lineWidth += charWidth;
                    }
                    sb.Append(' ');
                    lineWidth += spaceWidth;
                    continue;
                }
            }

            sb.Append(word).Append(' ');
            lineWidth += wordSize.X + spaceWidth;
        }

        return sb.ToString().TrimEnd();
    }

    public static RichTextContainer MeasureRichText(IEnumerable<IRichTextElement> elements, Font defaultFont)
    {
        var cursor = Vector2.Zero;
        float currentLineHeight = 0;
        float totalWidth = 0;
        List<PositionedRichText> positionedElements = [];
        
        foreach (var element in elements)
        {
            SubMeasure(element, defaultFont, ref currentLineHeight, ref totalWidth, ref cursor, positionedElements, null, null, null);
        }
        
        return new RichTextContainer(positionedElements, new Vector2(totalWidth, cursor.Y + currentLineHeight));

        static void SubMeasure(
            IRichTextElement element,
            Font font,
            ref float currentLineHeight,
            ref float totalWidth,
            ref Vector2 cursor,
            List<PositionedRichText> positionedElements,
            Color? foreground,
            Color? background,
            Color? stroke
        )
        {
            font = new Font(element.FontFamily ?? font.FontFamily, element.FontStyle ?? font.Style, element.FontSize ?? font.Size);
            foreground = element.Foreground ?? foreground;
            background = element.Background ?? background;
            stroke = element.Stroke ?? stroke;
            
            if (element is IRichTextLeaf leaf && !string.IsNullOrEmpty(leaf.Text))
            {
                var fontMetrics = G.GetFontMetrics(font);

                if (leaf.Text.Contains('\n'))
                {
                    var isFirst = true;
                    foreach (var range in leaf.Text.AsSpan().Split('\n'))
                    {
                        if (isFirst)
                        {
                            isFirst = false;
                        }
                        else
                        {
                            cursor.Y += currentLineHeight; // Move cursor down by the height of the line
                        }
                        
                        var lineSize = fontMetrics.MeasureText(leaf.Text.AsSpan(range));
                        positionedElements.Add(new PositionedRichText(cursor, lineSize, background, foreground, stroke, font, leaf.Text[range]));

                        cursor.X = lineSize.X; // Move cursor to the end of the line
                        currentLineHeight = lineSize.Y; // Store the height of the current line
                        
                        totalWidth = Math.Max(totalWidth, cursor.X); // Update total width if the current line is wider
                    }
                }
                else
                {
                    var lineSize = fontMetrics.MeasureText(leaf.Text);
                    positionedElements.Add(new PositionedRichText(cursor, lineSize, background, foreground, stroke, font, leaf.Text));

                    cursor.X += lineSize.X; // Move cursor to the end of the line
                    currentLineHeight = Math.Max(currentLineHeight, lineSize.Y); // Update the previous line height if the current line is taller
                    
                    totalWidth = Math.Max(totalWidth, cursor.X); // Update total width if the current line is wider
                }
            }
            else if (element is IRichTextContainer container)
            {
                foreach (var child in container.Children)
                {
                    SubMeasure(child, font, ref currentLineHeight, ref totalWidth, ref cursor, positionedElements, foreground, background, stroke);
                }
            }
        }
    }
    
    public static void AlignBounds(Vector2 sz, int areaWidth, int areaHeight, TextHorizontalAlignment hAlign, TextVerticalAlignment vAlign, ref float x, ref float y)
    {
        if (hAlign != TextHorizontalAlignment.Left)
        {
            if (hAlign == TextHorizontalAlignment.Center)
            {
                x += areaWidth / 2f;
                x -= sz.X / 2f;
            }
            else if (hAlign == TextHorizontalAlignment.Right)
            {
                x += areaWidth;
                x -= sz.X;
            }
        }
            
        if (vAlign == TextVerticalAlignment.Center)
        {
            y += areaHeight / 2f;
        }
        else if (vAlign == TextVerticalAlignment.Bottom)
        {
            y += areaHeight;
        }
    }

    public readonly struct RichTextContainer(IReadOnlyList<PositionedRichText> elements, Vector2 size)
    {
        public IReadOnlyList<PositionedRichText> Elements { get; } = elements;
        public Vector2 Size { get; } = size;
    }

    public readonly record struct PositionedRichText(
        Vector2 Position,
        Vector2 Size,
        Color? Background,
        Color? Foreground,
        Color? Stroke,
        Font Font,
        string Text);
}
