using Avalonia;
using nfm_world.ui.yoga;

namespace nfm_world.ui.hud;

/// <summary>
/// A XAML-defined view showing lap timer and splits information.
/// This demonstrates the yoga XAML layout system.
/// </summary>
public class LapTimerSplitsView : Node
{
    public LapTimerSplitsView()
    {
        // InitializeComponent();
    }

    public void SetLapText(int currentLap, int totalLaps)
    {
        // LapText.Text = $"{currentLap}/{totalLaps}";
    }
}
