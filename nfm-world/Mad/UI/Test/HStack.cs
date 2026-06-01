using WorldXaml.UI.Base;
using WorldXaml.UI.Yoga;

namespace NFMWorld.UI.Test;

public partial class HStack : TemplatedControl
{
    public HStack()
    {
        InitializeComponent();
    }

    [Property(OnChangedMethod = nameof(OnOrientationChanged))]
    public partial StackOrientation Orientation { get; set; }
    
    [Property(DefaultValue = YgFlexDirection.Row)]
    public partial YgFlexDirection BoxFlexDirection { get; set; }

    [Property]
    public partial float GapColumn { get; set; }

    [Property]
    public partial float GapRow { get; set; }

    private partial void OnOrientationChanged(StackOrientation prop)
    {
        BoxFlexDirection = prop switch
        {
            StackOrientation.Horizontal => YgFlexDirection.Row,
            StackOrientation.Vertical => YgFlexDirection.Column,
            _ => throw new ArgumentOutOfRangeException(nameof(prop), prop, null)
        };
    }
}

public enum StackOrientation
{
    Horizontal,
    Vertical
}
