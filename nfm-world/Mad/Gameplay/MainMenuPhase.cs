using Microsoft.Xna.Framework.Graphics;
using NFMWorld.Account;
using NFMWorld.DriverInterface;
using NFMWorld.UI;
using NFMWorld.UI.Menu;
using NFMWorld.Util;
using NFMWorldLibrary;
using NFMWorldLibrary.Backend.Gamemodes;
using NFMWorldLibrary.Multiplayer;
using WorldXaml.UI.Yoga;
using WorldXaml.UI.Yoga.Events;

namespace NFMWorld.Gameplay;

// TODO: implement the same menu as in nfm-lit

public class MainMenuPhase : BasePhase
{
    private GraphicsDevice _graphicsDevice;

    private AccountManagerFloatingMenu? accountManagerMenu;
    
    private MainMenuView _mainMenuView = new();
    private UIManager _uiManager;

    public MainMenuPhase(GraphicsDevice graphicsDevice)
    {
        _uiManager = new UIManager
        {
            FocusManager = FocusManager
        };

        _graphicsDevice = graphicsDevice;

        _uiManager.RootPanel.Children.Add(_mainMenuView);
        Uis.Add(_uiManager);

        _mainMenuView.DataContext.Garage += OnGarageClicked;
        _mainMenuView.DataContext.Settings += OnSettingsClicked;
        _mainMenuView.DataContext.Credits += OnClickUnavailable;
        _mainMenuView.DataContext.Quit += OnQuitClicked;
        _mainMenuView.DataContext.Login += OnLoginClicked;
        _mainMenuView.DataContext.Logout += OnLogoutClicked;
        _mainMenuView.DataContext.PlayNFM1 += OnClickUnavailable;
        _mainMenuView.DataContext.PlayNFM2 += OnClickUnavailable;
        _mainMenuView.DataContext.PlayCommunity += OnClickUnavailable;
        _mainMenuView.DataContext.PlayFreePlay += OnFreePlayClicked;
        _mainMenuView.DataContext.PlayCompetitive += OnClickUnavailable;
        _mainMenuView.DataContext.PlayCasual += OnClickUnavailable;
        _mainMenuView.DataContext.ModelEditor += OnModelEditorClicked;
        _mainMenuView.DataContext.StageEditor += OnStageEditorClicked;
        _mainMenuView.DataContext.CampaignEditor += OnClickUnavailable;
        _mainMenuView.DataContext.TimeTrials += OnTTClicked;
        _mainMenuView.DataContext.Challenges += OnClickUnavailable;
        _mainMenuView.DataContext.GameInstructions += OnClickUnavailable;
    }

    private void OnFreePlayClicked()
    {
        if (GameSparker.InRace != null)
        {
	        GameSparker.InRace.LoadStage("nfm2/15_dwm");
            GameSparker.SetPhase(GameSparker.InRace);

            Logging.Info("Game started!");
        }
    }

    private void OnLoginClicked()
    {
        accountManagerMenu ??= new AccountManagerFloatingMenu();
    }

    private void OnLogoutClicked()
    {
        if (GameSparker.AccountManager.LoggedIn)
        {
            GameSparker.AccountManager.LogOut();
        }
    }

    private void OnTTClicked()
    {
        GameSparker.InRace?.gamemode = GameModes.TimeTrial;

        StageSelectPhase ssp = new(_graphicsDevice);
        ssp.StageSelected += (sender, stage) =>
        {
            GameSparker.InRace.CurrentStage = stage;
            GameSparker.InRace.clientStageRenderer = new ClientStageRenderer(_graphicsDevice, stage);
            GameSparker.InRace.RecreateScene();
            GameSparker.InRace.LoadStageMusic(true);
            GameSparker.SetPhase(this);

            GaragePhase gp = new(_graphicsDevice);
            gp.CarSelected += (sender, car) =>
            {
                GameSparker.InRace.playerCarName = car.FileName;
                GameSparker.SetPhase(GameSparker.InRace);
            };

            gp.CarSelectionCancelled += (sender, _) =>
            {
                GameSparker.SetPhase(GameSparker.MainMenu);
            };

            gp.StageOverride = GameSparker.InRace.CurrentStage;
            gp.ClientStageRendererOverride = GameSparker.InRace.clientStageRenderer;
            GameSparker.SetPhase(gp);
        };

        GameSparker.SetPhase(ssp);
    }

    private void OnGarageClicked()
    {
        GaragePhase gp = new GaragePhase(_graphicsDevice);
        gp.Enter();
        gp.CarSelected += (sender, c) =>
        {
            GameSparker.SetPhase(this);
        };

        gp.CarSelectionCancelled += (sender, _) =>
        {
            GameSparker.SetPhase(this);
        };

        GameSparker.SetPhase(gp);
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

    public override void KeyPressed(Key key, bool imguiWantsKeyboard, in Keys keys)
    {
        base.KeyPressed(key, imguiWantsKeyboard, keys);

        if (imguiWantsKeyboard) return;

        // Handle key capture for settings menu
        if (GameSparker.SettingsMenu.IsOpen && GameSparker.SettingsMenu.IsCapturingKey())
        {
            GameSparker.SettingsMenu.HandleKeyCapture(key);
        }
        return;
    }

    public override void RenderImgui()
    {
        base.RenderImgui();

        if (accountManagerMenu is not null)
        {
            var res = accountManagerMenu.Process();
            if (res == AccountManagerFloatingMenu.AccountManagerFloatingMenuState.LoggedIn)
            {
                accountManagerMenu.Close();
                accountManagerMenu = null;
            }
            else if (res == AccountManagerFloatingMenu.AccountManagerFloatingMenuState.Canceled)
            {
                accountManagerMenu.Close();
                accountManagerMenu = null;
            }
            ;
        }
    }
}
