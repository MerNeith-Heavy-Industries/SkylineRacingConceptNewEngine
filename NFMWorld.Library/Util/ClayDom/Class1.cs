using System;
using System.Collections.Generic;
using System.Globalization;
using ClaySharp;
using nfm_world_library.Lua;

namespace NFMWorld.ClayDom;

[LuaVisible]
public abstract partial class ClayNode
{
    private protected static uint MaxId;

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

    private protected static Clay_Color ParseColor(object value)
    {
        string s = ToCss(value).Trim();
        if (s.Length == 0)
            throw new ArgumentException("Empty color value.");
        if (s[0] == '#')
            return ParseHexColor(s);
        if (s.StartsWith("rgb", StringComparison.OrdinalIgnoreCase))
            return ParseRgbColor(s);
        if (s_namedColors.TryGetValue(s, out var named))
            return named;
        throw new ArgumentException($"Unsupported color: '{s}'.");
    }

    private protected static Clay_Color ParseHexColor(string s)
    {
        var hex = s.AsSpan(1).Trim();
        return hex.Length switch
        {
            3 => new Clay_Color(HexNibble(hex[0]) * 17f, HexNibble(hex[1]) * 17f, HexNibble(hex[2]) * 17f, 255f),
            4 => new Clay_Color(HexNibble(hex[0]) * 17f, HexNibble(hex[1]) * 17f, HexNibble(hex[2]) * 17f, HexNibble(hex[3]) * 17f),
            6 => new Clay_Color(HexByte(hex[0], hex[1]), HexByte(hex[2], hex[3]), HexByte(hex[4], hex[5]), 255f),
            8 => new Clay_Color(HexByte(hex[0], hex[1]), HexByte(hex[2], hex[3]), HexByte(hex[4], hex[5]), HexByte(hex[6], hex[7])),
            _ => throw new ArgumentException($"Invalid hex color: '{s}'."),
        };

        static float HexNibble(char c) => Convert.ToInt32(c.ToString(), 16);
        static float HexByte(char hi, char lo) => Convert.ToInt32($"{hi}{lo}", 16);
    }

    private protected static Clay_Color ParseRgbColor(string s)
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
        return new Clay_Color(r, g, b, a);

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
        return s_namedColors.ContainsKey(token);
    }

    private protected static readonly Dictionary<string, Clay_Color> s_namedColors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["transparent"] = new Clay_Color(0, 0, 0, 0),
        ["black"] = new Clay_Color(0, 0, 0, 255),
        ["white"] = new Clay_Color(255, 255, 255, 255),
        ["red"] = new Clay_Color(255, 0, 0, 255),
        ["green"] = new Clay_Color(0, 128, 0, 255),
        ["lime"] = new Clay_Color(0, 255, 0, 255),
        ["blue"] = new Clay_Color(0, 0, 255, 255),
        ["yellow"] = new Clay_Color(255, 255, 0, 255),
        ["cyan"] = new Clay_Color(0, 255, 255, 255),
        ["aqua"] = new Clay_Color(0, 255, 255, 255),
        ["magenta"] = new Clay_Color(255, 0, 255, 255),
        ["fuchsia"] = new Clay_Color(255, 0, 255, 255),
        ["gray"] = new Clay_Color(128, 128, 128, 255),
        ["grey"] = new Clay_Color(128, 128, 128, 255),
        ["orange"] = new Clay_Color(255, 165, 0, 255),
        ["purple"] = new Clay_Color(128, 0, 128, 255),
        ["pink"] = new Clay_Color(255, 192, 203, 255),
        ["brown"] = new Clay_Color(165, 42, 42, 255),
        ["silver"] = new Clay_Color(192, 192, 192, 255),
        ["gold"] = new Clay_Color(255, 215, 0, 255),
        ["navy"] = new Clay_Color(0, 0, 128, 255),
        ["teal"] = new Clay_Color(0, 128, 128, 255),
    };
}

[LuaVisible]
public partial class ClayElement : ClayNode
{
    public Clay_LayoutConfig layout; // Controls the size and position of an element and its children.
    public Clay_Color backgroundColor; // Background color; generates a RECTANGLE render command (or is passed to IMAGE/CUSTOM).
    public Clay_Color overlayColor; // "Color Overlay" applied to this element and all its children.
    public Clay_CornerRadius cornerRadius; // Corner rounding of rectangles, borders and images.
    public Clay_AspectRatioElementConfig aspectRatio; // Aspect ratio scaling.
    public Clay_ImageElementConfig image; // Image element settings.
    public Clay_FloatingElementConfig floating; // Floating / absolute positioning settings.
    public Clay_CustomElementConfig custom; // CUSTOM render command settings.
    public Clay_ClipElementConfig clip; // Clip / scroll settings.
    public Clay_BorderElementConfig border; // Border settings.
    public Clay_TransitionElementConfig transition; // Transition settings.
    public object? userData; // Transparently passed through to resulting render commands.

    public uint Id = MaxId++;
    public string DebugName = string.Empty;

    public List<ClayNode>? Children = null;

    // id is the only thing that matter, stringId is shown in DebugView
    public Clay_ElementId ElementId => new Clay_ElementId()
    {
        id = Id,
        stringId = DebugName
    };

    [LuaName]
    public ClayElement()
    {
    }

    [LuaName]
    public void AppendChild(ClayNode node)
    {
        (Children ??= []).Add(node);
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
    }

    [LuaName]
    public void RemoveChild(ClayNode node)
    {
        Children?.Remove(node);
    }

    [LuaName]
    public override void SetProperty(string key, object value)
    {
        switch (key.Trim().ToLowerInvariant())
        {
            // ---- Clay-specific ----
            case "image":
                image.imageData = ToCss(value);
                break;
            case "name":
                DebugName = ToCss(value);
                break;
            case "data":
                userData = value;
                break;
            
            // ---- Sizing ----
            case "width":
                layout.sizing.width = ParseSizing(value, "width");
                break;
            case "height":
                layout.sizing.height = ParseSizing(value, "height");
                break;
            case "min-width":
                layout.sizing.width.minMax.min = ParsePixels(value, "min-width");
                break;
            case "max-width":
                layout.sizing.width.minMax.max = ParsePixels(value, "max-width");
                break;
            case "min-height":
                layout.sizing.height.minMax.min = ParsePixels(value, "min-height");
                break;
            case "max-height":
                layout.sizing.height.minMax.max = ParsePixels(value, "max-height");
                break;

            // ---- Box model ----
            case "padding":
            {
                var (t, r, b, l) = ParseEdgeValues(value, "padding");
                layout.padding = new Clay_Padding
                {
                    top = ToUshort(t, "padding"),
                    right = ToUshort(r, "padding"),
                    bottom = ToUshort(b, "padding"),
                    left = ToUshort(l, "padding"),
                };
                break;
            }
            case "padding-top":
                layout.padding.top = ToUshort(ParsePixels(value, "padding-top"), "padding-top");
                break;
            case "padding-right":
                layout.padding.right = ToUshort(ParsePixels(value, "padding-right"), "padding-right");
                break;
            case "padding-bottom":
                layout.padding.bottom = ToUshort(ParsePixels(value, "padding-bottom"), "padding-bottom");
                break;
            case "padding-left":
                layout.padding.left = ToUshort(ParsePixels(value, "padding-left"), "padding-left");
                break;
            case "gap":
            case "column-gap":
            case "row-gap":
                layout.childGap = ToUshort(ParsePixels(value, key), key);
                break;
            case "margin":
            case "margin-top":
            case "margin-right":
            case "margin-bottom":
            case "margin-left":
                throw new ArgumentException($"Clay has no equivalent for '{key}' (no margin concept — use padding or gap instead).");

            // ---- Flex layout ----
            case "flex-direction":
                layout.layoutDirection = ParseDirection(value);
                break;
            case "align-items":
            {
                // align-items = cross axis. row → y, column → x.
                if (layout.layoutDirection == Clay_LayoutDirection.CLAY_LEFT_TO_RIGHT)
                    layout.childAlignment.y = ParseAlignY(value, "align-items");
                else
                    layout.childAlignment.x = ParseAlignX(value, "align-items");
                break;
            }
            case "justify-content":
            {
                // justify-content = main axis. row → x, column → y.
                if (layout.layoutDirection == Clay_LayoutDirection.CLAY_LEFT_TO_RIGHT)
                    layout.childAlignment.x = ParseAlignX(value, "justify-content");
                else
                    layout.childAlignment.y = ParseAlignY(value, "justify-content");
                break;
            }

            // ---- Color ----
            case "background-color":
                backgroundColor = ParseColor(value);
                break;
            case "overlay-color":
                overlayColor = ParseColor(value);
                break;

            // ---- Border ----
            case "border":
                ApplyBorderShorthand(value);
                break;
            case "border-width":
            {
                var (t, r, b, l) = ParseEdgeValues(value, "border-width");
                border.width = new Clay_BorderWidth
                {
                    top = ToUshort(t, "border-width"),
                    right = ToUshort(r, "border-width"),
                    bottom = ToUshort(b, "border-width"),
                    left = ToUshort(l, "border-width"),
                    betweenChildren = 0,
                };
                break;
            }
            case "border-top-width":
                border.width.top = ToUshort(ParsePixels(value, "border-top-width"), "border-top-width");
                break;
            case "border-right-width":
                border.width.right = ToUshort(ParsePixels(value, "border-right-width"), "border-right-width");
                break;
            case "border-bottom-width":
                border.width.bottom = ToUshort(ParsePixels(value, "border-bottom-width"), "border-bottom-width");
                break;
            case "border-left-width":
                border.width.left = ToUshort(ParsePixels(value, "border-left-width"), "border-left-width");
                break;
            case "border-color":
                border.color = ParseColor(value);
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
                cornerRadius = new Clay_CornerRadius
                {
                    topLeft = t,
                    topRight = r,
                    bottomRight = b,
                    bottomLeft = l,
                };
                break;
            }
            case "border-top-left-radius":
                cornerRadius.topLeft = ParsePixels(value, "border-top-left-radius");
                break;
            case "border-top-right-radius":
                cornerRadius.topRight = ParsePixels(value, "border-top-right-radius");
                break;
            case "border-bottom-right-radius":
                cornerRadius.bottomRight = ParsePixels(value, "border-bottom-right-radius");
                break;
            case "border-bottom-left-radius":
                cornerRadius.bottomLeft = ParsePixels(value, "border-bottom-left-radius");
                break;

            // ---- Positioning / floating ----
            case "position":
            {
                switch (ToCss(value).Trim().ToLowerInvariant())
                {
                    case "static":
                    case "relative":
                    case "initial":
                        floating.attachTo = Clay_FloatingAttachToElement.CLAY_ATTACH_TO_NONE;
                        break;
                    case "absolute":
                        floating.attachTo = Clay_FloatingAttachToElement.CLAY_ATTACH_TO_PARENT;
                        break;
                    case "fixed":
                        floating.attachTo = Clay_FloatingAttachToElement.CLAY_ATTACH_TO_ROOT;
                        break;
                    default:
                        throw new ArgumentException($"Unsupported 'position' value: '{value}'.");
                }
                break;
            }
            case "top":
                EnsureFloating();
                floating.offset.Y = ParsePixels(value, "top");
                break;
            case "bottom":
                EnsureFloating();
                floating.offset.Y = -ParsePixels(value, "bottom");
                break;
            case "left":
                EnsureFloating();
                floating.offset.X = ParsePixels(value, "left");
                break;
            case "right":
                EnsureFloating();
                floating.offset.X = -ParsePixels(value, "right");
                break;
            case "z-index":
                floating.zIndex = (short)ParseInt(value, "z-index");
                break;
            case "aspect-ratio":
                aspectRatio.aspectRatio = ParseAspectRatio(value);
                break;

            // ---- Clip / scroll ----
            case "overflow":
            {
                switch (ToCss(value).Trim().ToLowerInvariant())
                {
                    case "visible":
                        clip.horizontal = false;
                        clip.vertical = false;
                        break;
                    case "hidden":
                    case "clip":
                    case "scroll":
                    case "auto":
                        clip.horizontal = true;
                        clip.vertical = true;
                        break;
                    default:
                        throw new ArgumentException($"Unsupported 'overflow' value: '{value}'.");
                }
                break;
            }
            case "overflow-x":
                clip.horizontal = ParseOverflowAxis(value, "overflow-x");
                break;
            case "overflow-y":
                clip.vertical = ParseOverflowAxis(value, "overflow-y");
                break;

            // ---- Transitions ----
            case "transition-duration":
                transition.duration = ParseDuration(value);
                break;
            case "transition-property":
                transition.properties = ParseTransitionProperty(value);
                break;

            default:
                throw new ArgumentException($"Unsupported CSS property '{key}'.");
        }
    }

    private void EnsureFloating()
    {
        if (floating.attachTo == Clay_FloatingAttachToElement.CLAY_ATTACH_TO_NONE)
            floating.attachTo = Clay_FloatingAttachToElement.CLAY_ATTACH_TO_PARENT;
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
                border.width = new Clay_BorderWidth { top = w, right = w, bottom = w, left = w, betweenChildren = 0 };
            }
            else if (IsColorToken(token))
            {
                border.color = ParseColor(token);
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

    private static float ParsePixels(object value, string prop)
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

    private static ushort ToUshort(float v, string prop)
    {
        if (v < 0 || v > ushort.MaxValue)
            throw new ArgumentException($"Value for '{prop}' out of range (0-{ushort.MaxValue}): {v}.");
        return (ushort)MathF.Round(v);
    }

    private static int ParseInt(object value, string prop)
    {
        string s = ToCss(value).Trim();
        return int.Parse(s, NumberStyles.Integer, CultureInfo.InvariantCulture);
    }

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

    private static Clay_SizingAxis ParseSizing(object value, string prop)
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

    private static Clay_LayoutDirection ParseDirection(object value) =>
        ToCss(value).Trim().ToLowerInvariant() switch
        {
            "row" => Clay_LayoutDirection.CLAY_LEFT_TO_RIGHT,
            "column" => Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
            _ => throw new ArgumentException($"Unsupported 'flex-direction' value: '{value}'. Clay supports 'row' or 'column' only (no reverse/wrap)."),
        };

    private static Clay_LayoutAlignmentX ParseAlignX(object value, string prop) =>
        ToCss(value).Trim().ToLowerInvariant() switch
        {
            "flex-start" or "start" or "left" => Clay_LayoutAlignmentX.CLAY_ALIGN_X_LEFT,
            "center" => Clay_LayoutAlignmentX.CLAY_ALIGN_X_CENTER,
            "flex-end" or "end" or "right" => Clay_LayoutAlignmentX.CLAY_ALIGN_X_RIGHT,
            _ => throw new ArgumentException($"Unsupported '{prop}' value: '{value}'. Clay supports flex-start/center/flex-end (or left/center/right)."),
        };

    private static Clay_LayoutAlignmentY ParseAlignY(object value, string prop) =>
        ToCss(value).Trim().ToLowerInvariant() switch
        {
            "flex-start" or "start" or "top" => Clay_LayoutAlignmentY.CLAY_ALIGN_Y_TOP,
            "center" => Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER,
            "flex-end" or "end" or "bottom" => Clay_LayoutAlignmentY.CLAY_ALIGN_Y_BOTTOM,
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

    private static Clay_TransitionProperty ParseTransitionProperty(object value)
    {
        string s = ToCss(value).Trim();
        if (s.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            return Clay_TransitionProperty.CLAY_TRANSITION_PROPERTY_X
                 | Clay_TransitionProperty.CLAY_TRANSITION_PROPERTY_Y
                 | Clay_TransitionProperty.CLAY_TRANSITION_PROPERTY_WIDTH
                 | Clay_TransitionProperty.CLAY_TRANSITION_PROPERTY_HEIGHT
                 | Clay_TransitionProperty.CLAY_TRANSITION_PROPERTY_BACKGROUND_COLOR
                 | Clay_TransitionProperty.CLAY_TRANSITION_PROPERTY_OVERLAY_COLOR
                 | Clay_TransitionProperty.CLAY_TRANSITION_PROPERTY_CORNER_RADIUS
                 | Clay_TransitionProperty.CLAY_TRANSITION_PROPERTY_BORDER_COLOR
                 | Clay_TransitionProperty.CLAY_TRANSITION_PROPERTY_BORDER_WIDTH;
        }

        Clay_TransitionProperty result = Clay_TransitionProperty.CLAY_TRANSITION_PROPERTY_NONE;
        foreach (string raw in s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            result |= raw.Trim().ToLowerInvariant() switch
            {
                "x" => Clay_TransitionProperty.CLAY_TRANSITION_PROPERTY_X,
                "y" => Clay_TransitionProperty.CLAY_TRANSITION_PROPERTY_Y,
                "width" => Clay_TransitionProperty.CLAY_TRANSITION_PROPERTY_WIDTH,
                "height" => Clay_TransitionProperty.CLAY_TRANSITION_PROPERTY_HEIGHT,
                "background-color" or "background" => Clay_TransitionProperty.CLAY_TRANSITION_PROPERTY_BACKGROUND_COLOR,
                "overlay-color" or "color" => Clay_TransitionProperty.CLAY_TRANSITION_PROPERTY_OVERLAY_COLOR,
                "corner-radius" or "border-radius" => Clay_TransitionProperty.CLAY_TRANSITION_PROPERTY_CORNER_RADIUS,
                "border-color" => Clay_TransitionProperty.CLAY_TRANSITION_PROPERTY_BORDER_COLOR,
                "border-width" => Clay_TransitionProperty.CLAY_TRANSITION_PROPERTY_BORDER_WIDTH,
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

    internal override void LayoutSelfAndChildren()
    {
        var scope = Clay.Element(ElementId, new Clay_ElementDeclaration
        {
            layout = layout,
            backgroundColor = backgroundColor,
            overlayColor = overlayColor,
            cornerRadius = cornerRadius,
            aspectRatio = aspectRatio,
            image = image,
            floating = floating,
            custom = custom,
            clip = clip,
            border = border,
            transition = transition,
            userData = userData
        });

        if (Children is not null)
        {
            foreach (var child in Children)
            {
                child.LayoutSelfAndChildren();
            }
        }
        
        scope.Close(); // not `using` as that is technically slightly slower
    }

    public Clay_RenderCommandArray Layout(float deltaTime)
    {
        Clay.BeginLayout();
        
        LayoutSelfAndChildren();
        
        Clay_RenderCommandArray renderCommands = Clay.EndLayout(deltaTime);
        return renderCommands;
    }
}

[LuaVisible]
public partial class ClayTextElement : ClayNode
{
    public object? userData; // A pointer that will be transparently passed through to the resulting render command.
    public Clay_Color textColor; // The RGBA color of the font to render, conventionally specified as 0-255.
    public ushort fontId; // An integer transparently passed to the measure text function to identify the font to use.
    public ushort fontSize; // Controls the size of the font.
    public ushort letterSpacing; // Controls extra horizontal spacing between characters.
    public ushort lineHeight; // Controls additional vertical space between wrapped lines of text.
    public Clay_TextElementConfigWrapMode wrapMode; // How text wraps.
    public Clay_TextAlignment textAlignment; // How wrapped lines are horizontally aligned.
    
    [LuaName]
    public string text = "";

    internal override void LayoutSelfAndChildren()
    {
        Clay.Text(text, new Clay_TextElementConfig()
        {
            userData = userData,
            textColor = textColor,
            fontId = fontId,
            fontSize = fontSize,
            letterSpacing = letterSpacing,
            lineHeight = lineHeight,
            wrapMode = wrapMode,
            textAlignment = textAlignment
        });
    }

    public override void SetProperty(string key, object value)
    {
        
    }
}