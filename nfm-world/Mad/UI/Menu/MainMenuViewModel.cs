using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NFMWorld.Accounts;
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
    public partial Color BackgroundColor { get; set; } = new(15, 0, 35, 255);

    public ObservableCollection<MainMenuItemViewModel> Items { get; } = new();

    [RelayCommand]
    public void DoLogin()
    {
        Login?.Invoke();
    }

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
        AddItem("GARAGE", "Customize and inspect your vehicles in the garage.", () => Garage?.Invoke());
        AddItem("WORKSHOP", "Build your own models and stages.", BuildWorkshopMenu);
        AddItem("SETTINGS", "Adjust game settings.", () => Settings?.Invoke());
        AddItem("CREDITS", "View game credits.", () => Credits?.Invoke());
        AddItem("QUIT", "Exit the game.", () => Quit?.Invoke());
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
        AddItem("NFM 1", "Play the original Need For Madness campaign.", () => PlayNFM1?.Invoke());
        AddItem("NFM 2", "Play the original Need For Madness 2 campaign.", () => PlayNFM2?.Invoke());
        AddItem("CUSTOM CAMPAIGN", "Play custom experiences crafted by the community.", () => PlayCommunity?.Invoke());
        AddItem("FREE PLAY", "The World is your oyster.", () => PlayFreePlay?.Invoke());
        AddItem("BACK", "Return to the previous menu.", GoBack);
    }

    private void BuildMPMenu()
    {
        _menuHistory.Push(BuildPlayMenu);
        Items.Clear();
        Title = "MULTIPLAYER";
        AddItem("COMPETITIVE", "Compete against other players via matchmaking.", () => PlayCompetitive?.Invoke());
        AddItem("CASUAL", "Play with people in a free relaxed environment.", () => PlayCasual?.Invoke());
        AddItem("BACK", "Return to the previous menu.", GoBack);
    }

    private void BuildWorkshopMenu()
    {
        _menuHistory.Push(BuildMainMenu);
        Items.Clear();
        Title = "WORKSHOP";
        AddItem("MODEL EDITOR", "View and edit custom models.", () => ModelEditor?.Invoke());
        AddItem("STAGE EDITOR", "Design your own stages.", () => StageEditor?.Invoke());
        AddItem("CAMPAIGN EDITOR", "Craft custom experiences.", () => CampaignEditor?.Invoke());
        AddItem("BACK", "Return to the previous menu.", GoBack);
    }

    private void BuildTrainingMenu()
    {
        _menuHistory.Push(BuildPlayMenu);
        Items.Clear();
        Title = "TRAINING";
        AddItem("TIME TRIALS", "Flex your fastest time against other people.", () => TimeTrials?.Invoke());
        AddItem("CHALLENGES", "Complete challenges to sharpen your mechanical skills.", () => Challenges?.Invoke());
        AddItem("GAME INSTRUCTIONS", "Read about the rules and controls of the game.", () => GameInstructions?.Invoke());
        AddItem("BACK", "Return to the previous menu.", GoBack);
    }

    public void GoBack()
    {
        if (_menuHistory.Count > 0)
            _menuHistory.Pop().Invoke();
    }

    private void AddItem(string text, string description, Action? onClick)
    {
        var vm = new MainMenuItemViewModel
        {
            Text = text,
            Description = description
        };
        vm.OnClick += onClick;
        Items.Add(vm);
    }
    
    public event Action? Garage;
    public event Action? Settings;
    public event Action? Credits;
    public event Action? Quit;
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
