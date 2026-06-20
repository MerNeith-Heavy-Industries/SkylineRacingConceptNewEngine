using WorldXaml.UI.Base;
using WorldXaml.UI.Yoga;

namespace NFMWorldLibrary.DriverInterface.UI.Elements;

/// <summary>
/// An absolute element whose children are centered both horizontally and vertically. This is useful for modals, popups,
/// and other overlay elements.
/// </summary>
public class Modal : FlexPanel
{
    public Modal()
    {
        Position = YgPositionType.Absolute;
        Top = 0;
        Left = 0;
        Right = 0;
        Bottom = 0;

        // Center content horizontally and vertically
        FlexDirection = YgFlexDirection.Column;
        JustifyContent = YgJustify.Center;
        AlignItems = YgAlign.Center;
    }
}