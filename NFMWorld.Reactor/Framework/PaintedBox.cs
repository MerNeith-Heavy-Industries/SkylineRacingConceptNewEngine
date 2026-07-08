using System.Numerics;
using Microsoft.Xna.Framework;
using NFMWorld.DriverInterface;

namespace NFMWorld.Reactor;

/// <summary>
/// Represents a box element with solid colors for border, background, and content.
/// </summary>
public class PaintedBox : FlexPanel
{
    public override bool DebugIsContentfulNode => BackgroundColor != null || BorderColor != null;

    [Property]
    public Color? BorderColor { get; set; }
    [Property]
    public Color? BackgroundColor { get; set; }

    [Property]
    public float BorderRadius
    {
        get => BorderTopLeftRadius == BorderTopRightRadius && BorderTopLeftRadius == BorderBottomLeftRadius && BorderTopLeftRadius == BorderBottomRightRadius
            ? BorderTopLeftRadius
            : 0;
        set
        {
            BorderTopLeftRadius = value;
            BorderTopRightRadius = value;
            BorderBottomLeftRadius = value;
            BorderBottomRightRadius = value;
        }
    }

    [Property]
    public float BorderTopLeftRadius { get; set; }
    [Property]
    public float BorderTopRightRadius { get; set; }
    [Property]
    public float BorderBottomLeftRadius { get; set; }
    [Property]
    public float BorderBottomRightRadius { get; set; }

    protected override void UpdateStyles(StyleSheetStyles? oldStyleSheet, StyleSheetStyles? newStyleSheet)
    {
        base.UpdateStyles(oldStyleSheet, newStyleSheet);

        if (oldStyleSheet is { } oldStyleSheetValue)
        {
            if (oldStyleSheetValue.BorderColor is not null) BorderColor = null;
            if (oldStyleSheetValue.BackgroundColor is not null) BackgroundColor = null;
            if (oldStyleSheetValue.BorderRadius is not null) BorderRadius = 0;
            if (oldStyleSheetValue.BorderTopLeftRadius is not null) BorderTopLeftRadius = 0;
            if (oldStyleSheetValue.BorderTopRightRadius is not null) BorderTopRightRadius = 0;
            if (oldStyleSheetValue.BorderBottomLeftRadius is not null) BorderBottomLeftRadius = 0;
            if (oldStyleSheetValue.BorderBottomRightRadius is not null) BorderBottomRightRadius = 0;
        }
        
        if (newStyleSheet is { } newStyleSheetValue)
        {
            if (newStyleSheetValue.BorderColor is {} borderColor) BorderColor = borderColor;
            if (newStyleSheetValue.BackgroundColor is {} backgroundColor) BackgroundColor = backgroundColor;
            if (newStyleSheetValue.BorderRadius is {} borderRadius) BorderRadius = borderRadius;
            if (newStyleSheetValue.BorderTopLeftRadius is {} borderTopLeftRadius) BorderTopLeftRadius = borderTopLeftRadius;
            if (newStyleSheetValue.BorderTopRightRadius is {} borderTopRightRadius) BorderTopRightRadius = borderTopRightRadius;
            if (newStyleSheetValue.BorderBottomLeftRadius is {} borderBottomLeftRadius) BorderBottomLeftRadius = borderBottomLeftRadius;
            if (newStyleSheetValue.BorderBottomRightRadius is {} borderBottomRightRadius) BorderBottomRightRadius = borderBottomRightRadius;
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