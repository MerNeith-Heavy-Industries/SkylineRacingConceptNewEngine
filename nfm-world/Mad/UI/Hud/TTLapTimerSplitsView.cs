using WorldXaml.UI.Yoga;

namespace NFMWorld.UI.Hud;

public partial class TTLapTimerSplitsView : View
{
    public new TTLapTimerSplitsViewModel DataContext => (TTLapTimerSplitsViewModel)base.DataContext!;

    public TTLapTimerSplitsView()
    {
        base.DataContext = new TTLapTimerSplitsViewModel();
        InitializeComponent();
    }
}
