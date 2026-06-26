using NFMWorld.Reactor;
using WorldXaml.UI.Yoga;
using static WorldXaml.UI.Yoga.Nodes;
using Node = WorldXaml.UI.Yoga.Node;

namespace NFMWorldLibrary.DriverInterface.UI.Elements;

/// <summary>
/// A component that renders its children centered on an absolutely-positioned overlay.
/// Useful for modals, popups, and other overlay elements.
/// </summary>
public class Modal(params VNode[] children) : Component
{
    protected override VNode Render()
        => FlexPanel(
            position: Position.Absolute,
            top: Node.MeasurementMarginPosition.Point(0), left: Node.MeasurementMarginPosition.Point(0), right: Node.MeasurementMarginPosition.Point(0), bottom: Node.MeasurementMarginPosition.Point(0),
            flexDirection: FlexDirection.Column,
            justifyContent: Justify.Center,
            alignItems: Align.Center,
            children: children
        );
}