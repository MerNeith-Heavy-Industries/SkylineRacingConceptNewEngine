using System.Collections.ObjectModel;
using NFMWorld.Reactor;
using WorldXaml.UI.Yoga;
using static WorldXaml.UI.Yoga.Nodes;

namespace NFMWorld.DriverInterface.UI;

/// <summary>
/// A component that renders each item in a collection using a provided render function.
/// Automatically re-renders when the collection changes.
/// </summary>
public class ItemsRepeater<T>(T[] items, Func<T, VNode> renderItem) : Component
{
    protected override VNode Render()
    {
        return FlexPanel(
            flexDirection: FlexDirection.Column,
            children: items.Select(renderItem).ToArray()
        );
    }
}