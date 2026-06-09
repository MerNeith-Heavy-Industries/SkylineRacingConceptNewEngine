using CommunityToolkit.Mvvm.ComponentModel;
using NFMWorld.DriverInterface;

namespace NFMWorld.UI.Menu;

public partial class MainMenuItemViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string Text { get; set; } = "";

    [ObservableProperty]
    public partial string Description { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BackgroundFillColor))]
    [NotifyPropertyChangedFor(nameof(BorderStrokeColor))]
    public partial bool IsHovered { get; set; }

    /// <summary>
    /// Orange fill when hovered, transparent otherwise.
    /// </summary>
    public Color BackgroundFillColor => IsHovered
        ? new Color(255, 140, 0, 255)
        : new Color(0, 0, 0, 0);

    /// <summary>
    /// Always orange border.
    /// </summary>
    public Color BorderStrokeColor => new(255, 140, 0, 255);

    public Action? OnClick { get; set; }
}
