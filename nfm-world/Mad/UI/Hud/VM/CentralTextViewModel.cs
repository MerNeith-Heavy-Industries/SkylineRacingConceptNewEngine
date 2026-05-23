using CommunityToolkit.Mvvm.ComponentModel;
using NFMWorld.Util;
using WorldXaml.UI.Yoga;

namespace NFMWorld.UI.Hud;

public partial class CentralTextViewModel : ObservableObject
{
    [ObservableProperty] public partial Visibility CenterTextVisibility { get; set; }
    [ObservableProperty] public partial string CenterText { get; set; }
    [ObservableProperty] public partial Color CenterTextColor { get; set; }
    [ObservableProperty] public partial Font CenterTextFont { get; set; }
    [ObservableProperty] public partial Color CenterTextStrokeColor { get; set; }
}