using NFMWorld.UI.Test;

namespace NFMWorld.Gameplay;

public class XamlTestPhase : BasePhase
{
    public XamlTestView _testView = new XamlTestView();

    public override void Render(float alpha)
    {
        base.Render(alpha);
        _testView.LayoutAndRender(G.Viewport);
    }

    public override void GameTick()
    {
        base.GameTick();
        _testView.DataContext.Tick();
    }
}