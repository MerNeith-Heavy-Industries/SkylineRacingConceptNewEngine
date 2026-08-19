using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ClaySharp;
using nfm_world_library.Lua;
using NFMWorld.DriverInterface.DriverInterface;

namespace NFMWorld.ClayDom;

[LuaVisible]
public enum NodeType
{
    Element,
    TextElement,
    Text,
}

[LuaVisible]
public abstract partial class ClayNode
{
    [LuaName]
    public abstract NodeType NodeType { get; }
}

[LuaVisible]
public abstract partial class ClayElementBase : ClayNode
{
    private protected static uint MaxId;

    public uint Id = MaxId++;
    public string DebugName = string.Empty;

    public List<ClayNode>? Children = null;

    // id is the only thing that matter, stringId is shown in DebugView
    public Clay.ElementId ElementId => new Clay.ElementId()
    {
        Id = Id,
        StringId = DebugName
    };

    protected virtual void OnChildrenChanged()
    {
    }

    [LuaName]
    public void AppendChild(ClayNode node)
    {
        (Children ??= []).Add(node);
        OnChildrenChanged();
    }

    [LuaName]
    public void InsertBefore(ClayNode node, ClayNode beforeNode)
    {
        if (Children?.IndexOf(beforeNode) is { } idx and > -1)
        {
            Children.Insert(idx, node);
        }
        else
        {
            AppendChild(node);
        }
        OnChildrenChanged();
    }

    [LuaName]
    public void RemoveChild(ClayNode node)
    {
        if (Children?.Remove(node) == true)
        {
            OnChildrenChanged();
        }
    }

    internal abstract void LayoutSelfAndChildren();

    [LuaName]
    public abstract void SetProperty(string key, object value);
    
    // ---------------- Parsers ----------------

    private protected static string ToCss(object value) => value switch
    {
        null => throw new ArgumentException("Property value is null."),
        string s => s,
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? throw new ArgumentException("Unsupported property value type."),
    };

    private protected static Clay.Color ParseColor(object value)
    {
        string s = ToCss(value).Trim();
        if (s.Length == 0)
            throw new ArgumentException("Empty color value.");
        if (s[0] == '#')
            return ParseHexColor(s);
        if (s.StartsWith("rgb", StringComparison.OrdinalIgnoreCase))
            return ParseRgbColor(s);
        if (NamedColors.TryGetValue(s, out var named))
            return named;
        throw new ArgumentException($"Unsupported color: '{s}'.");
    }

    private protected static Clay.Color ParseHexColor(string s)
    {
        var hex = s.AsSpan(1).Trim();
        return hex.Length switch
        {
            3 => new Clay.Color(HexNibble(hex[0]) * 17f, HexNibble(hex[1]) * 17f, HexNibble(hex[2]) * 17f, 255f),
            4 => new Clay.Color(HexNibble(hex[0]) * 17f, HexNibble(hex[1]) * 17f, HexNibble(hex[2]) * 17f, HexNibble(hex[3]) * 17f),
            6 => new Clay.Color(HexByte(hex[0], hex[1]), HexByte(hex[2], hex[3]), HexByte(hex[4], hex[5]), 255f),
            8 => new Clay.Color(HexByte(hex[0], hex[1]), HexByte(hex[2], hex[3]), HexByte(hex[4], hex[5]), HexByte(hex[6], hex[7])),
            _ => throw new ArgumentException($"Invalid hex color: '{s}'."),
        };

        static float HexNibble(char c) => Convert.ToInt32(c.ToString(), 16);
        static float HexByte(char hi, char lo) => Convert.ToInt32($"{hi}{lo}", 16);
    }

    private protected static Clay.Color ParseRgbColor(string s)
    {
        int open = s.IndexOf('(');
        int close = s.LastIndexOf(')');
        if (open < 0 || close <= open)
            throw new ArgumentException($"Invalid rgb() color: '{s}'.");

        string inner = s[(open + 1)..close];
        string[] parts = inner.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length is not (3 or 4))
            throw new ArgumentException($"Invalid rgb() color: '{s}'.");

        float r = ParseChannel(parts[0], s);
        float g = ParseChannel(parts[1], s);
        float b = ParseChannel(parts[2], s);
        float a = parts.Length == 4 ? ParseAlpha(parts[3], s) : 255f;
        return new Clay.Color(r, g, b, a);

        static float ParseChannel(string v, string full)
        {
            var span = v.AsSpan().Trim();
            if (span.EndsWith("%", StringComparison.Ordinal))
            {
                float p = float.Parse(span[..^1], NumberStyles.Float, CultureInfo.InvariantCulture);
                return Math.Clamp(p / 100f * 255f, 0f, 255f);
            }
            float n = float.Parse(span, NumberStyles.Float, CultureInfo.InvariantCulture);
            return Math.Clamp(n, 0f, 255f);
        }

        static float ParseAlpha(string v, string full)
        {
            var span = v.AsSpan().Trim();
            if (span.EndsWith("%", StringComparison.Ordinal))
            {
                float p = float.Parse(span[..^1], NumberStyles.Float, CultureInfo.InvariantCulture);
                return Math.Clamp(p / 100f * 255f, 0f, 255f);
            }
            float n = float.Parse(span, NumberStyles.Float, CultureInfo.InvariantCulture);
            return n <= 1f ? n * 255f : Math.Clamp(n, 0f, 255f);
        }
    }

    private protected static bool IsColorToken(string token)
    {
        if (token.StartsWith('#')) return true;
        if (token.StartsWith("rgb", StringComparison.OrdinalIgnoreCase)) return true;
        return NamedColors.ContainsKey(token);
    }

    private protected static float ParsePixels(object value, string prop)
    {
        string s = ToCss(value).Trim();
        var span = s.AsSpan();
        if (span.EndsWith("px", StringComparison.OrdinalIgnoreCase))
            span = span[..^2].Trim();
        if (span.Length == 0)
            throw new ArgumentException($"Invalid length for '{prop}': '{s}'.");
        if (span.IndexOf('%') >= 0)
            throw new ArgumentException($"'{prop}' does not accept percentages (expected pixels).");
        return float.Parse(span, NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    private protected static ushort ToUshort(float v, string prop)
    {
        if (v < 0 || v > ushort.MaxValue)
            throw new ArgumentException($"Value for '{prop}' out of range (0-{ushort.MaxValue}): {v}.");
        return (ushort)MathF.Round(v);
    }

    private protected static int ParseInt(object value, string prop)
    {
        string s = ToCss(value).Trim();
        return int.Parse(s, NumberStyles.Integer, CultureInfo.InvariantCulture);
    }

    private protected static readonly Dictionary<string, Clay.Color> NamedColors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["transparent"] = new Clay.Color(0, 0, 0, 0),
        ["black"] = new Clay.Color(0, 0, 0, 255),
        ["white"] = new Clay.Color(255, 255, 255, 255),
        ["red"] = new Clay.Color(255, 0, 0, 255),
        ["green"] = new Clay.Color(0, 128, 0, 255),
        ["lime"] = new Clay.Color(0, 255, 0, 255),
        ["blue"] = new Clay.Color(0, 0, 255, 255),
        ["yellow"] = new Clay.Color(255, 255, 0, 255),
        ["cyan"] = new Clay.Color(0, 255, 255, 255),
        ["aqua"] = new Clay.Color(0, 255, 255, 255),
        ["magenta"] = new Clay.Color(255, 0, 255, 255),
        ["fuchsia"] = new Clay.Color(255, 0, 255, 255),
        ["gray"] = new Clay.Color(128, 128, 128, 255),
        ["grey"] = new Clay.Color(128, 128, 128, 255),
        ["orange"] = new Clay.Color(255, 165, 0, 255),
        ["purple"] = new Clay.Color(128, 0, 128, 255),
        ["pink"] = new Clay.Color(255, 192, 203, 255),
        ["brown"] = new Clay.Color(165, 42, 42, 255),
        ["silver"] = new Clay.Color(192, 192, 192, 255),
        ["gold"] = new Clay.Color(255, 215, 0, 255),
        ["navy"] = new Clay.Color(0, 0, 128, 255),
        ["teal"] = new Clay.Color(0, 128, 128, 255),
    };
    
    private protected static Clay.TextAlignment ParseTextAlignment(object value) =>
        ToCss(value).Trim().ToLowerInvariant() switch
        {
            "left" or "start" => Clay.TextAlignment.Left,
            "center" => Clay.TextAlignment.Center,
            "right" or "end" => Clay.TextAlignment.Right,
            _ => throw new ArgumentException($"Unsupported 'text-align' value: '{value}'. Clay supports left/center/right."),
        };

    private protected static Clay.TextElementConfigWrapMode ParseWrapMode(object value) =>
        ToCss(value).Trim().ToLowerInvariant() switch
        {
            "normal" or "wrap" or "words" => Clay.TextElementConfigWrapMode.Words,
            "nowrap" or "none" => Clay.TextElementConfigWrapMode.None,
            "pre" or "pre-wrap" or "newlines" => Clay.TextElementConfigWrapMode.Newlines,
            _ => throw new ArgumentException($"Unsupported 'white-space' value: '{value}'. Clay supports normal/nowrap/pre."),
        };

    private protected static FontStyle ParseFontStyle(object value)
    {
        var style = ToCss(value);
        switch (style)
        {
            case "normal":
                return FontStyle.Plain;
            case "italic":
                return FontStyle.Italic;
            case "bold":
                return FontStyle.Bold;
            default:
                throw new ArgumentException("Value for 'font-style' must be normal, italic, or bold.");
        }
    }
}

[LuaVisible]
public partial class ClayElement : ClayElementBase
{
    public override NodeType NodeType => NodeType.Element;

    public Clay.LayoutConfig Layout; // Controls the size and position of an element and its children.
    public Clay.Color BackgroundColor; // Background color; generates a RECTANGLE render command (or is passed to IMAGE/CUSTOM).
    public Clay.Color OverlayColor; // "Color Overlay" applied to this element and all its children.
    public Clay.CornerRadiusValues CornerRadius; // Corner rounding of rectangles, borders and images.
    public Clay.AspectRatioElementConfig AspectRatio; // Aspect ratio scaling.
    public Clay.ImageElementConfig Image; // Image element settings.
    public Clay.FloatingElementConfig Floating; // Floating / absolute positioning settings.
    public Clay.CustomElementConfig Custom; // CUSTOM render command settings.
    public Clay.ClipElementConfig Clip; // Clip / scroll settings.
    public Clay.BorderElementConfig Border; // Border settings.
    public Clay.TransitionElementConfig Transition; // Transition settings.
    public object? UserData; // Transparently passed through to resulting render commands.

    // ---- Text ----
    public Clay.Color TextColor; // The RGBA color of the font to render, conventionally specified as 0-255.
    public ushort FontId; // An integer transparently passed to the measure text function to identify the font to use.
    public ushort FontSize; // Controls the size of the font.
    public ushort LetterSpacing; // Controls extra horizontal spacing between characters.
    public ushort LineHeight; // Controls additional vertical space between wrapped lines of text.
    public Clay.TextElementConfigWrapMode WrapMode; // How text wraps.
    public Clay.TextAlignment TextAlignment; // How wrapped lines are horizontally aligned.

    [LuaName]
    public ClayElement()
    {
    }

    [LuaName]
    public override void SetProperty(string key, object value)
    {
        switch (key.Trim().ToLowerInvariant())
        {
            // ---- Clay-specific ----
            case "image":
                Image.ImageData = ToCss(value);
                break;
            case "name":
                DebugName = ToCss(value);
                break;
            case "data":
                UserData = value;
                break;
            
            // ---- Sizing ----
            case "Width":
                Layout.Sizing.Width = ParseSizing(value, "Width");
                break;
            case "height":
                Layout.Sizing.Height = ParseSizing(value, "height");
                break;
            case "min-Width":
                Layout.Sizing.Width.MinMax.Min = ParsePixels(value, "min-Width");
                break;
            case "max-Width":
                Layout.Sizing.Width.MinMax.Max = ParsePixels(value, "max-Width");
                break;
            case "min-height":
                Layout.Sizing.Height.MinMax.Min = ParsePixels(value, "min-height");
                break;
            case "max-height":
                Layout.Sizing.Height.MinMax.Max = ParsePixels(value, "max-height");
                break;

            // ---- Box model ----
            case "padding":
            {
                var (t, r, b, l) = ParseEdgeValues(value, "padding");
                Layout.Padding = new Clay.Padding
                {
                    Top = ToUshort(t, "padding"),
                    Right = ToUshort(r, "padding"),
                    Bottom = ToUshort(b, "padding"),
                    Left = ToUshort(l, "padding"),
                };
                break;
            }
            case "padding-top":
                Layout.Padding.Top = ToUshort(ParsePixels(value, "padding-top"), "padding-top");
                break;
            case "padding-right":
                Layout.Padding.Right = ToUshort(ParsePixels(value, "padding-right"), "padding-right");
                break;
            case "padding-bottom":
                Layout.Padding.Bottom = ToUshort(ParsePixels(value, "padding-bottom"), "padding-bottom");
                break;
            case "padding-left":
                Layout.Padding.Left = ToUshort(ParsePixels(value, "padding-left"), "padding-left");
                break;
            case "gap":
            case "column-gap":
            case "row-gap":
                Layout.ChildGap = ToUshort(ParsePixels(value, key), key);
                break;
            case "margin":
            case "margin-top":
            case "margin-right":
            case "margin-bottom":
            case "margin-left":
                throw new ArgumentException($"Clay has no equivalent for '{key}' (no margin concept — use padding or gap instead).");

            // ---- Flex Layout ----
            case "flex-direction":
                Layout.LayoutDirection = ParseDirection(value);
                break;
            case "align-items":
            {
                // align-items = cross axis. row → y, column → x.
                if (Layout.LayoutDirection == Clay.LayoutDirection.LeftToRight)
                    Layout.ChildAlignment.Y = ParseAlignY(value, "align-items");
                else
                    Layout.ChildAlignment.X = ParseAlignX(value, "align-items");
                break;
            }
            case "justify-content":
            {
                // justify-content = main axis. row → x, column → y.
                if (Layout.LayoutDirection == Clay.LayoutDirection.LeftToRight)
                    Layout.ChildAlignment.X = ParseAlignX(value, "justify-content");
                else
                    Layout.ChildAlignment.Y = ParseAlignY(value, "justify-content");
                break;
            }

            // ---- Color ----
            case "background-color":
                BackgroundColor = ParseColor(value);
                break;
            case "overlay-color":
                OverlayColor = ParseColor(value);
                break;

            // ---- Border ----
            case "border":
                ApplyBorderShorthand(value);
                break;
            case "border-Width":
            {
                var (t, r, b, l) = ParseEdgeValues(value, "border-Width");
                Border.Width = new Clay.BorderWidth
                {
                    Top = ToUshort(t, "border-Width"),
                    Right = ToUshort(r, "border-Width"),
                    Bottom = ToUshort(b, "border-Width"),
                    Left = ToUshort(l, "border-Width"),
                    BetweenChildren = 0,
                };
                break;
            }
            case "border-top-Width":
                Border.Width.Top = ToUshort(ParsePixels(value, "border-top-Width"), "border-top-Width");
                break;
            case "border-right-Width":
                Border.Width.Right = ToUshort(ParsePixels(value, "border-right-Width"), "border-right-Width");
                break;
            case "border-bottom-Width":
                Border.Width.Bottom = ToUshort(ParsePixels(value, "border-bottom-Width"), "border-bottom-Width");
                break;
            case "border-left-Width":
                Border.Width.Left = ToUshort(ParsePixels(value, "border-left-Width"), "border-left-Width");
                break;
            case "border-color":
                Border.Color = ParseColor(value);
                break;
            case "border-style":
                if (!ToCss(value).Trim().Equals("solid", StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException($"Unsupported border-style '{value}' (Clay supports 'solid' only).");
                break;

            // ---- Corner radius ----
            case "border-radius":
            case "corner-radius":
            {
                var (t, r, b, l) = ParseEdgeValues(value, key);
                CornerRadius = new Clay.CornerRadiusValues
                {
                    TopLeft = t,
                    TopRight = r,
                    BottomRight = b,
                    BottomLeft = l,
                };
                break;
            }
            case "border-top-left-radius":
                CornerRadius.TopLeft = ParsePixels(value, "border-top-left-radius");
                break;
            case "border-top-right-radius":
                CornerRadius.TopRight = ParsePixels(value, "border-top-right-radius");
                break;
            case "border-bottom-right-radius":
                CornerRadius.BottomRight = ParsePixels(value, "border-bottom-right-radius");
                break;
            case "border-bottom-left-radius":
                CornerRadius.BottomLeft = ParsePixels(value, "border-bottom-left-radius");
                break;

            // ---- Positioning / floating ----
            case "position":
            {
                switch (ToCss(value).Trim().ToLowerInvariant())
                {
                    case "static":
                    case "relative":
                    case "initial":
                        Floating.AttachTo = Clay.FloatingAttachToElement.None;
                        break;
                    case "absolute":
                        Floating.AttachTo = Clay.FloatingAttachToElement.Parent;
                        break;
                    case "fixed":
                        Floating.AttachTo = Clay.FloatingAttachToElement.Root;
                        break;
                    default:
                        throw new ArgumentException($"Unsupported 'position' value: '{value}'.");
                }
                break;
            }
            case "top":
                EnsureFloating();
                Floating.Offset.Y = ParsePixels(value, "top");
                break;
            case "bottom":
                EnsureFloating();
                Floating.Offset.Y = -ParsePixels(value, "bottom");
                break;
            case "left":
                EnsureFloating();
                Floating.Offset.X = ParsePixels(value, "left");
                break;
            case "right":
                EnsureFloating();
                Floating.Offset.X = -ParsePixels(value, "right");
                break;
            case "z-index":
                Floating.ZIndex = (short)ParseInt(value, "z-index");
                break;
            case "aspect-ratio":
                AspectRatio.AspectRatio = ParseAspectRatio(value);
                break;

            // ---- Clip / scroll ----
            case "overflow":
            {
                switch (ToCss(value).Trim().ToLowerInvariant())
                {
                    case "visible":
                        Clip.Horizontal = false;
                        Clip.Vertical = false;
                        break;
                    case "hidden":
                    case "clip":
                    case "scroll":
                    case "auto":
                        Clip.Horizontal = true;
                        Clip.Vertical = true;
                        break;
                    default:
                        throw new ArgumentException($"Unsupported 'overflow' value: '{value}'.");
                }
                break;
            }
            case "overflow-x":
                Clip.Horizontal = ParseOverflowAxis(value, "overflow-x");
                break;
            case "overflow-y":
                Clip.Vertical = ParseOverflowAxis(value, "overflow-y");
                break;

            // ---- Transitions ----
            case "transition-duration":
                Transition.Duration = ParseDuration(value);
                break;
            case "transition-property":
                Transition.Properties = ParseTransitionProperty(value);
                break;
            
            // ---- Text ----
            
            case "font-family":
            {
                if (UserData is CustomFontInfo fontInfo)
                {
                    fontInfo.fontFamily = ToCss(value);
                }
                else
                {
                    UserData = new CustomFontInfo()
                    {
                        fontFamily = ToCss(value)
                    };
                }
                break;
            }
            case "font-style":
            {
                if (UserData is CustomFontInfo fontInfo)
                {
                    fontInfo.fontStyle = ParseFontStyle(value);
                }
                else
                {
                    UserData = new CustomFontInfo()
                    {
                        fontStyle = ParseFontStyle(value)
                    };
                }
                break;
            }
            case "color":
                TextColor = ParseColor(value);
                break;
            case "font-size":
                FontSize = ToUshort(ParsePixels(value, "font-size"), "font-size");
                break;
            case "letter-spacing":
                LetterSpacing = ToUshort(ParsePixels(value, "letter-spacing"), "letter-spacing");
                break;
            case "line-height":
                LineHeight = ToUshort(ParsePixels(value, "line-height"), "line-height");
                break;
            case "font-id":
                FontId = ToUshort(ParseInt(value, "font-id"), "font-id");
                break;
            case "text-align":
                TextAlignment = ParseTextAlignment(value);
                break;
            case "white-space":
            case "text-wrap":
                WrapMode = ParseWrapMode(value);
                break;

            default:
                throw new ArgumentException($"Unsupported CSS property '{key}'.");
        }
    }

    private void EnsureFloating()
    {
        if (Floating.AttachTo == Clay.FloatingAttachToElement.None)
            Floating.AttachTo = Clay.FloatingAttachToElement.Parent;
    }

    private void ApplyBorderShorthand(object value)
    {
        string s = ToCss(value).Trim();
        string[] tokens = s.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length == 0)
            throw new ArgumentException("Empty 'border' value.");

        foreach (string token in tokens)
        {
            if (TryParseBorderWidth(token, out ushort w))
            {
                Border.Width = new Clay.BorderWidth { Top = w, Right = w, Bottom = w, Left = w, BetweenChildren = 0 };
            }
            else if (IsColorToken(token))
            {
                Border.Color = ParseColor(token);
            }
            else
            {
                switch (token.ToLowerInvariant())
                {
                    case "solid":
                        break; // Clay only renders solid borders; accepted and ignored.
                    default:
                        throw new ArgumentException($"Unsupported border-style '{token}' (Clay supports 'solid' only).");
                }
            }
        }
    }

    // ---------------- Parsers ----------------

    private static (float top, float right, float bottom, float left) ParseEdgeValues(object value, string prop)
    {
        string s = ToCss(value).Trim();
        string[] parts = s.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length switch
        {
            1 => (P(0), P(0), P(0), P(0)),
            2 => (P(0), P(1), P(0), P(1)),
            3 => (P(0), P(1), P(2), P(1)),
            4 => (P(0), P(1), P(2), P(3)),
            _ => throw new ArgumentException($"'{prop}' expects 1-4 values, got {parts.Length}: '{s}'."),
        };
        float P(int i) => ParsePixels(parts[i], prop);
    }

    private static Clay.SizingAxis ParseSizing(object value, string prop)
    {
        string s = ToCss(value).Trim();
        if (s.Length == 0)
            throw new ArgumentException($"Empty value for '{prop}'.");

        if (s.EndsWith('%'))
        {
            float pct = float.Parse(s[..^1], NumberStyles.Float, CultureInfo.InvariantCulture) / 100f;
            return Clay.SizingPercent(pct);
        }

        switch (s.ToLowerInvariant())
        {
            case "auto":
            case "fit-content":
            case "fit":
                return Clay.SizingFit();
            case "grow":
            case "fill":
                return Clay.SizingGrow();
        }

        return Clay.SizingFixed(ParsePixels(s, prop));
    }

    private static Clay.LayoutDirection ParseDirection(object value) =>
        ToCss(value).Trim().ToLowerInvariant() switch
        {
            "row" => Clay.LayoutDirection.LeftToRight,
            "column" => Clay.LayoutDirection.TopToBottom,
            _ => throw new ArgumentException($"Unsupported 'flex-direction' value: '{value}'. Clay supports 'row' or 'column' only (no reverse/wrap)."),
        };

    private static Clay.LayoutAlignmentX ParseAlignX(object value, string prop) =>
        ToCss(value).Trim().ToLowerInvariant() switch
        {
            "flex-start" or "start" or "left" => Clay.LayoutAlignmentX.Left,
            "center" => Clay.LayoutAlignmentX.Center,
            "flex-end" or "end" or "right" => Clay.LayoutAlignmentX.Right,
            _ => throw new ArgumentException($"Unsupported '{prop}' value: '{value}'. Clay supports flex-start/center/flex-end (or left/center/right)."),
        };

    private static Clay.LayoutAlignmentY ParseAlignY(object value, string prop) =>
        ToCss(value).Trim().ToLowerInvariant() switch
        {
            "flex-start" or "start" or "top" => Clay.LayoutAlignmentY.Top,
            "center" => Clay.LayoutAlignmentY.Center,
            "flex-end" or "end" or "bottom" => Clay.LayoutAlignmentY.Bottom,
            _ => throw new ArgumentException($"Unsupported '{prop}' value: '{value}'. Clay supports flex-start/center/flex-end (or top/center/bottom)."),
        };

    private static bool ParseOverflowAxis(object value, string prop) =>
        ToCss(value).Trim().ToLowerInvariant() switch
        {
            "visible" => false,
            "hidden" or "clip" or "scroll" or "auto" => true,
            _ => throw new ArgumentException($"Unsupported '{prop}' value: '{value}'."),
        };

    private static float ParseAspectRatio(object value)
    {
        string s = ToCss(value).Trim();
        var span = s.AsSpan();
        int slash = span.IndexOf('/');
        if (slash < 0)
            return float.Parse(span, NumberStyles.Float, CultureInfo.InvariantCulture);

        float w = float.Parse(span[..slash], NumberStyles.Float, CultureInfo.InvariantCulture);
        float h = float.Parse(span[(slash + 1)..], NumberStyles.Float, CultureInfo.InvariantCulture);
        if (h == 0f)
            throw new ArgumentException($"Invalid aspect-ratio: '{value}' (height is zero).");
        return w / h;
    }

    private static float ParseDuration(object value)
    {
        string s = ToCss(value).Trim();
        var span = s.AsSpan();
        float scale = 1f;
        if (span.EndsWith("ms", StringComparison.OrdinalIgnoreCase))
        {
            span = span[..^2];
            scale = 0.001f;
        }
        else if (span.EndsWith("s", StringComparison.OrdinalIgnoreCase))
        {
            span = span[..^1];
        }
        return float.Parse(span, NumberStyles.Float, CultureInfo.InvariantCulture) * scale;
    }

    private static Clay.TransitionProperty ParseTransitionProperty(object value)
    {
        string s = ToCss(value).Trim();
        if (s.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            return Clay.TransitionProperty.X
                 | Clay.TransitionProperty.Y
                 | Clay.TransitionProperty.Width
                 | Clay.TransitionProperty.Height
                 | Clay.TransitionProperty.BackgroundColor
                 | Clay.TransitionProperty.OverlayColor
                 | Clay.TransitionProperty.CornerRadius
                 | Clay.TransitionProperty.BorderColor
                 | Clay.TransitionProperty.BorderWidth;
        }

        Clay.TransitionProperty result = Clay.TransitionProperty.None;
        foreach (string raw in s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            result |= raw.Trim().ToLowerInvariant() switch
            {
                "x" => Clay.TransitionProperty.X,
                "y" => Clay.TransitionProperty.Y,
                "Width" => Clay.TransitionProperty.Width,
                "height" => Clay.TransitionProperty.Height,
                "background-color" or "background" => Clay.TransitionProperty.BackgroundColor,
                "overlay-color" or "color" => Clay.TransitionProperty.OverlayColor,
                "corner-radius" or "border-radius" => Clay.TransitionProperty.CornerRadius,
                "border-color" => Clay.TransitionProperty.BorderColor,
                "border-Width" => Clay.TransitionProperty.BorderWidth,
                _ => throw new ArgumentException($"Unsupported transition-property: '{raw}'."),
            };
        }
        return result;
    }

    private static bool TryParseBorderWidth(string token, out ushort width)
    {
        var span = token.AsSpan().Trim();
        if (span.EndsWith("px", StringComparison.OrdinalIgnoreCase))
            span = span[..^2].Trim();
        if (span.Length == 0 || span.IndexOf('%') >= 0)
        {
            width = 0;
            return false;
        }
        if (float.TryParse(span, NumberStyles.Float, CultureInfo.InvariantCulture, out float v))
        {
            width = ToUshort(v, "border");
            return true;
        }
        width = 0;
        return false;
    }

    public Action<Vector2>? MouseEnter;
    public Action<Vector2>? MouseLeave;

    public Action<Vector2>? MouseDown;
    public Action<Vector2>? MouseUp;

    private bool _isHovered;

    internal override void LayoutSelfAndChildren()
    {
        var scope = Clay.Element(ElementId, new Clay.ElementDeclaration
        {
            Layout = Layout,
            BackgroundColor = BackgroundColor,
            OverlayColor = OverlayColor,
            CornerRadius = CornerRadius,
            AspectRatio = AspectRatio,
            Image = Image,
            Floating = Floating,
            Custom = Custom,
            Clip = Clip,
            Border = Border,
            Transition = Transition,
            UserData = UserData
        });

        // ---- Hover ----
        if (Clay.Hovered())
        {
            var data = Clay.GetPointerState();
            if (data.State == Clay.PointerDataInteractionState.PressedThisFrame)
            {
                MouseDown?.Invoke(data.Position);
            }
            else if (data.State == Clay.PointerDataInteractionState.ReleasedThisFrame)
            {
                MouseUp?.Invoke(data.Position);
            }

            if (!_isHovered)
            {
                _isHovered = true;
                MouseEnter?.Invoke(data.Position);
            }
        }
        else
        {
            var data = Clay.GetPointerState();
            if (_isHovered)
            {
                _isHovered = false;
                MouseLeave?.Invoke(data.Position);
            }
        }

        // ---- Render children ----
        if (Children is not null)
        {
            StringBuilder? sb = null;
            
            foreach (var child in Children)
            {
                if (child is ClayElement element)
                {
                    // Allow text embedded directly in ClayElement without ClayTextElement, like HTML
                    if (sb?.Length > 0)
                    {
                        Clay.Text(sb.ToString(), new Clay.TextElementConfig()
                        {
                            UserData = UserData,
                            TextColor = TextColor,
                            FontId = FontId,
                            FontSize = FontSize,
                            LetterSpacing = LetterSpacing,
                            LineHeight = LineHeight,
                            WrapMode = WrapMode,
                            TextAlignment = TextAlignment
                        });
                        sb.Clear();
                    }
                    
                    element.LayoutSelfAndChildren();
                }
                else if (child is ClayTextNode textNode)
                {
                    (sb ??= new StringBuilder()).Append(textNode.Text);
                }
            }
        }
        
        scope.Close(); // not `using` as that is technically slightly slower
    }

    public Clay.RenderCommandArray DoLayout(float deltaTime)
    {
        Clay.BeginLayout();
        
        LayoutSelfAndChildren();
        
        Clay.RenderCommandArray renderCommands = Clay.EndLayout(deltaTime);
        return renderCommands;
    }
}

[LuaVisible]
public partial class ClayTextNode : ClayNode
{
    public override NodeType NodeType => NodeType.Text;

    [LuaName]
    public string Text = "";
}

[LuaVisible]
public partial class ClayTextElement : ClayElementBase
{
    public override NodeType NodeType => NodeType.TextElement;

    public object? UserData; // A pointer that will be transparently passed through to the resulting render command.
    public Clay.Color TextColor; // The RGBA color of the font to render, conventionally specified as 0-255.
    public ushort FontId; // An integer transparently passed to the measure text function to identify the font to use.
    public ushort FontSize; // Controls the size of the font.
    public ushort LetterSpacing; // Controls extra horizontal spacing between characters.
    public ushort LineHeight; // Controls additional vertical space between wrapped lines of text.
    public Clay.TextElementConfigWrapMode WrapMode; // How text wraps.
    public Clay.TextAlignment TextAlignment; // How wrapped lines are horizontally aligned.

    private string _text = "";

    protected override void OnChildrenChanged()
    {
        var sb = new StringBuilder();
        if (Children is not null)
        {
            foreach (var child in Children)
            {
                if (child is ClayTextNode textNode)
                {
                    sb.Append(textNode.Text);
                }
            }
        }
        _text = sb.ToString();
    }

    internal override void LayoutSelfAndChildren()
    {
        Clay.Text(_text, new Clay.TextElementConfig()
        {
            UserData = UserData,
            TextColor = TextColor,
            FontId = FontId,
            FontSize = FontSize,
            LetterSpacing = LetterSpacing,
            LineHeight = LineHeight,
            WrapMode = WrapMode,
            TextAlignment = TextAlignment
        });
    }

    [LuaName]
    public override void SetProperty(string key, object value)
    {
        switch (key.Trim().ToLowerInvariant())
        {
            case "font-family":
            {
                if (UserData is CustomFontInfo fontInfo)
                {
                    fontInfo.fontFamily = ToCss(value);
                }
                else
                {
                    UserData = new CustomFontInfo()
                    {
                        fontFamily = ToCss(value)
                    };
                }
                break;
            }
            case "font-style":
            {
                if (UserData is CustomFontInfo fontInfo)
                {
                    fontInfo.fontStyle = ParseFontStyle(value);
                }
                else
                {
                    UserData = new CustomFontInfo()
                    {
                        fontStyle = ParseFontStyle(value)
                    };
                }
                break;
            }
            case "color":
                TextColor = ParseColor(value);
                break;
            case "font-size":
                FontSize = ToUshort(ParsePixels(value, "font-size"), "font-size");
                break;
            case "letter-spacing":
                LetterSpacing = ToUshort(ParsePixels(value, "letter-spacing"), "letter-spacing");
                break;
            case "line-height":
                LineHeight = ToUshort(ParsePixels(value, "line-height"), "line-height");
                break;
            case "font-id":
                FontId = ToUshort(ParseInt(value, "font-id"), "font-id");
                break;
            case "text-align":
                TextAlignment = ParseTextAlignment(value);
                break;
            case "white-space":
            case "text-wrap":
                WrapMode = ParseWrapMode(value);
                break;
            case "data":
                UserData = value;
                break;
            default:
                throw new ArgumentException($"Unsupported CSS property '{key}'.");
        }
    }
}