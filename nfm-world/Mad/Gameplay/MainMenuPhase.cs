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

public class MainMenuPhase : BaseStageRenderingPhase
{
    private MainMenuView _mainMenuView = new();
    private UIManager _uiManager;

    public MainMenuPhase(GraphicsDevice graphicsDevice) : base(graphicsDevice)
    {
        _uiManager = new UIManager
        {
            FocusManager = FocusManager
        };

        // MVU: Mount the main menu view directly via its own reconciler
        _mainMenuView.Mount(_uiManager.RootPanel);

        _mainMenuView.Garage += OnGarageClicked;
        _mainMenuView.Settings += OnSettingsClicked;
        _mainMenuView.Credits += OnClickUnavailable;
        _mainMenuView.Quit += OnQuitClicked;
        _mainMenuView.Login += OnLoginClicked;
        _mainMenuView.Logout += OnLogoutClicked;
        _mainMenuView.PlayNFM1 += OnClickUnavailable;
        _mainMenuView.PlayNFM2 += OnClickUnavailable;
        _mainMenuView.PlayCommunity += OnClickUnavailable;
        _mainMenuView.PlayFreePlay += OnFreePlayClicked;
        _mainMenuView.PlayCompetitive += OnClickUnavailable;
        _mainMenuView.PlayCasual += OnClickUnavailable;
        _mainMenuView.ModelEditor += OnModelEditorClicked;
        _mainMenuView.StageEditor += OnStageEditorClicked;
        _mainMenuView.CampaignEditor += OnClickUnavailable;
        _mainMenuView.TimeTrials += OnTTClicked;
        _mainMenuView.Challenges += OnClickUnavailable;
        _mainMenuView.GameInstructions += OnClickUnavailable;
    }

    private void OnFreePlayClicked()
    {
        var inRace = new InRacePhase(GraphicsDevice, "nfmm/radicalone");
        
        inRace.LoadStage("nfm2/15_dwm");
        GameSparker.SetPhase(inRace);

        Logging.Info("Game started!");
    }

    private void OnLoginClicked()
    {
        // accountManagerMenu ??= new AccountManagerModal();
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
        StageSelectPhase ssp = new(GraphicsDevice);
        ssp.StageSelected += (sender, stageName) =>
        {
            PhaseSharedState.SelectedStageName = stageName;
            
            GaragePhase gp = new(GraphicsDevice, stageName);
            gp.CarSelected += (sender, car) =>
            {
                var inRace = new InRacePhase(GraphicsDevice, car.FileName);
                inRace.gamemode = GameModes.TimeTrial;
                inRace.LoadStage(stageName);
                GameSparker.SetPhase(inRace);
            };

            gp.CarSelectionCancelled += (sender, _) =>
            {
                GameSparker.SetPhase(GameSparker.MainMenu);
            };

            GameSparker.SetPhase(gp);
        };

        GameSparker.SetPhase(ssp);
    }

    private void OnGarageClicked()
    {
        GaragePhase gp = new GaragePhase(GraphicsDevice);
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

    public override void GameTick()
    {
        base.GameTick();
        _mainMenuView.Update();
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

        // if (accountManagerMenu is not null)
        // {
        //     var res = accountManagerMenu.Process();
        //     if (res == AccountManagerModal.AccountManagerFloatingMenuState.LoggedIn)
        //     {
        //         accountManagerMenu.Close();
        //         accountManagerMenu = null;
        //     }
        //     else if (res == AccountManagerModal.AccountManagerFloatingMenuState.Canceled)
        //     {
        //         accountManagerMenu.Close();
        //         accountManagerMenu = null;
        //     }
        // }
    }
}