using CommunityToolkit.Mvvm.ComponentModel;
using NFMWorld.Util;
using WorldXaml.UI.Yoga;

namespace NFMWorld.UI.Test;

public partial class XamlTestViewModel : ObservableObject
{
    // Text bindings
    [ObservableProperty] public partial string Title { get; set; } = "XAML Test View";
    [ObservableProperty] public partial string Subtitle { get; set; } = "All systems operational";
    [ObservableProperty] public partial int Counter { get; set; } = 0;

    // Color bindings
    [ObservableProperty] public partial Color TitleColor { get; set; } = new Color(255, 255, 255, 255);
    [ObservableProperty] public partial Color SubtitleColor { get; set; } = new Color(180, 180, 180, 255);
    [ObservableProperty] public partial Color AccentColor { get; set; } = new Color(100, 200, 255, 255);

    // Font bindings
    [ObservableProperty] public partial Font TitleFont { get; set; } = new Font(FontFamily.Adventure, FontStyle.Bold, 32);
    [ObservableProperty] public partial Font BodyFont { get; set; } = new Font(FontFamily.Adventure, FontStyle.Plain, 18);

    // Visibility binding
    [ObservableProperty] public partial Visibility BadgeVisibility { get; set; } = Visibility.Visible;

    // Orientation binding for HStack
    [ObservableProperty] public partial StackOrientation StackDir { get; set; } = StackOrientation.Horizontal;

    public void Tick()
    {
        Counter++;
        CounterModulo = Counter % 63;
    }

    [ObservableProperty] public partial int CounterModulo { get; set; }
}
