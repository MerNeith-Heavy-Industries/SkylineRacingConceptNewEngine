using Microsoft.Xna.Framework.Graphics;
using NFMWorld.Account;
using NFMWorld.DriverInterface;
using NFMWorld.Reactor;
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
    private MainMenuView _mainMenuView;
    private ComponentNode _mainMenuViewNode;
    private ReactorDom _dom;
    private UIManager _uiManager;

    public MainMenuPhase(GraphicsDevice graphicsDevice) : base(graphicsDevice)
    {
        _uiManager = new UIManager
        {
            FocusManager = FocusManager
        };

        _mainMenuView = new MainMenuView(
            Garage: OnGarageClicked,
            Settings: OnSettingsClicked,
            Credits: OnClickUnavailable,
            Quit: OnQuitClicked,
            Login: OnLoginClicked,
            Logout: OnLogoutClicked,
            PlayNFM1: OnClickUnavailable,
            PlayNFM2: OnClickUnavailable,
            PlayCommunity: OnClickUnavailable,
            PlayFreePlay: OnFreePlayClicked,
            PlayCompetitive: OnClickUnavailable,
            PlayCasual: OnClickUnavailable,
            ModelEditor: OnModelEditorClicked,
            StageEditor: OnStageEditorClicked,
            CampaignEditor: OnClickUnavailable,
            TimeTrials: OnTTClicked,
            Challenges: OnClickUnavailable,
            GameInstructions: OnClickUnavailable
        );

        _dom = new ReactorDom(SynchronizationContext.Current ?? new SynchronizationContext());
        _mainMenuViewNode = ComponentNodeFactory.Create(_mainMenuView);
        _dom.Mount(_uiManager.RootPanel, _mainMenuViewNode);
    }

    private void OnFreePlayClicked()
    {
        var inRace = new InRacePhase(GraphicsDevice, "nfmm/radicalone");
        
        inRace.LoadStage("user/dwm_test");
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
        _dom.Mount(_uiManager.RootPanel, _mainMenuViewNode);
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