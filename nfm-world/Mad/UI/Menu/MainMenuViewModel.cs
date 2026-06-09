using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using NFMWorld.Account;
using NFMWorld.DriverInterface;
using NFMWorldLibrary;

namespace NFMWorld.UI.Menu;

public partial class MainMenuViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string Title { get; set; } = "NFM WORLD?";

    [ObservableProperty]
    public partial string Description { get; set; } = "";

    [ObservableProperty]
    public partial string AccountStatus { get; set; } = "Logged Out";

    [ObservableProperty]
    public partial string LoginButtonText { get; set; } = "LOGIN";

    [ObservableProperty]
    public partial Color LoginButtonColor { get; set; } = new(255, 140, 0, 255);

    [ObservableProperty]
    public partial Color TitleColor { get; set; } = new(255, 140, 0, 255);

    [ObservableProperty]
    public partial Color ButtonTextColor { get; set; } = new(255, 140, 0, 255);

    [ObservableProperty]
    public partial Color ButtonHoverBgColor { get; set; } = new(255, 140, 0, 255);

    [ObservableProperty]
    public partial Color BackgroundColor { get; set; } = new(15, 0, 35, 255);

    public ObservableCollection<MainMenuItemViewModel> Items { get; } = new();

    private readonly Stack<Action> _menuHistory = new();

    public MainMenuViewModel()
    {
        BuildMainMenu();
        RefreshLoginState();
    }

    public void BuildMainMenu()
    {
        Items.Clear();
        Title = "NEED FOR MADNESS?";
        AddItem("PLAY", "Play public, private matches online or play singleplayer.", BuildPlayMenu);
        AddItem("GARAGE", "Customize and inspect your vehicles in the garage.", null);
        AddItem("WORKSHOP", "Build your own models and stages.", BuildWorkshopMenu);
        AddItem("SETTINGS", "Adjust game settings.", OnSettingsClicked);
        AddItem("CREDITS", "View game credits.", OnClickUnavailable);
        AddItem("QUIT", "Exit the game.", OnQuitClicked);
    }

    private void OnModelEditorClicked()
    {
        GameSparker.StartModelViewer();
    }

    private void OnStageEditorClicked()
    {
        GameSparker.StartStageEditor();
    }

    private void OnSettingsClicked()
    {
        GameSparker.SettingsMenu.Open();
    }

    private void OnClickUnavailable()
    {
        GameSparker.MessageWindow.ShowMessage("Info", "This feature is currently unavailable.");
    }

    private void OnQuitClicked()
    {
        GameSparker.MessageWindow.ShowYesNo("Quit", "Are you sure you want to quit?",
            result =>
            {
                if (result == MessageWindow.MessageResult.Yes)
                {
                    System.Environment.Exit(0);
                }
            });
    }

    private void BuildPlayMenu()
    {
        _menuHistory.Push(BuildMainMenu);
        Items.Clear();
        Title = "PLAY";
        AddItem("SINGLEPLAYER", "Play the original single player experiences.", BuildSPMenu);
        AddItem("MULTIPLAYER", "Play online with other players.", BuildMPMenu);
        AddItem("TRAINING", "Train your skills and learn the game mechanics.", BuildTrainingMenu);
        AddItem("BACK", "Return to the previous menu.", GoBack);
    }

    private void BuildSPMenu()
    {
        _menuHistory.Push(BuildPlayMenu);
        Items.Clear();
        Title = "SINGLEPLAYER";
        AddItem("NFM 1", "Play the original Need For Madness campaign.", PlayNFM1);
        AddItem("NFM 2", "Play the original Need For Madness 2 campaign.", PlayNFM2);
        AddItem("CUSTOM CAMPAIGN", "Play custom experiences crafted by the community.", PlayCommunity);
        AddItem("FREE PLAY", "The World is your oyster.", PlayFreePlay);
        AddItem("BACK", "Return to the previous menu.", GoBack);
    }

    private void BuildMPMenu()
    {
        _menuHistory.Push(BuildPlayMenu);
        Items.Clear();
        Title = "MULTIPLAYER";
        AddItem("COMPETITIVE", "Compete against other players via matchmaking.", PlayCompetitive);
        AddItem("CASUAL", "Play with people in a free relaxed environment.", PlayCasual);
        AddItem("BACK", "Return to the previous menu.", GoBack);
    }

    private void BuildWorkshopMenu()
    {
        _menuHistory.Push(BuildMainMenu);
        Items.Clear();
        Title = "WORKSHOP";
        AddItem("MODEL EDITOR", "View and edit custom models.", ModelEditor);
        AddItem("STAGE EDITOR", "Design your own stages.", StageEditor);
        AddItem("CAMPAIGN EDITOR", "Craft custom experiences.", CampaignEditor);
        AddItem("BACK", "Return to the previous menu.", GoBack);
    }

    private void BuildTrainingMenu()
    {
        _menuHistory.Push(BuildPlayMenu);
        Items.Clear();
        Title = "TRAINING";
        AddItem("TIME TRIALS", "Flex your fastest time against other people.", TimeTrials);
        AddItem("CHALLENGES", "Complete challenges to sharpen your mechanical skills.", Challenges);
        AddItem("GAME INSTRUCTIONS", "Read about the rules and controls of the game.", GameInstructions);
        AddItem("BACK", "Return to the previous menu.", GoBack);
    }

    public void GoBack()
    {
        if (_menuHistory.Count > 0)
            _menuHistory.Pop().Invoke();
    }

    private void AddItem(string text, string description, Action? onClick)
    {
        Items.Add(new MainMenuItemViewModel
        {
            Text = text,
            Description = description,
            OnClick = onClick
        });
    }
    
    private void OnLoginClicked()
    {
        // Show the account manager floating menu (handled by the phase via ImGui)
        Login?.Invoke();
    }

    private void OnLogoutClicked()
    {
        if (GameSparker.AccountManager.LoggedIn)
        {
            GameSparker.AccountManager.LogOut();
            RefreshLoginState();
        }
    }

    public event Action? Login;
    public event Action? Logout;
    public event Action? PlayNFM1;
    public event Action? PlayNFM2;
    public event Action? PlayCommunity;
    public event Action? PlayFreePlay;
    public event Action? PlayCompetitive;
    public event Action? PlayCasual;
    public event Action? ModelEditor;
    public event Action? StageEditor;
    public event Action? CampaignEditor;
    public event Action? TimeTrials;
    public event Action? Challenges;
    public event Action? GameInstructions;

    /// <summary>
    /// Call this from the phase to update the login UI after a login/logout event.
    /// </summary>
    public void RefreshLoginState()
    {
        var account = GameSparker.AccountManager.ActiveAccount;
        if (account is not null)
        {
            AccountStatus = $"Logged in as: {account.Username}";
            LoginButtonText = "LOGOUT";
        }
        else
        {
            AccountStatus = "Logged Out";
            LoginButtonText = "LOGIN";
        }
    }
}
