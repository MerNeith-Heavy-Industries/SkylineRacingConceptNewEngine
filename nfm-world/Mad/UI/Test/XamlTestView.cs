using WorldXaml.UI.Yoga;

namespace NFMWorld.UI.Test;

public partial class XamlTestView : View
{
    public new XamlTestViewModel DataContext => (XamlTestViewModel)base.DataContext!;

    public XamlTestView()
    {
        base.DataContext = new XamlTestViewModel();
        InitializeComponent();
    }
}
