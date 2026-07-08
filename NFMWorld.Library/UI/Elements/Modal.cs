using NFMWorld.Reactor;
using WorldXaml.UI.Yoga;
using static NFMWorld.Reactor.Nodes;
using Node = NFMWorld.Reactor.Node;

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
            top: MeasurementMarginPosition.Point(0), left: MeasurementMarginPosition.Point(0), right: MeasurementMarginPosition.Point(0), bottom: MeasurementMarginPosition.Point(0),
            flexDirection: FlexDirection.Column,
            justifyContent: Justify.Center,
            alignItems: Align.Center,
            children: children
        );
}