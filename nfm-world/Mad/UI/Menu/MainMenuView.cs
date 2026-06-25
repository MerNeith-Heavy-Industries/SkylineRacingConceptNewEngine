using System.Collections.ObjectModel;
using NFMWorld.Reactor;
using WorldXaml.UI.Yoga;
using static WorldXaml.UI.Yoga.Nodes;

namespace NFMWorld.UI.Menu;

public class MainMenuView : Component
{
    private readonly MainMenuViewModel _vm;

    // Navigation events — hooked by MainMenuPhase
    public event Action? Garage { add => _vm.Garage += value; remove => _vm.Garage -= value; }
    public event Action? Settings { add => _vm.Settings += value; remove => _vm.Settings -= value; }
    public event Action? Credits { add => _vm.Credits += value; remove => _vm.Credits -= value; }
    public event Action? Quit { add => _vm.Quit += value; remove => _vm.Quit -= value; }
    public event Action? Login { add => _vm.Login += value; remove => _vm.Login -= value; }
    public event Action? Logout { add => _vm.Logout += value; remove => _vm.Logout -= value; }
    public event Action? PlayNFM1 { add => _vm.PlayNFM1 += value; remove => _vm.PlayNFM1 -= value; }
    public event Action? PlayNFM2 { add => _vm.PlayNFM2 += value; remove => _vm.PlayNFM2 -= value; }
    public event Action? PlayCommunity { add => _vm.PlayCommunity += value; remove => _vm.PlayCommunity -= value; }
    public event Action? PlayFreePlay { add => _vm.PlayFreePlay += value; remove => _vm.PlayFreePlay -= value; }
    public event Action? PlayCompetitive { add => _vm.PlayCompetitive += value; remove => _vm.PlayCompetitive -= value; }
    public event Action? PlayCasual { add => _vm.PlayCasual += value; remove => _vm.PlayCasual -= value; }
    public event Action? ModelEditor { add => _vm.ModelEditor += value; remove => _vm.ModelEditor -= value; }
    public event Action? StageEditor { add => _vm.StageEditor += value; remove => _vm.StageEditor -= value; }
    public event Action? CampaignEditor { add => _vm.CampaignEditor += value; remove => _vm.CampaignEditor -= value; }
    public event Action? TimeTrials { add => _vm.TimeTrials += value; remove => _vm.TimeTrials -= value; }
    public event Action? Challenges { add => _vm.Challenges += value; remove => _vm.Challenges -= value; }
    public event Action? GameInstructions { add => _vm.GameInstructions += value; remove => _vm.GameInstructions -= value; }

    public MainMenuView()
    {
        _vm = new MainMenuViewModel();
        DisableMemo();
    }

    protected override VNode Render()
    {
        var vm = UseObservable(_vm);
        var items = UseCollection(vm.Items);

        return View(
            name: "MainMenu",
            flexDirection: YgFlexDirection.Column,
            alignItems: YgAlign.Stretch,
            justifyContent: YgJustify.FlexStart,
            gap: 10f,
            padding: 20f,
            children: [
                // Title
                FlexPanel(name: vm.Title, flexDirection: YgFlexDirection.Row, minHeight: 60f,
                    alignItems: YgAlign.Center, justifyContent: YgJustify.SpaceBetween, children: [
                        FlexPanel(name: vm.LoginButtonText,
                            flexDirection: YgFlexDirection.Row, justifyContent: YgJustify.FlexEnd, minWidth: 200f,
                            children:
                                FlexPanel(name: vm.LoginButtonText)
                        )
                    ]
                ),
                // Menu items
                FlexPanel(flexDirection: YgFlexDirection.Column, alignItems: YgAlign.FlexStart, gap: 12f,
                    children: RenderItems(items)
                ),
                // Spacer
                FlexPanel(flex: 1f),
                // Description
                FlexPanel(name: vm.Description),
                // Account status
                FlexPanel(name: vm.AccountStatus)
            ]
        );
    }

    private VNode[] RenderItems(ObservableCollection<MainMenuItemViewModel> items)
    {
        var nodes = new VNode[items.Count];
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            nodes[i] = FlexPanel(
                name: item.Text,
                flexDirection: YgFlexDirection.Row,
                alignItems: YgAlign.Center,
                minWidth: 230f,
                minHeight: 35f,
                padding: 12f,
                children:
                    FlexPanel(name: item.Description)
            );
        }
        return nodes;
    }
}
