using System.Numerics;
using Microsoft.Xna.Framework;
using NFMWorld.DriverInterface;

namespace NFMWorld.Reactor;

/// <summary>
/// Represents a box element with solid colors for border, background, and content.
/// </summary>
public class PaintedBox : FlexPanel
{
    internal Property<Color?> _borderColor;
    internal Property<Color?> _backgroundColor;
    internal Property<float> _borderTopLeftRadius;
    internal Property<float> _borderTopRightRadius;
    internal Property<float> _borderBottomLeftRadius;
    internal Property<float> _borderBottomRightRadius;

    public PaintedBox()
    {
        _borderColor = null;
        _backgroundColor = null;
        _borderTopLeftRadius = 0f;
        _borderTopRightRadius = 0f;
        _borderBottomLeftRadius = 0f;
        _borderBottomRightRadius = 0f;
    }

    public override bool DebugIsContentfulNode => BackgroundColor != null || BorderColor != null;

    public Color? BorderColor
    {
        get => _borderColor.ComputedValue;
        set => _borderColor.SetOverrideValue(value);
    }
    public Color? BackgroundColor
    {
        get => _backgroundColor.ComputedValue;
        set => _backgroundColor.SetOverrideValue(value);
    }

    [Property]
    public float BorderRadius
    {
        get => BorderTopLeftRadius == BorderTopRightRadius && BorderTopLeftRadius == BorderBottomLeftRadius && BorderTopLeftRadius == BorderBottomRightRadius
            ? BorderTopLeftRadius
            : 0;
        set
        {
            _borderTopLeftRadius.SetOverrideValue(value);
            _borderTopRightRadius.SetOverrideValue(value);
            _borderBottomLeftRadius.SetOverrideValue(value);
            _borderBottomRightRadius.SetOverrideValue(value);
        }
    }

    public float BorderTopLeftRadius
    {
        get => _borderTopLeftRadius.ComputedValue;
        set => _borderTopLeftRadius.SetOverrideValue(value);
    }
    public float BorderTopRightRadius
    {
        get => _borderTopRightRadius.ComputedValue;
        set => _borderTopRightRadius.SetOverrideValue(value);
    }
    public float BorderBottomLeftRadius
    {
        get => _borderBottomLeftRadius.ComputedValue;
        set => _borderBottomLeftRadius.SetOverrideValue(value);
    }
    public float BorderBottomRightRadius
    {
        get => _borderBottomRightRadius.ComputedValue;
        set => _borderBottomRightRadius.SetOverrideValue(value);
    }

    protected override void UpdateStyles(StyleSheetStyles? oldStyleSheet, StyleSheetStyles? newStyleSheet)
    {
        base.UpdateStyles(oldStyleSheet, newStyleSheet);

        if (oldStyleSheet is { } oldStyleSheetValue)
        {
            if (oldStyleSheetValue.BorderColor is not null) _borderColor.ClearStyleValue();
            if (oldStyleSheetValue.BackgroundColor is not null) _backgroundColor.ClearStyleValue();
            if (oldStyleSheetValue.BorderRadius is not null) { _borderTopLeftRadius.ClearStyleValue(); _borderTopRightRadius.ClearStyleValue(); _borderBottomLeftRadius.ClearStyleValue(); _borderBottomRightRadius.ClearStyleValue(); }
            if (oldStyleSheetValue.BorderTopLeftRadius is not null) _borderTopLeftRadius.ClearStyleValue();
            if (oldStyleSheetValue.BorderTopRightRadius is not null) _borderTopRightRadius.ClearStyleValue();
            if (oldStyleSheetValue.BorderBottomLeftRadius is not null) _borderBottomLeftRadius.ClearStyleValue();
            if (oldStyleSheetValue.BorderBottomRightRadius is not null) _borderBottomRightRadius.ClearStyleValue();
        }
        
        if (newStyleSheet is { } newStyleSheetValue)
        {
            if (newStyleSheetValue.BorderColor is {} borderColor) _borderColor.SetStyleValue(borderColor);
            if (newStyleSheetValue.BackgroundColor is {} backgroundColor) _backgroundColor.SetStyleValue(backgroundColor);
            if (newStyleSheetValue.BorderRadius is {} borderRadius) { _borderTopLeftRadius.SetStyleValue(borderRadius); _borderTopRightRadius.SetStyleValue(borderRadius); _borderBottomLeftRadius.SetStyleValue(borderRadius); _borderBottomRightRadius.SetStyleValue(borderRadius); }
            if (newStyleSheetValue.BorderTopLeftRadius is {} borderTopLeftRadius) _borderTopLeftRadius.SetStyleValue(borderTopLeftRadius);
            if (newStyleSheetValue.BorderTopRightRadius is {} borderTopRightRadius) _borderTopRightRadius.SetStyleValue(borderTopRightRadius);
            if (newStyleSheetValue.BorderBottomLeftRadius is {} borderBottomLeftRadius) _borderBottomLeftRadius.SetStyleValue(borderBottomLeftRadius);
            if (newStyleSheetValue.BorderBottomRightRadius is {} borderBottomRightRadius) _borderBottomRightRadius.SetStyleValue(borderBottomRightRadius);
        }
    }

    protected override void RenderBackground(Vector2 position, Vector2 size)
    {
        if (BackgroundColor is {} backgroundColor && backgroundColor != Color.Transparent)
        {
            var avgBorder = (BorderTop ?? 0) + (BorderLeft ?? 0) + (BorderBottom ?? 0) + (BorderRight ?? 0) / 4f;

            G.SetColor(backgroundColor);
            var radTopLeft = Math.Max(0, BorderTopLeftRadius - ((BorderTop ?? 0) + (BorderLeft ?? 0) / 2f));
            var radTopRight = Math.Max(0, BorderTopRightRadius - ((BorderTop ?? 0) + (BorderRight ?? 0) / 2f));
            var radBottomRight = Math.Max(0, BorderBottomRightRadius - ((BorderBottom ?? 0) + (BorderRight ?? 0) / 2f));
            var radBottomLeft = Math.Max(0, BorderBottomLeftRadius - ((BorderBottom ?? 0) + (BorderLeft ?? 0) / 2f));
            G.FillRoundedRect((int)(position.X - avgBorder / 2), (int)(position.Y - avgBorder / 2), (int)(size.X + avgBorder), (int)(size.Y + avgBorder), radTopLeft * G.Scale, radTopRight * G.Scale, radBottomRight * G.Scale, radBottomLeft * G.Scale);
        }
        
    }
    
    protected override void RenderBorder(Vector2 position, Vector2 size)
    {
        if (BorderColor is { } borderColor && borderColor != Color.Transparent)
        {
            G.SetColor(borderColor);
            var avgBorder = (BorderTop ?? 0) + (BorderLeft ?? 0) + (BorderBottom ?? 0) + (BorderRight ?? 0) / 4f;

            if (avgBorder > 0)
            {
                G.SetStrokeWidth(avgBorder);
                var radTopLeft = BorderTopLeftRadius;
                var radTopRight = BorderTopRightRadius;
                var radBottomRight = BorderBottomRightRadius;
                var radBottomLeft = BorderBottomLeftRadius;
                G.DrawRoundedRect((int)(position.X), (int)(position.Y), (int)size.X, (int)size.Y, radTopLeft * G.Scale, radTopRight * G.Scale, radBottomRight * G.Scale, radBottomLeft * G.Scale);
                G.SetStrokeWidth();
            }
        }
    }
}