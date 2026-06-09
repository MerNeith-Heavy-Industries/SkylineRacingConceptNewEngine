using WorldXaml.UI.Yoga;

namespace NFMWorld.UI.Menu;

public partial class MainMenuView : View
{
    public new MainMenuViewModel DataContext => (MainMenuViewModel)base.DataContext!;

    public MainMenuView()
    {
        base.DataContext = new MainMenuViewModel();
        InitializeComponent();
    }
}
