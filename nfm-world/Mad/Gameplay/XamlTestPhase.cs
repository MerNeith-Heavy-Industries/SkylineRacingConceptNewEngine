using NFMWorld.DriverInterface;
using NFMWorld.UI.Test;
using WorldXaml.UI.Yoga;

namespace NFMWorld.Gameplay;

public class XamlTestPhase : BasePhase
{
    public XamlTestView _testView = new();
    private bool _mounted;

    public override void Enter()
    {
        base.Enter();
        _testView.Mount(new FlexPanel());
        _mounted = true;
    }

    public override void Render(float alpha)
    {
        base.Render(alpha);
        _testView.Update();
    }

    public override void GameTick()
    {
        base.GameTick();
        _testView.Tick();
        _testView.Update();
    }
}