using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using Yoga;
using Font = nfm_world.util.Font;
using FontFamily = nfm_world.util.FontFamily;
using FontStyle = nfm_world.util.FontStyle;

namespace nfm_world.ui.yoga.xaml;

/// <summary>
/// Type converter for XNA/FNA Color - parses "R,G,B" or "R,G,B,A" format.
/// </summary>
public class ColorTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string str)
        {
            var parts = str.Split(',');
            if (parts.Length == 3)
            {
                return new Color(
                    byte.Parse(parts[0].Trim(), CultureInfo.InvariantCulture),
                    byte.Parse(parts[1].Trim(), CultureInfo.InvariantCulture),
                    byte.Parse(parts[2].Trim(), CultureInfo.InvariantCulture)
                );
            }
            if (parts.Length == 4)
            {
                return new Color(
                    byte.Parse(parts[0].Trim(), CultureInfo.InvariantCulture),
                    byte.Parse(parts[1].Trim(), CultureInfo.InvariantCulture),
                    byte.Parse(parts[2].Trim(), CultureInfo.InvariantCulture),
                    byte.Parse(parts[3].Trim(), CultureInfo.InvariantCulture)
                );
            }
            // parse hex color
            var trimmed = str.AsSpan().Trim();
            
            // empty color
            if (trimmed.Length == 0)
                return new Color(0,0,0,0);

            // #RRGGBB or #RGB
            if (trimmed[0] == '#' &&
                trimmed.Length is 9 or 7 or 5 or 4)
            {
	            if (trimmed.Length == 9)
	            {
		            return new Color(
			            int.Parse(trimmed.Slice(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
			            int.Parse(trimmed.Slice(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
			            int.Parse(trimmed.Slice(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
			            int.Parse(trimmed.Slice(7, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture)
		            );
	            }
                if (trimmed.Length == 7)
                {
                    return new Color(
                        int.Parse(trimmed.Slice(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                        int.Parse(trimmed.Slice(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                        int.Parse(trimmed.Slice(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture)
                    );
                }
                else if (trimmed.Length == 5)
                {
	                var r = int.Parse(trimmed.Slice(1, 1), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
	                var g = int.Parse(trimmed.Slice(2, 1), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
	                var b = int.Parse(trimmed.Slice(3, 1), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
	                var a = int.Parse(trimmed.Slice(4, 1), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
	                return new Color(
		                (byte)(r * 17),
		                (byte)(g * 17),
		                (byte)(b * 17),
		                (byte)(a * 17)
	                );
                }
                else
                {
                    var r = int.Parse(trimmed.Slice(1, 1), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    var g = int.Parse(trimmed.Slice(2, 1), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    var b = int.Parse(trimmed.Slice(3, 1), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    return new Color(
                        (byte)(r * 17),
                        (byte)(g * 17),
                        (byte)(b * 17)
                    );
                }
            }

            // rgb(...) or rgba(...)
            if (trimmed.StartsWith("rgb", StringComparison.OrdinalIgnoreCase))
            {
	            var start = trimmed.IndexOf('(');
	            var end = trimmed.IndexOf(')');
	            if (start >= 0 && end > start)
	            {
		            var inner = trimmed.Slice(start + 1, end - start - 1);
		            var rgbParts = inner.ToString().Split(' ');
		            if (rgbParts.Length == 3)
		            {
			            return new Color(
				            byte.Parse(CssTrim(rgbParts[0]), CultureInfo.InvariantCulture),
				            byte.Parse(CssTrim(rgbParts[1]), CultureInfo.InvariantCulture),
				            byte.Parse(CssTrim(rgbParts[2]), CultureInfo.InvariantCulture)
			            );
		            }

		            if (rgbParts.Length == 4)
		            {
			            return new Color(
				            byte.Parse(CssTrim(rgbParts[0]), CultureInfo.InvariantCulture),
				            byte.Parse(CssTrim(rgbParts[1]), CultureInfo.InvariantCulture),
				            byte.Parse(CssTrim(rgbParts[2]), CultureInfo.InvariantCulture),
				            ParsePercent(CssTrim(rgbParts[3]))
			            );
		            }

		            if (rgbParts.Length == 5)
		            {
			            // rgb(a b c / d%)
			            return new Color(
				            byte.Parse(CssTrim(rgbParts[0]), CultureInfo.InvariantCulture),
				            byte.Parse(CssTrim(rgbParts[1]), CultureInfo.InvariantCulture),
				            byte.Parse(CssTrim(rgbParts[2]), CultureInfo.InvariantCulture),
				            ParsePercent(CssTrim(rgbParts[4])) // convert 0-100% to 0-255
			            );
		            }

		            static byte ParsePercent(ReadOnlySpan<char> str)
		            {
			            // 0-100%
			            if (str.EndsWith("%"))
			            {
				            str = str[..^1].Trim();
				            return (byte)(float.Parse(str, CultureInfo.InvariantCulture) * 2.55f);
			            }
			            
			            // 0-1
			            return (byte)(float.Parse(str, CultureInfo.InvariantCulture) * 255f);
		            }

		            static ReadOnlySpan<char> CssTrim(ReadOnlySpan<char> span)
		            {
			            span = span.Trim();
			            if (span.EndsWith(","))
			            {
				            span = span[..^1].Trim();
			            }
			            else if (span.StartsWith(","))
			            {
				            span = span[1..].Trim();
			            }
			            if (span.EndsWith(","))
			            {
				            span = span[..^1].Trim();
			            }
			            else if (span.StartsWith(","))
			            {
				            span = span[1..].Trim();
			            }

			            return span;
		            }
	            }
            }

            // special case. Html requires LightGrey, but .NET uses LightGray
            if (trimmed.Equals("LightGrey", StringComparison.OrdinalIgnoreCase))
            {
	            return Color.LightGray;
            }

            #region Named Colors
            
			if (trimmed.Equals("Transparent", StringComparison.OrdinalIgnoreCase))
			{
				return Color.Transparent;				
			}
			if (trimmed.Equals("AliceBlue", StringComparison.OrdinalIgnoreCase))
			{
				return Color.AliceBlue;				
			}
			if (trimmed.Equals("AntiqueWhite", StringComparison.OrdinalIgnoreCase))
			{
				return Color.AntiqueWhite;				
			}
			if (trimmed.Equals("Aqua", StringComparison.OrdinalIgnoreCase))
			{
				return Color.Aqua;				
			}
			if (trimmed.Equals("Aquamarine", StringComparison.OrdinalIgnoreCase))
			{
				return Color.Aquamarine;				
			}
			if (trimmed.Equals("Azure", StringComparison.OrdinalIgnoreCase))
			{
				return Color.Azure;				
			}
			if (trimmed.Equals("Beige", StringComparison.OrdinalIgnoreCase))
			{
				return Color.Beige;				
			}
			if (trimmed.Equals("Bisque", StringComparison.OrdinalIgnoreCase))
			{
				return Color.Bisque;				
			}
			if (trimmed.Equals("Black", StringComparison.OrdinalIgnoreCase))
			{
				return Color.Black;				
			}
			if (trimmed.Equals("BlanchedAlmond", StringComparison.OrdinalIgnoreCase))
			{
				return Color.BlanchedAlmond;				
			}
			if (trimmed.Equals("Blue", StringComparison.OrdinalIgnoreCase))
			{
				return Color.Blue;				
			}
			if (trimmed.Equals("BlueViolet", StringComparison.OrdinalIgnoreCase))
			{
				return Color.BlueViolet;				
			}
			if (trimmed.Equals("Brown", StringComparison.OrdinalIgnoreCase))
			{
				return Color.Brown;				
			}
			if (trimmed.Equals("BurlyWood", StringComparison.OrdinalIgnoreCase))
			{
				return Color.BurlyWood;				
			}
			if (trimmed.Equals("CadetBlue", StringComparison.OrdinalIgnoreCase))
			{
				return Color.CadetBlue;				
			}
			if (trimmed.Equals("Chartreuse", StringComparison.OrdinalIgnoreCase))
			{
				return Color.Chartreuse;				
			}
			if (trimmed.Equals("Chocolate", StringComparison.OrdinalIgnoreCase))
			{
				return Color.Chocolate;				
			}
			if (trimmed.Equals("Coral", StringComparison.OrdinalIgnoreCase))
			{
				return Color.Coral;				
			}
			if (trimmed.Equals("CornflowerBlue", StringComparison.OrdinalIgnoreCase))
			{
				return Color.CornflowerBlue;				
			}
			if (trimmed.Equals("Cornsilk", StringComparison.OrdinalIgnoreCase))
			{
				return Color.Cornsilk;				
			}
			if (trimmed.Equals("Crimson", StringComparison.OrdinalIgnoreCase))
			{
				return Color.Crimson;				
			}
			if (trimmed.Equals("Cyan", StringComparison.OrdinalIgnoreCase))
			{
				return Color.Cyan;				
			}
			if (trimmed.Equals("DarkBlue", StringComparison.OrdinalIgnoreCase))
			{
				return Color.DarkBlue;				
			}
			if (trimmed.Equals("DarkCyan", StringComparison.OrdinalIgnoreCase))
			{
				return Color.DarkCyan;				
			}
			if (trimmed.Equals("DarkGoldenrod", StringComparison.OrdinalIgnoreCase))
			{
				return Color.DarkGoldenrod;				
			}
			if (trimmed.Equals("DarkGray", StringComparison.OrdinalIgnoreCase))
			{
				return Color.DarkGray;				
			}
			if (trimmed.Equals("DarkGreen", StringComparison.OrdinalIgnoreCase))
			{
				return Color.DarkGreen;				
			}
			if (trimmed.Equals("DarkKhaki", StringComparison.OrdinalIgnoreCase))
			{
				return Color.DarkKhaki;				
			}
			if (trimmed.Equals("DarkMagenta", StringComparison.OrdinalIgnoreCase))
			{
				return Color.DarkMagenta;				
			}
			if (trimmed.Equals("DarkOliveGreen", StringComparison.OrdinalIgnoreCase))
			{
				return Color.DarkOliveGreen;				
			}
			if (trimmed.Equals("DarkOrange", StringComparison.OrdinalIgnoreCase))
			{
				return Color.DarkOrange;				
			}
			if (trimmed.Equals("DarkOrchid", StringComparison.OrdinalIgnoreCase))
			{
				return Color.DarkOrchid;				
			}
			if (trimmed.Equals("DarkRed", StringComparison.OrdinalIgnoreCase))
			{
				return Color.DarkRed;				
			}
			if (trimmed.Equals("DarkSalmon", StringComparison.OrdinalIgnoreCase))
			{
				return Color.DarkSalmon;				
			}
			if (trimmed.Equals("DarkSeaGreen", StringComparison.OrdinalIgnoreCase))
			{
				return Color.DarkSeaGreen;				
			}
			if (trimmed.Equals("DarkSlateBlue", StringComparison.OrdinalIgnoreCase))
			{
				return Color.DarkSlateBlue;				
			}
			if (trimmed.Equals("DarkSlateGray", StringComparison.OrdinalIgnoreCase))
			{
				return Color.DarkSlateGray;				
			}
			if (trimmed.Equals("DarkTurquoise", StringComparison.OrdinalIgnoreCase))
			{
				return Color.DarkTurquoise;				
			}
			if (trimmed.Equals("DarkViolet", StringComparison.OrdinalIgnoreCase))
			{
				return Color.DarkViolet;				
			}
			if (trimmed.Equals("DeepPink", StringComparison.OrdinalIgnoreCase))
			{
				return Color.DeepPink;				
			}
			if (trimmed.Equals("DeepSkyBlue", StringComparison.OrdinalIgnoreCase))
			{
				return Color.DeepSkyBlue;				
			}
			if (trimmed.Equals("DimGray", StringComparison.OrdinalIgnoreCase))
			{
				return Color.DimGray;				
			}
			if (trimmed.Equals("DodgerBlue", StringComparison.OrdinalIgnoreCase))
			{
				return Color.DodgerBlue;				
			}
			if (trimmed.Equals("Firebrick", StringComparison.OrdinalIgnoreCase))
			{
				return Color.Firebrick;				
			}
			if (trimmed.Equals("FloralWhite", StringComparison.OrdinalIgnoreCase))
			{
				return Color.FloralWhite;				
			}
			if (trimmed.Equals("ForestGreen", StringComparison.OrdinalIgnoreCase))
			{
				return Color.ForestGreen;				
			}
			if (trimmed.Equals("Fuchsia", StringComparison.OrdinalIgnoreCase))
			{
				return Color.Fuchsia;				
			}
			if (trimmed.Equals("Gainsboro", StringComparison.OrdinalIgnoreCase))
			{
				return Color.Gainsboro;				
			}
			if (trimmed.Equals("GhostWhite", StringComparison.OrdinalIgnoreCase))
			{
				return Color.GhostWhite;				
			}
			if (trimmed.Equals("Gold", StringComparison.OrdinalIgnoreCase))
			{
				return Color.Gold;				
			}
			if (trimmed.Equals("Goldenrod", StringComparison.OrdinalIgnoreCase))
			{
				return Color.Goldenrod;				
			}
			if (trimmed.Equals("Gray", StringComparison.OrdinalIgnoreCase))
			{
				return Color.Gray;				
			}
			if (trimmed.Equals("Green", StringComparison.OrdinalIgnoreCase))
			{
				return Color.Green;				
			}
			if (trimmed.Equals("GreenYellow", StringComparison.OrdinalIgnoreCase))
			{
				return Color.GreenYellow;				
			}
			if (trimmed.Equals("Honeydew", StringComparison.OrdinalIgnoreCase))
			{
				return Color.Honeydew;				
			}
			if (trimmed.Equals("HotPink", StringComparison.OrdinalIgnoreCase))
			{
				return Color.HotPink;				
			}
			if (trimmed.Equals("IndianRed", StringComparison.OrdinalIgnoreCase))
			{
				return Color.IndianRed;				
			}
			if (trimmed.Equals("Indigo", StringComparison.OrdinalIgnoreCase))
			{
				return Color.Indigo;				
			}
			if (trimmed.Equals("Ivory", StringComparison.OrdinalIgnoreCase))
			{
				return Color.Ivory;				
			}
			if (trimmed.Equals("Khaki", StringComparison.OrdinalIgnoreCase))
			{
				return Color.Khaki;				
			}
			if (trimmed.Equals("Lavender", StringComparison.OrdinalIgnoreCase))
			{
				return Color.Lavender;				
			}
			if (trimmed.Equals("LavenderBlush", StringComparison.OrdinalIgnoreCase))
			{
				return Color.LavenderBlush;				
			}
			if (trimmed.Equals("LawnGreen", StringComparison.OrdinalIgnoreCase))
			{
				return Color.LawnGreen;				
			}
			if (trimmed.Equals("LemonChiffon", StringComparison.OrdinalIgnoreCase))
			{
				return Color.LemonChiffon;				
			}
			if (trimmed.Equals("LightBlue", StringComparison.OrdinalIgnoreCase))
			{
				return Color.LightBlue;				
			}
			if (trimmed.Equals("LightCoral", StringComparison.OrdinalIgnoreCase))
			{
				return Color.LightCoral;				
			}
			if (trimmed.Equals("LightCyan", StringComparison.OrdinalIgnoreCase))
			{
				return Color.LightCyan;				
			}
			if (trimmed.Equals("LightGoldenrodYellow", StringComparison.OrdinalIgnoreCase))
			{
				return Color.LightGoldenrodYellow;				
			}
			if (trimmed.Equals("LightGray", StringComparison.OrdinalIgnoreCase))
			{
				return Color.LightGray;				
			}
			if (trimmed.Equals("LightGreen", StringComparison.OrdinalIgnoreCase))
			{
				return Color.LightGreen;				
			}
			if (trimmed.Equals("LightPink", StringComparison.OrdinalIgnoreCase))
			{
				return Color.LightPink;				
			}
			if (trimmed.Equals("LightSalmon", StringComparison.OrdinalIgnoreCase))
			{
				return Color.LightSalmon;				
			}
			if (trimmed.Equals("LightSeaGreen", StringComparison.OrdinalIgnoreCase))
			{
				return Color.LightSeaGreen;				
			}
			if (trimmed.Equals("LightSkyBlue", StringComparison.OrdinalIgnoreCase))
			{
				return Color.LightSkyBlue;				
			}
			if (trimmed.Equals("LightSlateGray", StringComparison.OrdinalIgnoreCase))
			{
				return Color.LightSlateGray;				
			}
			if (trimmed.Equals("LightSteelBlue", StringComparison.OrdinalIgnoreCase))
			{
				return Color.LightSteelBlue;				
			}
			if (trimmed.Equals("LightYellow", StringComparison.OrdinalIgnoreCase))
			{
				return Color.LightYellow;				
			}
			if (trimmed.Equals("Lime", StringComparison.OrdinalIgnoreCase))
			{
				return Color.Lime;				
			}
			if (trimmed.Equals("LimeGreen", StringComparison.OrdinalIgnoreCase))
			{
				return Color.LimeGreen;				
			}
			if (trimmed.Equals("Linen", StringComparison.OrdinalIgnoreCase))
			{
				return Color.Linen;				
			}
			if (trimmed.Equals("Magenta", StringComparison.OrdinalIgnoreCase))
			{
				return Color.Magenta;				
			}
			if (trimmed.Equals("Maroon", StringComparison.OrdinalIgnoreCase))
			{
				return Color.Maroon;				
			}
			if (trimmed.Equals("MediumAquamarine", StringComparison.OrdinalIgnoreCase))
			{
				return Color.MediumAquamarine;				
			}
			if (trimmed.Equals("MediumBlue", StringComparison.OrdinalIgnoreCase))
			{
				return Color.MediumBlue;				
			}
			if (trimmed.Equals("MediumOrchid", StringComparison.OrdinalIgnoreCase))
			{
				return Color.MediumOrchid;				
			}
			if (trimmed.Equals("MediumPurple", StringComparison.OrdinalIgnoreCase))
			{
				return Color.MediumPurple;				
			}
			if (trimmed.Equals("MediumSeaGreen", StringComparison.OrdinalIgnoreCase))
			{
				return Color.MediumSeaGreen;				
			}
			if (trimmed.Equals("MediumSlateBlue", StringComparison.OrdinalIgnoreCase))
			{
				return Color.MediumSlateBlue;				
			}
			if (trimmed.Equals("MediumSpringGreen", StringComparison.OrdinalIgnoreCase))
			{
				return Color.MediumSpringGreen;				
			}
			if (trimmed.Equals("MediumTurquoise", StringComparison.OrdinalIgnoreCase))
			{
				return Color.MediumTurquoise;				
			}
			if (trimmed.Equals("MediumVioletRed", StringComparison.OrdinalIgnoreCase))
			{
				return Color.MediumVioletRed;				
			}
			if (trimmed.Equals("MidnightBlue", StringComparison.OrdinalIgnoreCase))
			{
				return Color.MidnightBlue;				
			}
			if (trimmed.Equals("MintCream", StringComparison.OrdinalIgnoreCase))
			{
				return Color.MintCream;				
			}
			if (trimmed.Equals("MistyRose", StringComparison.OrdinalIgnoreCase))
			{
				return Color.MistyRose;				
			}
			if (trimmed.Equals("Moccasin", StringComparison.OrdinalIgnoreCase))
			{
				return Color.Moccasin;				
			}
			if (trimmed.Equals("NavajoWhite", StringComparison.OrdinalIgnoreCase))
			{
				return Color.NavajoWhite;				
			}
			if (trimmed.Equals("Navy", StringComparison.OrdinalIgnoreCase))
			{
				return Color.Navy;				
			}
			if (trimmed.Equals("OldLace", StringComparison.OrdinalIgnoreCase))
			{
				return Color.OldLace;				
			}
			if (trimmed.Equals("Olive", StringComparison.OrdinalIgnoreCase))
			{
				return Color.Olive;				
			}
			if (trimmed.Equals("OliveDrab", StringComparison.OrdinalIgnoreCase))
			{
				return Color.OliveDrab;				
			}
			if (trimmed.Equals("Orange", StringComparison.OrdinalIgnoreCase))
			{
				return Color.Orange;				
			}
			if (trimmed.Equals("OrangeRed", StringComparison.OrdinalIgnoreCase))
			{
				return Color.OrangeRed;				
			}
			if (trimmed.Equals("Orchid", StringComparison.OrdinalIgnoreCase))
			{
				return Color.Orchid;				
			}
			if (trimmed.Equals("PaleGoldenrod", StringComparison.OrdinalIgnoreCase))
			{
				return Color.PaleGoldenrod;				
			}
			if (trimmed.Equals("PaleGreen", StringComparison.OrdinalIgnoreCase))
			{
				return Color.PaleGreen;				
			}
			if (trimmed.Equals("PaleTurquoise", StringComparison.OrdinalIgnoreCase))
			{
				return Color.PaleTurquoise;				
			}
			if (trimmed.Equals("PaleVioletRed", StringComparison.OrdinalIgnoreCase))
			{
				return Color.PaleVioletRed;				
			}
			if (trimmed.Equals("PapayaWhip", StringComparison.OrdinalIgnoreCase))
			{
				return Color.PapayaWhip;				
			}
			if (trimmed.Equals("PeachPuff", StringComparison.OrdinalIgnoreCase))
			{
				return Color.PeachPuff;				
			}
			if (trimmed.Equals("Peru", StringComparison.OrdinalIgnoreCase))
			{
				return Color.Peru;				
			}
			if (trimmed.Equals("Pink", StringComparison.OrdinalIgnoreCase))
			{
				return Color.Pink;				
			}
			if (trimmed.Equals("Plum", StringComparison.OrdinalIgnoreCase))
			{
				return Color.Plum;				
			}
			if (trimmed.Equals("PowderBlue", StringComparison.OrdinalIgnoreCase))
			{
				return Color.PowderBlue;				
			}
			if (trimmed.Equals("Purple", StringComparison.OrdinalIgnoreCase))
			{
				return Color.Purple;				
			}
			if (trimmed.Equals("Red", StringComparison.OrdinalIgnoreCase))
			{
				return Color.Red;				
			}
			if (trimmed.Equals("RosyBrown", StringComparison.OrdinalIgnoreCase))
			{
				return Color.RosyBrown;				
			}
			if (trimmed.Equals("RoyalBlue", StringComparison.OrdinalIgnoreCase))
			{
				return Color.RoyalBlue;				
			}
			if (trimmed.Equals("SaddleBrown", StringComparison.OrdinalIgnoreCase))
			{
				return Color.SaddleBrown;				
			}
			if (trimmed.Equals("Salmon", StringComparison.OrdinalIgnoreCase))
			{
				return Color.Salmon;				
			}
			if (trimmed.Equals("SandyBrown", StringComparison.OrdinalIgnoreCase))
			{
				return Color.SandyBrown;				
			}
			if (trimmed.Equals("SeaGreen", StringComparison.OrdinalIgnoreCase))
			{
				return Color.SeaGreen;				
			}
			if (trimmed.Equals("SeaShell", StringComparison.OrdinalIgnoreCase))
			{
				return Color.SeaShell;				
			}
			if (trimmed.Equals("Sienna", StringComparison.OrdinalIgnoreCase))
			{
				return Color.Sienna;				
			}
			if (trimmed.Equals("Silver", StringComparison.OrdinalIgnoreCase))
			{
				return Color.Silver;				
			}
			if (trimmed.Equals("SkyBlue", StringComparison.OrdinalIgnoreCase))
			{
				return Color.SkyBlue;				
			}
			if (trimmed.Equals("SlateBlue", StringComparison.OrdinalIgnoreCase))
			{
				return Color.SlateBlue;				
			}
			if (trimmed.Equals("SlateGray", StringComparison.OrdinalIgnoreCase))
			{
				return Color.SlateGray;				
			}
			if (trimmed.Equals("Snow", StringComparison.OrdinalIgnoreCase))
			{
				return Color.Snow;				
			}
			if (trimmed.Equals("SpringGreen", StringComparison.OrdinalIgnoreCase))
			{
				return Color.SpringGreen;				
			}
			if (trimmed.Equals("SteelBlue", StringComparison.OrdinalIgnoreCase))
			{
				return Color.SteelBlue;				
			}
			if (trimmed.Equals("Tan", StringComparison.OrdinalIgnoreCase))
			{
				return Color.Tan;				
			}
			if (trimmed.Equals("Teal", StringComparison.OrdinalIgnoreCase))
			{
				return Color.Teal;				
			}
			if (trimmed.Equals("Thistle", StringComparison.OrdinalIgnoreCase))
			{
				return Color.Thistle;				
			}
			if (trimmed.Equals("Tomato", StringComparison.OrdinalIgnoreCase))
			{
				return Color.Tomato;				
			}
			if (trimmed.Equals("Turquoise", StringComparison.OrdinalIgnoreCase))
			{
				return Color.Turquoise;				
			}
			if (trimmed.Equals("Violet", StringComparison.OrdinalIgnoreCase))
			{
				return Color.Violet;				
			}
			if (trimmed.Equals("Wheat", StringComparison.OrdinalIgnoreCase))
			{
				return Color.Wheat;				
			}
			if (trimmed.Equals("White", StringComparison.OrdinalIgnoreCase))
			{
				return Color.White;				
			}
			if (trimmed.Equals("WhiteSmoke", StringComparison.OrdinalIgnoreCase))
			{
				return Color.WhiteSmoke;				
			}
			if (trimmed.Equals("Yellow", StringComparison.OrdinalIgnoreCase))
			{
				return Color.Yellow;				
			}
			if (trimmed.Equals("YellowGreen", StringComparison.OrdinalIgnoreCase))
			{
				return Color.YellowGreen;				
			}
			
			#endregion
            
            throw new FormatException($"Invalid color format: {str}. Expected 'R,G,B' or 'R,G,B,A', or a valid hex color, or a named XNA color.");
        }
        return base.ConvertFrom(context, culture, value);
    }
}

/// <summary>
/// Type converter for Font - parses "FontFamily,Flags,Size" format.
/// </summary>
public class FontTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string str)
        {
            var parts = str.Split(',');
            if (parts.Length == 3)
            {
                var fontFamily = Enum.Parse<FontFamily>(parts[0].AsSpan().Trim(), ignoreCase: true);
                var flags = Enum.Parse<FontStyle>(parts[1].AsSpan().Trim(), ignoreCase: true);
                var sizeStr = parts[2].AsSpan().Trim();
                if (sizeStr.EndsWith("px", StringComparison.OrdinalIgnoreCase))
                {
                    sizeStr = sizeStr[..^2].Trim();
                }
                var size = float.Parse(sizeStr, CultureInfo.InvariantCulture);
                return new Font(fontFamily, flags, size);
            }
            
            parts = str.Split(' ');
            if (parts.Length >= 2)
            {
                var flags = FontStyle.Plain;
                for (var i = 0; i < parts.Length - 2; i++)
                {
                    var part = parts[i].AsSpan().Trim();
                    if (part.Equals("bold", StringComparison.OrdinalIgnoreCase))
                        flags |= FontStyle.Bold;
                    else if (part.Equals("italic", StringComparison.OrdinalIgnoreCase))
                        flags |= FontStyle.Italic;
                    else
                        throw new FormatException($"Invalid font style: {part} in '{str}'. Expected 'bold' or 'italic'.");
                }
                var sizeStr = parts[^2].AsSpan().Trim();
                if (sizeStr.EndsWith("px", StringComparison.OrdinalIgnoreCase))
                {
                    sizeStr = sizeStr[..^2].Trim();
                }
                var size = float.Parse(sizeStr, CultureInfo.InvariantCulture);
                var fontFamily = Enum.Parse<FontFamily>(parts[^1].AsSpan().Trim(), ignoreCase: true);
                return new Font(fontFamily, flags, size);
            }
            throw new FormatException($"Invalid font format: {str}. Expected '[style*] <size> <family>' or 'FontFamily,Style,Size'.");
        }
        return base.ConvertFrom(context, culture, value);
    }
}
