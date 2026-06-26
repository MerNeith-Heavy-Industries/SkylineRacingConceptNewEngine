using NFMWorld.DriverInterface;
using NFMWorld.Reactor;
using NFMWorld.UI.Test;
using WorldXaml.UI.Yoga;
using static NFMWorld.UI.Test.Nodes;

namespace NFMWorld.Gameplay;

public class XamlTestPhase : BasePhase
{
    private ReactorDom _dom;
    private FlexPanel _container = new();
    private int _counter;

    public override void Enter()
    {
        base.Enter();
        _dom = new ReactorDom(SynchronizationContext.Current ?? new SynchronizationContext());
        _dom.Mount(_container, XamlTestView(_counter));
    }

    public override void Render(float alpha)
    {
        base.Render(alpha);
        _container.LayoutAndRender(G.Viewport);
    }

    public override void GameTick()
    {
        base.GameTick();
        _dom.Mount(_container, XamlTestView(++_counter));
    }
}