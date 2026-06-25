using System.Collections.Immutable;
using System.Collections.ObjectModel;
using NFMWorld.Reactor;
using WorldXaml.UI.Yoga;
using static WorldXaml.UI.Yoga.Nodes;

namespace NFMWorld.UI.Menu;

public class MainMenuView(
    string Title = "NFM WORLD?",
    Action? Garage = null,
    Action? Settings = null,
    Action? Credits = null,
    Action? Quit = null,
    Action? Login = null,
    Action? Logout = null,
    Action? PlayNFM1 = null,
    Action? PlayNFM2 = null,
    Action? PlayCommunity = null,
    Action? PlayFreePlay = null,
    Action? PlayCompetitive = null,
    Action? PlayCasual = null,
    Action? ModelEditor = null,
    Action? StageEditor = null,
    Action? CampaignEditor = null,
    Action? TimeTrials = null,
    Action? Challenges = null,
    Action? GameInstructions = null
) : Component
{
    public record MainMenuItem(string Text, string Description, Action? OnClick);
    
    protected override VNode Render()
    {
        var (menuHistory, setMenuHistory) = UseState(new ImmutableArray<Action>());
        var (activeItem, setActiveItem) = UseState<MainMenuItem?>(null);
        
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
