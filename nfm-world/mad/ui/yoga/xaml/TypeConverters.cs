using System.ComponentModel;
using System.Globalization;
using nfm_world.util;
using Yoga;

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
                    byte.Parse(parts[0].Trim()),
                    byte.Parse(parts[1].Trim()),
                    byte.Parse(parts[2].Trim())
                );
            }
            if (parts.Length == 4)
            {
                return new Color(
                    byte.Parse(parts[0].Trim()),
                    byte.Parse(parts[1].Trim()),
                    byte.Parse(parts[2].Trim()),
                    byte.Parse(parts[3].Trim())
                );
            }
            throw new FormatException($"Invalid color format: {str}. Expected 'R,G,B' or 'R,G,B,A'.");
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
                var fontFamily = Enum.Parse<FontFamily>(parts[0].Trim(), ignoreCase: true);
                var flags = int.Parse(parts[1].Trim());
                var size = float.Parse(parts[2].Trim(), CultureInfo.InvariantCulture);
                return new Font(fontFamily, flags, size);
            }
            throw new FormatException($"Invalid font format: {str}. Expected 'FontFamily,Flags,Size'.");
        }
        return base.ConvertFrom(context, culture, value);
    }
}
