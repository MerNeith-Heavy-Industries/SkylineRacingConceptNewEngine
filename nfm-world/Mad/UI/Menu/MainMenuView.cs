using System.Collections.Immutable;
using System.Collections.ObjectModel;
using NFMWorld.Accounts;
using NFMWorld.DriverInterface;
using NFMWorld.DriverInterface.UI;
using NFMWorld.Reactor;
using NFMWorldLibrary.DriverInterface.UI.Elements;
using WorldXaml.UI.Yoga;

namespace NFMWorld.UI.Menu;

public class MainMenuView(
    Action? garage = null,
    Action? settings = null,
    Action? credits = null,
    Action? quit = null,
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
    Action? gameInstructions = null,
    AccountManager? accountManager = null
) : Component
{
    public record MainMenuPage(string Title, ImmutableArray<MainMenuItem> Items);

    public record MainMenuItem(string Text, string Description, Action? OnClick, bool Hovered = false);

    protected override VNode Render()
    {
        var (activePage, setActivePage) = UseState<MainMenuPage?>(null);
        var pageHistory = UseRef(new Stack<MainMenuPage>());

        var pushPage = UseCallback<Func<MainMenuPage>>(pageBuilder =>
        {
            setActivePage(prev =>
            {
                if (prev is not null)
                    pageHistory.Current.Push(prev);
                return pageBuilder();
            });
        }, []);
        
        var goBack = UseCallback(() =>
        {
            setActivePage(prev =>
                pageHistory.Current.TryPop(out var previousPage) ? previousPage : prev);
        }, []);

        var buildSpMenu = UseCallback(() =>
        {
            return new MainMenuPage("SINGLEPLAYER", [
                new MainMenuItem("NFM1", "Play the original NFM1 singleplayer campaign.", () => playNfm1?.Invoke()),
                new MainMenuItem("NFM2", "Play the original NFM2 singleplayer campaign.", () => playNfm2?.Invoke()),
                new MainMenuItem("COMMUNITY", "Play custom experiences crafted by the community.", () => playCommunity?.Invoke()),
                new MainMenuItem("FREE PLAY", "Play freely without any restrictions.", () => playFreePlay?.Invoke()),
                new MainMenuItem("BACK", "Return to the previous menu.", goBack)
            ]);
        }, [goBack, playNfm1, playNfm2, playCommunity, playFreePlay]);
        
        var buildMpMenu = UseCallback(() =>
        {
            return new MainMenuPage("MULTIPLAYER", [
                new MainMenuItem("COMPETITIVE", "Compete against other players via matchmaking.", () => playCompetitive?.Invoke()),
                new MainMenuItem("CASUAL", "Play with people in a free relaxed environment.", () => playCasual?.Invoke()),
                new MainMenuItem("BACK", "Return to the previous menu.", goBack)
            ]);
        }, [goBack, playCompetitive, playCasual]);

        var buildWorkshopMenu = UseCallback(() =>
        {
            return new MainMenuPage("WORKSHOP", [
                new MainMenuItem("MODEL EDITOR", "View and edit custom models.", () => modelEditor?.Invoke()),
                new MainMenuItem("STAGE EDITOR", "Design your own stages.", () => stageEditor?.Invoke()),
                new MainMenuItem("CAMPAIGN EDITOR", "Craft custom experiences.", () => campaignEditor?.Invoke()),
                new MainMenuItem("BACK", "Return to the previous menu.", goBack)
            ]);
        }, [goBack, modelEditor, stageEditor, campaignEditor]);
        
        var buildTrainingMenu = UseCallback(() =>
        {
            return new MainMenuPage("TRAINING", [
                new MainMenuItem("TIME TRIALS", "Flex your fastest time against other people.", () => timeTrials?.Invoke()),
                new MainMenuItem("CHALLENGES", "Complete challenges to sharpen your mechanical skills.", () => challenges?.Invoke()),
                new MainMenuItem("GAME INSTRUCTIONS", "Read about the rules and controls of the game.", () => gameInstructions?.Invoke()),
                new MainMenuItem("BACK", "Return to the previous menu.", goBack)
            ]);
        }, [goBack, timeTrials, challenges, gameInstructions]);
        
        var buildPlayMenu = UseCallback(() =>
        {
            return new MainMenuPage("PLAY", [
                new MainMenuItem("SINGLEPLAYER", "Play the original single player experiences.", () => pushPage(buildSpMenu)),
                new MainMenuItem("MULTIPLAYER", "Play online with other players.", () => pushPage(buildMpMenu)),
                new MainMenuItem("TRAINING", "Train your skills and learn the game mechanics.", () => pushPage(buildTrainingMenu)),
                new MainMenuItem("BACK", "Return to the previous menu.", goBack)
            ]);
        }, [goBack, buildSpMenu, buildMpMenu, buildTrainingMenu]);

        var buildMainMenu = UseCallback(() =>
        {
            return new MainMenuPage("NFM WORLD?", [
                new MainMenuItem("PLAY", "Play public, private matches online or play singleplayer.", () => pushPage(buildPlayMenu)),
                new MainMenuItem("GARAGE", "Customize and inspect your vehicles in the garage.", () => garage?.Invoke()),
                new MainMenuItem("WORKSHOP", "Build your own models and stages.", () => pushPage(buildWorkshopMenu)),
                new MainMenuItem("SETTINGS", "Adjust game settings.", () => settings?.Invoke()),
                new MainMenuItem("CREDITS", "View game credits.", () => credits?.Invoke()),
                new MainMenuItem("QUIT", "Exit the game.", () => quit?.Invoke()),
            ]);
        }, [garage, settings, credits, quit, buildPlayMenu, buildWorkshopMenu]);

        UseEffect(() =>
        {
            setActivePage(_ => buildMainMenu());
        });

        var setHover = UseCallback<int, bool>((itemIndex, hoverState) =>
        {
            if (activePage is null) return;
            setActivePage(page => page with
            {
                Items = [..page.Items.Select((item, idx) => idx == itemIndex ? item with { Hovered = hoverState } : item with { Hovered = false })]
            });
        }, [activePage]);

        bool IsHovered(int itemIndex)
        {
            return activePage.Items[itemIndex].Hovered;
        }

        MainMenuItem? HoveredItem()
        {
            return activePage?.Items.FirstOrDefault(item => item.Hovered);
        }

        var (loginModalOpen, setLoginModalOpen) = UseState(false);
        var account = UseObservable(accountManager?.ActiveAccountObservable);
        
        var closeLoginModal = UseCallback(() => setLoginModalOpen(_ => false));

        // Top row: title + login button
        return View(
            flexDirection: FlexDirection.Column,
            margin: 15,
            width: MeasurementWidthHeight.Percent(100),
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
                            style: Theme.Styles.Title,
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
                                    foreground: account is null ? Theme.Colors.Primary : Theme.Colors.Unimportant,
                                    stroke: Color.Black,
                                    text: account is null ? "Login" : "Logout",
                                    mousePressed: account is null ? _ => setLoginModalOpen(_ => true) : _ => logout?.Invoke()
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
                        ..activePage?.Items.Select((item, idx) => PaintedBox(
                            key: $"{activePage.Title}::{idx}",
                            name: "ItemRow",
                            style: Theme.Styles.BigButton,

                            mouseEntered: _ => setHover(idx, true),
                            mouseLeft: _ => setHover(idx, false),
                            isFocusable: true,
                            mousePressed: _ => item.OnClick?.Invoke(),

                            children:
                            [
                                // button text
                                TextRun(
                                    name: "ButtonText",
                                    style: Theme.Styles.BigButtonText,
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
                    foreground: Theme.Colors.Primary,
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
                            foreground: Theme.Colors.Unimportant,
                            text: account is null ? "Not logged in" : $"Logged in as {account.Username}"
                        )
                    ]
                ),
                
                LoginModal(
                    isVisible: loginModalOpen,
                    onClose: closeLoginModal
                )
            ]
        );
    }
}
