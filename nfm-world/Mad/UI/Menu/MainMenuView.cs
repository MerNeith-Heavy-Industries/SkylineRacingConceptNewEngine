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
    Action? garage = null,
    Action? settings = null,
    Action? credits = null,
    Action? quit = null,
    Action? login = null,
    Action? logout = null,
    Action? playNfm1 = null,
    Action? playNfm2 = null,
    Action? playCommunity = null,
    Action? playFreePlay = null,
    Action? playCompetitive = null,
    Action? playCasual = null,
    Action? modelEditor = null,
    Action? stageEditor = null,
    Action? campaignEditor = null,
    Action? timeTrials = null,
    Action? challenges = null,
    Action? gameInstructions = null
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
                new MainMenuItem("GARAGE", "Customize and inspect your vehicles in the garage.", () => garage?.Invoke()),
                new MainMenuItem("WORKSHOP", "Build your own models and stages.", () => PushPage(BuildWorkshopMenu)),
                new MainMenuItem("SETTINGS", "Adjust game settings.", () => settings?.Invoke()),
                new MainMenuItem("CREDITS", "View game credits.", () => credits?.Invoke()),
                new MainMenuItem("QUIT", "Exit the game.", () => quit?.Invoke()),
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
                new MainMenuItem("NFM1", "Play the original NFM1 singleplayer campaign.", () => playNfm1?.Invoke()),
                new MainMenuItem("NFM2", "Play the original NFM2 singleplayer campaign.", () => playNfm2?.Invoke()),
                new MainMenuItem("COMMUNITY", "Play custom experiences crafted by the community.", () => playCommunity?.Invoke()),
                new MainMenuItem("FREE PLAY", "Play freely without any restrictions.", () => playFreePlay?.Invoke()),
                new MainMenuItem("BACK", "Return to the previous menu.", GoBack)
            ]);
        }
        
        MainMenuPage BuildMPMenu()
        {
            return new MainMenuPage("MULTIPLAYER", [
                new MainMenuItem("COMPETITIVE", "Compete against other players via matchmaking.", () => playCompetitive?.Invoke()),
                new MainMenuItem("CASUAL", "Play with people in a free relaxed environment.", () => playCasual?.Invoke()),
                new MainMenuItem("BACK", "Return to the previous menu.", GoBack)
            ]);
        }
        
        MainMenuPage BuildWorkshopMenu()
        {
            return new MainMenuPage("WORKSHOP", [
                new MainMenuItem("MODEL EDITOR", "View and edit custom models.", () => modelEditor?.Invoke()),
                new MainMenuItem("STAGE EDITOR", "Design your own stages.", () => stageEditor?.Invoke()),
                new MainMenuItem("CAMPAIGN EDITOR", "Craft custom experiences.", () => campaignEditor?.Invoke()),
                new MainMenuItem("BACK", "Return to the previous menu.", GoBack)
            ]);
        }
        
        MainMenuPage BuildTrainingMenu()
        {
            return new MainMenuPage("TRAINING", [
                new MainMenuItem("TIME TRIALS", "Flex your fastest time against other people.", () => timeTrials?.Invoke()),
                new MainMenuItem("CHALLENGES", "Complete challenges to sharpen your mechanical skills.", () => challenges?.Invoke()),
                new MainMenuItem("GAME INSTRUCTIONS", "Read about the rules and controls of the game.", () => gameInstructions?.Invoke()),
                new MainMenuItem("BACK", "Return to the previous menu.", GoBack)
            ]);
        }
        
        UseEffect(() =>
        {
            setActivePage(BuildMainMenu());
        });

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
            return activePage?.Items.FirstOrDefault(item => item.Hovered);
        }
        
        // Top row: title + login button
        return View(
            flexDirection: FlexDirection.Column,
            margin: 15,
            children:
            [
                FlexPanel(
                    name: "TopRow",
                    flexDirection: FlexDirection.Row,
                    justifyContent: Justify.SpaceBetween,
                    alignItems: Align.FlexEnd,
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
                            flexDirection: FlexDirection.Row,
                            justifyContent: Justify.FlexEnd,
                            alignItems: Align.FlexEnd,
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
                                    mousePressed: account is null ? _ => login?.Invoke() : _ => logout?.Invoke()
                                )
                            ]
                        )
                    ]
                ),
                // Menu items
                FlexPanel(
                    name: "MenuItems",
                    flexDirection: FlexDirection.Column,
                    alignItems: Align.FlexStart,
                    gap: 15,
                    children: [
                        ..activePage?.Items.Select((item, idx) => SolidBox(
                            key: idx,
                            name: "ItemRow",
                            flexDirection: FlexDirection.Row,
                            alignItems: Align.Center,
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
                        ) ?? []
                    ]
                ),

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
                    flexDirection: FlexDirection.Row,
                    justifyContent: Justify.SpaceBetween,
                    alignItems: Align.Center,
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
            ]
        );
    }
}
