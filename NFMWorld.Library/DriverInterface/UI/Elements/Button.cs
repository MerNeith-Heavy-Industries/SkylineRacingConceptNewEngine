using System.ComponentModel;
using NFMWorld.DriverInterface.UI;
using NFMWorld.Reactor;
using WorldXaml.UI.Base;
using WorldXaml.UI.Yoga;
using static NFMWorld.DriverInterface.UI.Nodes;
using static WorldXaml.UI.Yoga.Nodes;
using Component = NFMWorld.Reactor.Component;

namespace NFMWorldLibrary.DriverInterface.UI.Elements;

public class Button(
    Color? borderColor = null,
    Color? backgroundColor = null,
    Color? backgroundHoverColor = null,
    int borderTopLeftRadius = 5,
    int borderTopRightRadius = 5,
    int borderBottomLeftRadius = 5,
    int borderBottomRightRadius = 5
) : Component
{
    protected override VNode Render()
    {
        var (isHovered, setIsHovered) = UseState(false);
        
        return FlexPanel(
            isFocusable: true,
            mouseEntered: _ =>
            {
                setIsHovered(true);
            },
            mouseLeft: _ =>
            {
                setIsHovered(false);
            },
            children: [
                SolidBox(
                    borderColor: borderColor,
                    backgroundColor: isHovered ? backgroundHoverColor : backgroundColor,
                    border: isHovered ? 3 : 1,
                    borderTopLeftRadius: borderTopLeftRadius,
                    borderTopRightRadius: borderTopRightRadius,
                    borderBottomLeftRadius: borderBottomLeftRadius,
                    borderBottomRightRadius: borderBottomRightRadius
                )
            ]
        );
    }
}
