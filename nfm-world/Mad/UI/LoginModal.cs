using NFMWorld.Reactor;
using WorldXaml.UI.Yoga;
using static WorldXaml.UI.Yoga.Nodes;

namespace NFMWorld.UI.Menu;

public class LoginModal : Component
{
    public LoginModal()
    {
        DisableMemo();
    }

    protected override VNode Render()
    {
        return View(
            name: "LoginModal",
            flexDirection: YgFlexDirection.Column,
            alignItems: YgAlign.Center,
            justifyContent: YgJustify.Center,
            padding: 20f,
            gap: 16f,
            children:
                FlexPanel(name: "LoginContent")
        );
    }
}
