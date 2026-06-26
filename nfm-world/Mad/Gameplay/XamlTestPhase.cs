using NFMWorld.DriverInterface;
using NFMWorld.Reactor;
using NFMWorld.UI.Test;
using WorldXaml.UI.Yoga;

namespace NFMWorld.Gameplay;

public class XamlTestPhase : BasePhase
{
    private XamlTestView _testView = new();
    private ReactorDom _dom;
    private FlexPanel _container = new();

    public override void Enter()
    {
        base.Enter();
        _dom = new ReactorDom(SynchronizationContext.Current ?? new SynchronizationContext());
        _dom.Mount(_container, ComponentNodeFactory.Create(_testView));
    }

    public override void Render(float alpha)
    {
        base.Render(alpha);
        _dom.Mount(_container, ComponentNodeFactory.Create(_testView));
    }

    public override void GameTick()
    {
        base.GameTick();
        _testView.Tick();
        _dom.Mount(_container, ComponentNodeFactory.Create(_testView));
    }
}