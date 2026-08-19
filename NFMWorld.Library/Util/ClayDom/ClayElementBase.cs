using System.Globalization;
using ClaySharp;
using nfm_world_library.Lua;
using NFMWorld.DriverInterface.DriverInterface;

namespace NFMWorld.ClayDom;

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
        ["aliceblue"] = new Clay.Color(240, 248, 255, 255),
        ["antiquewhite"] = new Clay.Color(250, 235, 215, 255),
        ["aqua"] = new Clay.Color(0, 255, 255, 255),
        ["aquamarine"] = new Clay.Color(127, 255, 212, 255),
        ["azure"] = new Clay.Color(240, 255, 255, 255),
        ["beige"] = new Clay.Color(245, 245, 220, 255),
        ["bisque"] = new Clay.Color(255, 228, 196, 255),
        ["black"] = new Clay.Color(0, 0, 0, 255),
        ["blanchedalmond"] = new Clay.Color(255, 235, 205, 255),
        ["blue"] = new Clay.Color(0, 0, 255, 255),
        ["blueviolet"] = new Clay.Color(138, 43, 226, 255),
        ["brown"] = new Clay.Color(165, 42, 42, 255),
        ["burlywood"] = new Clay.Color(222, 184, 135, 255),
        ["cadetblue"] = new Clay.Color(95, 158, 160, 255),
        ["chartreuse"] = new Clay.Color(127, 255, 0, 255),
        ["chocolate"] = new Clay.Color(210, 105, 30, 255),
        ["coral"] = new Clay.Color(255, 127, 80, 255),
        ["cornflowerblue"] = new Clay.Color(100, 149, 237, 255),
        ["cornsilk"] = new Clay.Color(255, 248, 220, 255),
        ["crimson"] = new Clay.Color(220, 20, 60, 255),
        ["cyan"] = new Clay.Color(0, 255, 255, 255),
        ["darkblue"] = new Clay.Color(0, 0, 139, 255),
        ["darkcyan"] = new Clay.Color(0, 139, 139, 255),
        ["darkgoldenrod"] = new Clay.Color(184, 134, 11, 255),
        ["darkgray"] = new Clay.Color(169, 169, 169, 255),
        ["darkgreen"] = new Clay.Color(0, 100, 0, 255),
        ["darkkhaki"] = new Clay.Color(189, 183, 107, 255),
        ["darkmagenta"] = new Clay.Color(139, 0, 139, 255),
        ["darkolivegreen"] = new Clay.Color(85, 107, 47, 255),
        ["darkorange"] = new Clay.Color(255, 140, 0, 255),
        ["darkorchid"] = new Clay.Color(153, 50, 204, 255),
        ["darkred"] = new Clay.Color(139, 0, 0, 255),
        ["darksalmon"] = new Clay.Color(233, 150, 122, 255),
        ["darkseagreen"] = new Clay.Color(143, 188, 139, 255),
        ["darkslateblue"] = new Clay.Color(72, 61, 139, 255),
        ["darkslategray"] = new Clay.Color(47, 79, 79, 255),
        ["darkturquoise"] = new Clay.Color(0, 206, 209, 255),
        ["darkviolet"] = new Clay.Color(148, 0, 211, 255),
        ["deeppink"] = new Clay.Color(255, 20, 147, 255),
        ["deepskyblue"] = new Clay.Color(0, 191, 255, 255),
        ["dimgray"] = new Clay.Color(105, 105, 105, 255),
        ["dodgerblue"] = new Clay.Color(30, 144, 255, 255),
        ["firebrick"] = new Clay.Color(178, 34, 34, 255),
        ["floralwhite"] = new Clay.Color(255, 250, 240, 255),
        ["forestgreen"] = new Clay.Color(34, 139, 34, 255),
        ["fuchsia"] = new Clay.Color(255, 0, 255, 255),
        ["gainsboro"] = new Clay.Color(220, 220, 220, 255),
        ["ghostwhite"] = new Clay.Color(248, 248, 255, 255),
        ["gold"] = new Clay.Color(255, 215, 0, 255),
        ["goldenrod"] = new Clay.Color(218, 165, 32, 255),
        ["gray"] = new Clay.Color(128, 128, 128, 255),
        ["green"] = new Clay.Color(0, 128, 0, 255),
        ["greenyellow"] = new Clay.Color(173, 255, 47, 255),
        ["honeydew"] = new Clay.Color(240, 255, 240, 255),
        ["hotpink"] = new Clay.Color(255, 105, 180, 255),
        ["indianred"] = new Clay.Color(205, 92, 92, 255),
        ["indigo"] = new Clay.Color(75, 0, 130, 255),
        ["ivory"] = new Clay.Color(255, 255, 240, 255),
        ["khaki"] = new Clay.Color(240, 230, 140, 255),
        ["lavender"] = new Clay.Color(230, 230, 250, 255),
        ["lavenderblush"] = new Clay.Color(255, 240, 245, 255),
        ["lawngreen"] = new Clay.Color(124, 252, 0, 255),
        ["lemonchiffon"] = new Clay.Color(255, 250, 205, 255),
        ["lightblue"] = new Clay.Color(173, 216, 230, 255),
        ["lightcoral"] = new Clay.Color(240, 128, 128, 255),
        ["lightcyan"] = new Clay.Color(224, 255, 255, 255),
        ["lightgoldenrodyellow"] = new Clay.Color(250, 250, 210, 255),
        ["lightgray"] = new Clay.Color(211, 211, 211, 255),
        ["lightgreen"] = new Clay.Color(144, 238, 144, 255),
        ["lightpink"] = new Clay.Color(255, 182, 193, 255),
        ["lightsalmon"] = new Clay.Color(255, 160, 122, 255),
        ["lightseagreen"] = new Clay.Color(32, 178, 170, 255),
        ["lightskyblue"] = new Clay.Color(135, 206, 250, 255),
        ["lightslategray"] = new Clay.Color(119, 136, 153, 255),
        ["lightsteelblue"] = new Clay.Color(176, 196, 222, 255),
        ["lightyellow"] = new Clay.Color(255, 255, 224, 255),
        ["lime"] = new Clay.Color(0, 255, 0, 255),
        ["limegreen"] = new Clay.Color(50, 205, 50, 255),
        ["linen"] = new Clay.Color(250, 240, 230, 255),
        ["magenta"] = new Clay.Color(255, 0, 255, 255),
        ["maroon"] = new Clay.Color(128, 0, 0, 255),
        ["mediumaquamarine"] = new Clay.Color(102, 205, 170, 255),
        ["mediumblue"] = new Clay.Color(0, 0, 205, 255),
        ["mediumorchid"] = new Clay.Color(186, 85, 211, 255),
        ["mediumpurple"] = new Clay.Color(147, 112, 219, 255),
        ["mediumseagreen"] = new Clay.Color(60, 179, 113, 255),
        ["mediumslateblue"] = new Clay.Color(123, 104, 238, 255),
        ["mediumspringgreen"] = new Clay.Color(0, 250, 154, 255),
        ["mediumturquoise"] = new Clay.Color(72, 209, 204, 255),
        ["mediumvioletred"] = new Clay.Color(199, 21, 133, 255),
        ["midnightblue"] = new Clay.Color(25, 25, 112, 255),
        ["mintcream"] = new Clay.Color(245, 255, 250, 255),
        ["mistyrose"] = new Clay.Color(255, 228, 225, 255),
        ["moccasin"] = new Clay.Color(255, 228, 181, 255),
        ["navajowhite"] = new Clay.Color(255, 222, 173, 255),
        ["navy"] = new Clay.Color(0, 0, 128, 255),
        ["oldlace"] = new Clay.Color(253, 245, 230, 255),
        ["olive"] = new Clay.Color(128, 128, 0, 255),
        ["olivedrab"] = new Clay.Color(107, 142, 35, 255),
        ["orange"] = new Clay.Color(255, 165, 0, 255),
        ["orangered"] = new Clay.Color(255, 69, 0, 255),
        ["orchid"] = new Clay.Color(218, 112, 214, 255),
        ["palegoldenrod"] = new Clay.Color(238, 232, 170, 255),
        ["palegreen"] = new Clay.Color(152, 251, 152, 255),
        ["paleturquoise"] = new Clay.Color(175, 238, 238, 255),
        ["palevioletred"] = new Clay.Color(219, 112, 147, 255),
        ["papayawhip"] = new Clay.Color(255, 239, 213, 255),
        ["peachpuff"] = new Clay.Color(255, 218, 185, 255),
        ["peru"] = new Clay.Color(205, 133, 63, 255),
        ["pink"] = new Clay.Color(255, 192, 203, 255),
        ["plum"] = new Clay.Color(221, 160, 221, 255),
        ["powderblue"] = new Clay.Color(176, 224, 230, 255),
        ["purple"] = new Clay.Color(128, 0, 128, 255),
        ["red"] = new Clay.Color(255, 0, 0, 255),
        ["rosybrown"] = new Clay.Color(188, 143, 143, 255),
        ["royalblue"] = new Clay.Color(65, 105, 225, 255),
        ["saddlebrown"] = new Clay.Color(139, 69, 19, 255),
        ["salmon"] = new Clay.Color(250, 128, 114, 255),
        ["sandybrown"] = new Clay.Color(244, 164, 96, 255),
        ["seagreen"] = new Clay.Color(46, 139, 87, 255),
        ["seashell"] = new Clay.Color(255, 245, 238, 255),
        ["sienna"] = new Clay.Color(160, 82, 45, 255),
        ["silver"] = new Clay.Color(192, 192, 192, 255),
        ["skyblue"] = new Clay.Color(135, 206, 235, 255),
        ["slateblue"] = new Clay.Color(106, 90, 205, 255),
        ["slategray"] = new Clay.Color(112, 128, 144, 255),
        ["snow"] = new Clay.Color(255, 250, 250, 255),
        ["springgreen"] = new Clay.Color(0, 255, 127, 255),
        ["steelblue"] = new Clay.Color(70, 130, 180, 255),
        ["tan"] = new Clay.Color(210, 180, 140, 255),
        ["teal"] = new Clay.Color(0, 128, 128, 255),
        ["thistle"] = new Clay.Color(216, 191, 216, 255),
        ["tomato"] = new Clay.Color(255, 99, 71, 255),
        ["turquoise"] = new Clay.Color(64, 224, 208, 255),
        ["violet"] = new Clay.Color(238, 130, 238, 255),
        ["wheat"] = new Clay.Color(245, 222, 179, 255),
        ["white"] = new Clay.Color(255, 255, 255, 255),
        ["whitesmoke"] = new Clay.Color(245, 245, 245, 255),
        ["yellow"] = new Clay.Color(255, 255, 0, 255),
        ["yellowgreen"] = new Clay.Color(154, 205, 50, 255),
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