using NFMWorld.UI.Hud;
using WorldXaml.UI.Yoga;

namespace NFMWorldLibrary.Backend.Gamemodes;

[ClientOnly]
public class DefaultHudManager : UIManager, IHud
{
    private readonly FlexPanel _rootPanel = new();
    private readonly OverlayPanel _overlay = new();

    public HudViewModel DataContext
    {
        get;
        set
        {
            field = value;
            _rootPanel.DataContext = field;
        }
    } = new();

    public DefaultHudManager()
    {
        RootPanel = _rootPanel;
        
        _rootPanel.DataContext = DataContext;
        _rootPanel.Children.Add(_overlay);
        
        _overlay.ContentChildren.Add(new PowerDamageBars());
        _overlay.ContentChildren.Add(new CentralTextView());
    }
}