using System.ComponentModel;
using nfm_world.ui.yoga.xaml;

namespace nfm_world.util;

[TypeConverter(typeof(FontTypeConverter))]
public readonly record struct Font(FontFamily FontFamily, int Flags, float Size)
{
    public const int PLAIN = 0;
    public const int BOLD = 1;
    public const int ITALIC = 2;
}

public enum FontFamily : byte
{
    DroidSans,
    Adventure,
    AdventureHollow,
    RobotoMono
}