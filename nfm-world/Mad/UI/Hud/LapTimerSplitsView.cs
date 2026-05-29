using WorldXaml.UI.Yoga;

namespace NFMWorld.UI.Hud;

/// <summary>
/// Code-behind for LapTimerSplitsView.xaml.
/// </summary>
public partial class LapTimerSplitsView : View
{
    public new LapTimerSplitsViewModel DataContext => (LapTimerSplitsViewModel)base.DataContext!;

    public LapTimerSplitsView()
    {
        base.DataContext = new LapTimerSplitsViewModel();
        InitializeComponent();
    }
}
