using System.ComponentModel;
using System.Reactive.Linq;
using NFMWorld.DriverInterface.UI;
using WorldXaml.UI.Base;
using WorldXaml.UI.Yoga;
using WorldXaml.UI.Yoga.Events;

namespace NFMWorldLibrary.DriverInterface.UI.Elements;

public partial class Button : FlexPanel
{
    private readonly SolidBox _buttonBox;
    private readonly bool _isHovered;

    public Button()
    {
        IsFocusable = true;
        _buttonBox = new SolidBox();
        _isHovered = false;
    }
    
    [Property(DefaultValueMember = nameof(DefaultBorderColor), OnChangedMethod = nameof(OnBorderColorChanged))]
    public partial Color BorderColor { get; set; }
    private static partial Color DefaultBorderColor => new Color(255, 140, 0);
    
    [Property(DefaultValueMember = nameof(DefaultBackgroundColor), OnChangedMethod = nameof(OnBackgroundColorChanged))]
    public partial Color BackgroundColor { get; set; }
    private static partial Color DefaultBackgroundColor => Color.Transparent;
    
    [Property(DefaultValueMember = nameof(DefaultBackgroundHoverColor), OnChangedMethod = nameof(OnContentHoverColorChanged))]
    public partial Color BackgroundHoverColor { get; set; }
    private static partial Color DefaultBackgroundHoverColor => new Color(20, 15, 35);

    [Property(DefaultValue = 5, OnChangedMethod = nameof(OnBorderTopLeftRadiusChanged))]
    public partial int BorderTopLeftRadius { get; set; }
    [Property(DefaultValue = 5, OnChangedMethod = nameof(OnBorderTopRightRadiusChanged))]
    public partial int BorderTopRightRadius { get; set; }
    [Property(DefaultValue = 5, OnChangedMethod = nameof(OnBorderBottomLeftRadiusChanged))]
    public partial int BorderBottomLeftRadius { get; set; }
    [Property(DefaultValue = 5, OnChangedMethod = nameof(OnBorderBottomRightRadiusChanged))]
    public partial int BorderBottomRightRadius { get; set; }

    private partial void OnBorderColorChanged(Color color) => UpdateState();
    private partial void OnBackgroundColorChanged(Color color) => UpdateState();
    private partial void OnContentHoverColorChanged(Color color) => UpdateState();
    private partial void OnBorderTopLeftRadiusChanged(int radius)  => UpdateState();
    private partial void OnBorderTopRightRadiusChanged(int radius) => UpdateState();
    private partial void OnBorderBottomLeftRadiusChanged(int radius) => UpdateState();
    private partial void OnBorderBottomRightRadiusChanged(int radius) => UpdateState();

    private void UpdateState()
    {
        _buttonBox.BorderColor = BorderColor;
        _buttonBox.BackgroundColor = _isHovered ? BackgroundHoverColor : BackgroundColor;
        _buttonBox.Border = _isHovered ? 3 : 1;
        _buttonBox.BorderTopLeftRadius = BorderTopLeftRadius;
        _buttonBox.BorderTopRightRadius = BorderTopRightRadius;
        _buttonBox.BorderBottomLeftRadius = BorderBottomLeftRadius;
        _buttonBox.BorderBottomRightRadius = BorderBottomRightRadius;
    }
    
    protected override void OnMouseEntered(FocusManager focusManager, MouseMoveEvent @event)
    {
        base.OnMouseEntered(focusManager, @event);
    }
}
