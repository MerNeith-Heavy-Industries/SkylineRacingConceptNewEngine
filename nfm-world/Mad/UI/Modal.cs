using NFMWorld.Reactor;
using static NFMWorld.Reactor.Nodes;

namespace NFMWorld.UI;

public class Modal(VNode modal, bool isVisible) : Component
{
    protected override VNode Render()
    {
        return FlexPanel(
            display: isVisible ? Display.Flex : Display.None,
            position: Position.Absolute,
            top: 0, left: 0, right: 0, bottom: 0,
            
            // Center content horizontally and vertically
            flexDirection: FlexDirection.Column,
            justifyContent: Justify.Center,
            alignItems: Align.Center,
            children: modal
        );
    }
}