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

/// <summary>
/// Type converter for Yoga YGFlexDirection enum.
/// </summary>
public class YGFlexDirectionTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string str)
        {
            return Enum.Parse<YGFlexDirection>(str.Trim(), ignoreCase: true);
        }
        return base.ConvertFrom(context, culture, value);
    }
}

/// <summary>
/// Type converter for Yoga YGAlign enum.
/// </summary>
public class YGAlignTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string str)
        {
            return Enum.Parse<YGAlign>(str.Trim(), ignoreCase: true);
        }
        return base.ConvertFrom(context, culture, value);
    }
}

/// <summary>
/// Type converter for Yoga YGJustify enum.
/// </summary>
public class YGJustifyTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string str)
        {
            return Enum.Parse<YGJustify>(str.Trim(), ignoreCase: true);
        }
        return base.ConvertFrom(context, culture, value);
    }
}

/// <summary>
/// Type converter for Yoga YGDisplay enum.
/// </summary>
public class YGDisplayTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string str)
        {
            return Enum.Parse<YGDisplay>(str.Trim(), ignoreCase: true);
        }
        return base.ConvertFrom(context, culture, value);
    }
}

/// <summary>
/// Type converter for Node.MeasurementPadding.
/// Parses a single float value.
/// </summary>
public class MeasurementPaddingTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string str)
        {
            var trimmed = str.Trim();
            if (float.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var floatValue))
            {
                return (Node.MeasurementPadding)floatValue;
            }
            throw new FormatException($"Invalid padding format: {str}. Expected a number.");
        }
        return base.ConvertFrom(context, culture, value);
    }
}

/// <summary>
/// Type converter for Node.MeasurementGap.
/// Parses a single float value.
/// </summary>
public class MeasurementGapTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string str)
        {
            var trimmed = str.Trim();
            if (float.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var floatValue))
            {
                return (Node.MeasurementGap)floatValue;
            }
            throw new FormatException($"Invalid gap format: {str}. Expected a number.");
        }
        return base.ConvertFrom(context, culture, value);
    }
}

/// <summary>
/// Type converter for Node.MeasurementWidthHeight.
/// Parses values like "100", "50%", "auto", "stretch", etc.
/// </summary>
public class MeasurementWidthHeightTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string str)
        {
            var trimmed = str.Trim().ToLowerInvariant();

            if (trimmed == "auto")
                return Node.MeasurementWidthHeight.Auto();
            if (trimmed == "stretch")
                return Node.MeasurementWidthHeight.Stretch();
            if (trimmed == "fit-content" || trimmed == "fitcontent")
                return Node.MeasurementWidthHeight.FitContent();
            if (trimmed == "max-content" || trimmed == "maxcontent")
                return Node.MeasurementWidthHeight.MaxContent();
            if (trimmed.EndsWith('%'))
            {
                var percentValue = float.Parse(trimmed.TrimEnd('%'), CultureInfo.InvariantCulture);
                return Node.MeasurementWidthHeight.Percent(percentValue);
            }
            if (float.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var floatValue))
            {
                return (Node.MeasurementWidthHeight)floatValue;
            }

            throw new FormatException($"Invalid measurement format: {str}. Expected a number, percentage, 'auto', 'stretch', 'fit-content', or 'max-content'.");
        }
        return base.ConvertFrom(context, culture, value);
    }
}
