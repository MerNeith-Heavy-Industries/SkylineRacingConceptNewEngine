using NFMWorld.UI.Hud;
using NFMWorldLibrary.DriverInterface.UI.Elements;
using WorldXaml.UI.Yoga;

namespace NFMWorld.UI.Menu;

public partial class LoginModal : Modal
{
    public new LoginModalViewModel DataContext => (LoginModalViewModel)base.DataContext!;

    public LoginModal()
    {
        base.DataContext = new LoginModalViewModel();
        InitializeComponent();
    }
}
