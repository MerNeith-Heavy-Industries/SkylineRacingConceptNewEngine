using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NFMWorld.DriverInterface;

namespace NFMWorld.UI.Menu;

public partial class MainMenuItemViewModel : ObservableObject
{
    [ObservableProperty]
    public partial Color ButtonTextColor { get; set; } = new(255, 140, 0, 255);

    [ObservableProperty]
    public partial Color ButtonHoverBgColor { get; set; } = new(255, 140, 0, 255);

    [ObservableProperty]
    public partial string Text { get; set; } = "";

    [ObservableProperty]
    public partial string Description { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BackgroundFillColor))]
    [NotifyPropertyChangedFor(nameof(BorderStrokeColor))]
    [NotifyPropertyChangedFor(nameof(BorderSize))]
    public partial bool IsHovered { get; set; }

    /// <summary>
    /// Orange fill when hovered, transparent otherwise.
    /// </summary>
    public Color BackgroundFillColor => IsHovered
        ? ButtonHoverBgColor
        : Color.Transparent;

    /// <summary>
    /// Always orange border.
    /// </summary>
    public Color BorderStrokeColor => new(255, 140, 0, 255);

    public int BorderSize => IsHovered ? 3 : 1;

    public event Action? OnClick;

    [RelayCommand]
    public void Clicked()
    {
        OnClick?.Invoke();
    }
}
