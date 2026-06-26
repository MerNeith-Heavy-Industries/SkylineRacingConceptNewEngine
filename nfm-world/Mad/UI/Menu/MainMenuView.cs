using System.Collections.Immutable;
using System.Collections.ObjectModel;
using NFMWorld.Accounts;
using NFMWorld.DriverInterface;
using NFMWorld.DriverInterface.UI;
using NFMWorld.Reactor;
using NFMWorldLibrary.DriverInterface.UI.Elements;
using WorldXaml.UI.Yoga;
using static WorldXaml.UI.Yoga.Nodes;
using static NFMWorld.DriverInterface.UI.Nodes;
using Node = WorldXaml.UI.Yoga.Node;

namespace NFMWorld.UI.Menu;

public class MainMenuView(
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
    public record MainMenuPage(string Title, ImmutableArray<MainMenuItem> Items);

    public record MainMenuItem(string Text, string Description, Action? OnClick, bool Hovered = false);

    protected override VNode Render()
    {
        var (activePage, setActivePage) = UseState<MainMenuPage?>(null);
        var (account, setAccount) = UseState<Account?>(null);
        
        void PushPage(Func<MainMenuPage> pageBuilder)
        {
            setActivePage(pageBuilder());
        }
        
        void GoBack()
        {
            // TODO
        }

        MainMenuPage BuildMainMenu()
        {
            return new MainMenuPage("NFM WORLD?", [
                new MainMenuItem("PLAY", "Play public, private matches online or play singleplayer.", () => PushPage(BuildPlayMenu)),
                new MainMenuItem("GARAGE", "Customize and inspect your vehicles in the garage.", () => Garage?.Invoke()),
                new MainMenuItem("WORKSHOP", "Build your own models and stages.", () => PushPage(BuildWorkshopMenu)),
                new MainMenuItem("SETTINGS", "Adjust game settings.", () => Settings?.Invoke()),
                new MainMenuItem("CREDITS", "View game credits.", () => Credits?.Invoke()),
                new MainMenuItem("QUIT", "Exit the game.", () => Quit?.Invoke()),
            ]);
        }

        MainMenuPage BuildPlayMenu()
        {
            return new MainMenuPage("PLAY", [
                new MainMenuItem("SINGLEPLAYER", "Play the original single player experiences.", () => PushPage(BuildSPMenu)),
                new MainMenuItem("MULTIPLAYER", "Play online with other players.", () => PushPage(BuildMPMenu)),
                new MainMenuItem("TRAINING", "Train your skills and learn the game mechanics.", () => PushPage(BuildTrainingMenu)),
                new MainMenuItem("BACK", "Return to the previous menu.", GoBack)
            ]);
        }
        
        MainMenuPage BuildSPMenu()
        {
            return new MainMenuPage("SINGLEPLAYER", [
                new MainMenuItem("NFM1", "Play the original NFM1 singleplayer campaign.", () => PlayNFM1?.Invoke()),
                new MainMenuItem("NFM2", "Play the original NFM2 singleplayer campaign.", () => PlayNFM2?.Invoke()),
                new MainMenuItem("COMMUNITY", "Play custom experiences crafted by the community.", () => PlayCommunity?.Invoke()),
                new MainMenuItem("FREE PLAY", "Play freely without any restrictions.", () => PlayFreePlay?.Invoke()),
                new MainMenuItem("BACK", "Return to the previous menu.", GoBack)
            ]);
        }
        
        MainMenuPage BuildMPMenu()
        {
            return new MainMenuPage("MULTIPLAYER", [
                new MainMenuItem("COMPETITIVE", "Compete against other players via matchmaking.", () => PlayCompetitive?.Invoke()),
                new MainMenuItem("CASUAL", "Play with people in a free relaxed environment.", () => PlayCasual?.Invoke()),
                new MainMenuItem("BACK", "Return to the previous menu.", GoBack)
            ]);
        }
        
        MainMenuPage BuildWorkshopMenu()
        {
            return new MainMenuPage("WORKSHOP", [
                new MainMenuItem("MODEL EDITOR", "View and edit custom models.", () => ModelEditor?.Invoke()),
                new MainMenuItem("STAGE EDITOR", "Design your own stages.", () => StageEditor?.Invoke()),
                new MainMenuItem("CAMPAIGN EDITOR", "Craft custom experiences.", () => CampaignEditor?.Invoke()),
                new MainMenuItem("BACK", "Return to the previous menu.", GoBack)
            ]);
        }
        
        MainMenuPage BuildTrainingMenu()
        {
            return new MainMenuPage("TRAINING", [
                new MainMenuItem("TIME TRIALS", "Flex your fastest time against other people.", () => TimeTrials?.Invoke()),
                new MainMenuItem("CHALLENGES", "Complete challenges to sharpen your mechanical skills.", () => Challenges?.Invoke()),
                new MainMenuItem("GAME INSTRUCTIONS", "Read about the rules and controls of the game.", () => GameInstructions?.Invoke()),
                new MainMenuItem("BACK", "Return to the previous menu.", GoBack)
            ]);
        }

        setActivePage(BuildMainMenu());

        void SetHover(int itemIndex, bool hoverState)
        {
            setActivePage(activePage with
            {
                Items = activePage.Items.SetItem(itemIndex, activePage.Items[itemIndex] with { Hovered = hoverState })
            });
        }

        bool IsHovered(int itemIndex)
        {
            return activePage.Items[itemIndex].Hovered;
        }

        MainMenuItem? HoveredItem()
        {
            return activePage.Items.FirstOrDefault(item => item.Hovered);
        }
        
        // Top row: title + login button
        return View(children:
        [
            FlexPanel(
                name: "TopRow",
                flexDirection: YgFlexDirection.Row,
                justifyContent: YgJustify.SpaceBetween,
                alignItems: YgAlign.FlexEnd,
                minHeight: 200,
                marginBottom: 30,
                children:
                [
                    // Title
                    TextRun(
                        name: "TitleText",
                        fontStyle: FontStyle.Bold,
                        fontSize: 48,
                        fontFamily: FontFamily.Adventure,
                        foreground: new Color(255, 140, 0),
                        stroke: Color.Black,
                        text: activePage?.Title ?? "NFM WORLD?"
                    ),

                    FlexPanel(
                        name: "LoginButton",
                        flexDirection: YgFlexDirection.Row,
                        justifyContent: YgJustify.FlexEnd,
                        alignItems: YgAlign.FlexEnd,
                        flex: 1,
                        minWidth: 250,
                        minHeight: 35,
                        padding: 12,
                        children:
                        [
                            // Login / Logout button
                            TextRun(
                                name: "LoginButtonText",
                                fontStyle: FontStyle.Bold,
                                fontSize: 24,
                                fontFamily: FontFamily.Adventure,
                                foreground: account is null ? new Color(255, 140, 0) : new Color(180, 180, 180),
                                stroke: Color.Black,
                                text: account is null ? "Login" : "Logout",
                                mousePressed: account is null ? _ => Login?.Invoke() : _ => Logout?.Invoke()
                            )
                        ]
                    )
                ]
            ),
            // Menu items
            ..activePage?.Items.Select((item, idx) => SolidBox(
                key: idx,
                name: "ItemRow",
                flexDirection: YgFlexDirection.Row,
                alignItems: YgAlign.Center,
                minWidth: 230,
                minHeight: 35,
                padding: Node.MeasurementMultiPadding.XY(12, 8),
                mouseEntered: _ => SetHover(idx, true),
                mouseLeft: _ => SetHover(idx, false),
                isFocusable: true,
                mousePressed: _ => item.OnClick?.Invoke(),
                gap: 0,
                
                backgroundColor: IsHovered(idx) ? new Color(20, 15, 35) : Color.Transparent,
                borderColor: IsHovered(idx) ? new Color(255, 140, 0) : Color.Transparent,
                borderTop: 1,
                borderLeft: 1,
                borderRight: 1,
                borderBottom: 1,
                borderTopLeftRadius: 5,
                borderTopRightRadius: 5,
                borderBottomRightRadius: 5,
                borderBottomLeftRadius: 5,

                children:
                [
                    // button text
                    TextRun(
                        name: "ButtonText",
                        fontStyle: FontStyle.Bold,
                        fontSize: 24,
                        fontFamily: FontFamily.Adventure,
                        foreground: new Color(255, 140, 0),
                        stroke: Color.Black,
                        text: item.Text
                    )
                ])
            ) ?? [],

            // Spacer to push description to bottom
            Node(flex: 1),

            // Description / tooltip
            TextRun(
                name: "DescriptionText",
                fontSize: 14,
                fontFamily: FontFamily.Adventure,
                foreground: new Color(255, 140, 0),
                text: HoveredItem()?.Description ?? ""
            ),

            // Account status row
            FlexPanel(
                name: "AccountRow",
                flexDirection: YgFlexDirection.Row,
                justifyContent: YgJustify.SpaceBetween,
                alignItems: YgAlign.Center,
                marginTop: 10,
                children:
                [
                    TextRun(
                        name: "AccountStatusText",
                        fontSize: 14,
                        fontFamily: FontFamily.Adventure,
                        foreground: new Color(180, 180, 180),
                        text: account is null ? "Not logged in" : $"Logged in as {account.Username}"
                    )
                ]
            )
        ]);
    }
}
