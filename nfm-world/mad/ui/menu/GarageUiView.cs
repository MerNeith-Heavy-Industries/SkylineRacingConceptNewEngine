using nfm_world.ui.yoga;
using nfm_world.ui.yoga.xaml;

namespace nfm_world.ui.menu;

public partial class GarageUiView : View
{
    public GarageUiView()
    {
        InitializeComponent();
        XamlHotReload.Register(this, "mad/ui/menu/GarageUiView.xaml");
    }
}
