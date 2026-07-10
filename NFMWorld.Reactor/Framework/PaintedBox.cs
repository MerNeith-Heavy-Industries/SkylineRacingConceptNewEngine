using System.Numerics;
using Microsoft.Xna.Framework;
using NFMWorld.DriverInterface;

namespace NFMWorld.Reactor;

/// <summary>
/// Represents a box element with solid colors for border, background, and content.
/// </summary>
public class PaintedBox : FlexPanel
{
    public StyledProperty<Color?> BorderColor = new(null);
    public StyledProperty<Color?> BackgroundColor = new(null);
    public StyledProperty<float> BorderTopLeftRadius = new(0f);
    public StyledProperty<float> BorderTopRightRadius = new(0f);
    public StyledProperty<float> BorderBottomLeftRadius = new(0f);
    public StyledProperty<float> BorderBottomRightRadius = new(0f);

    public override bool DebugIsContentfulNode => BackgroundColor != null || BorderColor != null;

    [Property]
    public float BorderRadius
    {
        get => BorderTopLeftRadius.OverrideValue == BorderTopRightRadius.OverrideValue && BorderTopLeftRadius.OverrideValue == BorderBottomLeftRadius.OverrideValue && BorderTopLeftRadius.OverrideValue == BorderBottomRightRadius.OverrideValue
            ? BorderTopLeftRadius.OverrideValue
            : 0;
        set
        {
            BorderTopLeftRadius.OverrideValue = value;
            BorderTopRightRadius.OverrideValue = value;
            BorderBottomLeftRadius.OverrideValue = value;
            BorderBottomRightRadius.OverrideValue = value;
        }
    }

    protected override void UpdateStyles(StyleSheetStyles? oldStyleSheet, StyleSheetStyles? newStyleSheet)
    {
        base.UpdateStyles(oldStyleSheet, newStyleSheet);

        if (oldStyleSheet is { } oldStyleSheetValue)
        {
            if (oldStyleSheetValue.BorderColor is not null) BorderColor.ClearStyleValue();
            if (oldStyleSheetValue.BackgroundColor is not null) BackgroundColor.ClearStyleValue();
            if (oldStyleSheetValue.BorderRadius is not null) { BorderTopLeftRadius.ClearStyleValue(); BorderTopRightRadius.ClearStyleValue(); BorderBottomLeftRadius.ClearStyleValue(); BorderBottomRightRadius.ClearStyleValue(); }
            if (oldStyleSheetValue.BorderTopLeftRadius is not null) BorderTopLeftRadius.ClearStyleValue();
            if (oldStyleSheetValue.BorderTopRightRadius is not null) BorderTopRightRadius.ClearStyleValue();
            if (oldStyleSheetValue.BorderBottomLeftRadius is not null) BorderBottomLeftRadius.ClearStyleValue();
            if (oldStyleSheetValue.BorderBottomRightRadius is not null) BorderBottomRightRadius.ClearStyleValue();
        }
        
        if (newStyleSheet is { } newStyleSheetValue)
        {
            if (newStyleSheetValue.BorderColor is {} borderColor) BorderColor.StyleValue = borderColor;
            if (newStyleSheetValue.BackgroundColor is {} backgroundColor) BackgroundColor.StyleValue = backgroundColor;
            if (newStyleSheetValue.BorderRadius is {} borderRadius) { BorderTopLeftRadius.StyleValue = borderRadius; BorderTopRightRadius.StyleValue = borderRadius; BorderBottomLeftRadius.StyleValue = borderRadius; BorderBottomRightRadius.StyleValue = borderRadius; }
            if (newStyleSheetValue.BorderTopLeftRadius is {} borderTopLeftRadius) BorderTopLeftRadius.StyleValue = borderTopLeftRadius;
            if (newStyleSheetValue.BorderTopRightRadius is {} borderTopRightRadius) BorderTopRightRadius.StyleValue = borderTopRightRadius;
            if (newStyleSheetValue.BorderBottomLeftRadius is {} borderBottomLeftRadius) BorderBottomLeftRadius.StyleValue = borderBottomLeftRadius;
            if (newStyleSheetValue.BorderBottomRightRadius is {} borderBottomRightRadius) BorderBottomRightRadius.StyleValue = borderBottomRightRadius;
        }
    }

    protected override void RenderBackground(Vector2 position, Vector2 size)
    {
        if (BackgroundColor.ComputedValue is {} backgroundColor && backgroundColor != Color.Transparent)
        {
            var avgBorder = (BorderTop.ComputedValue ?? 0) + (BorderLeft.ComputedValue ?? 0) + (BorderBottom.ComputedValue ?? 0) + (BorderRight.ComputedValue ?? 0) / 4f;

            G.SetColor(backgroundColor);
            var radTopLeft = Math.Max(0, BorderTopLeftRadius.ComputedValue - ((BorderTop.ComputedValue ?? 0) + (BorderLeft.ComputedValue ?? 0) / 2f));
            var radTopRight = Math.Max(0, BorderTopRightRadius.ComputedValue - ((BorderTop.ComputedValue ?? 0) + (BorderRight.ComputedValue ?? 0) / 2f));
            var radBottomRight = Math.Max(0, BorderBottomRightRadius.ComputedValue - ((BorderBottom.ComputedValue ?? 0) + (BorderRight.ComputedValue ?? 0) / 2f));
            var radBottomLeft = Math.Max(0, BorderBottomLeftRadius.ComputedValue - ((BorderBottom.ComputedValue ?? 0) + (BorderLeft.ComputedValue ?? 0) / 2f));
            G.FillRoundedRect((int)(position.X - avgBorder / 2), (int)(position.Y - avgBorder / 2), (int)(size.X + avgBorder), (int)(size.Y + avgBorder), radTopLeft * G.Scale, radTopRight * G.Scale, radBottomRight * G.Scale, radBottomLeft * G.Scale);
        }
        
    }
    
    protected override void RenderBorder(Vector2 position, Vector2 size)
    {
        if (BorderColor.ComputedValue is { } borderColor && borderColor != Color.Transparent)
        {
            G.SetColor(borderColor);
            var avgBorder = (BorderTop.ComputedValue ?? 0) + (BorderLeft.ComputedValue ?? 0) + (BorderBottom.ComputedValue ?? 0) + (BorderRight.ComputedValue ?? 0) / 4f;

            if (avgBorder > 0)
            {
                G.SetStrokeWidth(avgBorder);
                var radTopLeft = BorderTopLeftRadius.ComputedValue;
                var radTopRight = BorderTopRightRadius.ComputedValue;
                var radBottomRight = BorderBottomRightRadius.ComputedValue;
                var radBottomLeft = BorderBottomLeftRadius.ComputedValue;
                G.DrawRoundedRect((int)(position.X), (int)(position.Y), (int)size.X, (int)size.Y, radTopLeft * G.Scale, radTopRight * G.Scale, radBottomRight * G.Scale, radBottomLeft * G.Scale);
                G.SetStrokeWidth();
            }
        }
    }
}