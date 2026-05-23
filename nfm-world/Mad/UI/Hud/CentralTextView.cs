using WorldXaml.UI.Yoga;

namespace NFMWorld.UI.Hud;

public partial class CentralTextView : View
{
    public new CentralTextViewModel DataContext => (CentralTextViewModel)base.DataContext!;

    public CentralTextView()
    {
        base.DataContext = new CentralTextViewModel();
        InitializeComponent();
    }
}
