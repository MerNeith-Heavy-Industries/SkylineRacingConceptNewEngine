using CommunityToolkit.Mvvm.ComponentModel;

namespace NFMWorld.UI.Hud;

public partial class TTLapTimerSplitsViewModel : ObservableObject
{
    [ObservableProperty] public partial int CurrentLap { get; set; }
    [ObservableProperty] public partial int TotalLaps { get; set; }
}