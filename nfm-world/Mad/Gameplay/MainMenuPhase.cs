﻿﻿using Microsoft.Xna.Framework.Graphics;
 using NFMWorld.Accounts;
using NFMWorld.UI;
using NFMWorld.UI.Cef;
using NFMWorldLibrary;
using NFMWorldLibrary.Backend.Gamemodes;
using NFMWorldLibrary.Multiplayer;
using NFMWorldLibrary.Util;
using WorldXaml.UI.Yoga.Events;

namespace NFMWorld.Gameplay;

// TODO: implement the same menu as in nfm-lit

public class MainMenuPhase : BaseStageRenderingPhase
{
    public override bool IsSingleton => true;

    private readonly MainMenuBridge _bridge = new();

    public MainMenuPhase(GraphicsDevice graphicsDevice) : base(graphicsDevice)
    {
        CefBridge = _bridge;

        _bridge.NavigateRequested += OnNavigateRequested;
        _bridge.LogoutRequested += OnLogoutClicked;

        // Push initial account state if available
        var account = GameSparker.AccountManager.LoggedIn
            ? GameSparker.AccountManager.ActiveAccount
            : null;
        _bridge.PushAccount(account?.Username, account != null);

        // Subscribe to account changes via the CLR event directly
        GameSparker.AccountManager.ActiveAccountChanged += OnActiveAccountChanged;
    }

    private void OnActiveAccountChanged(Account? account)
    {
        _bridge.PushAccount(account?.Username, account != null);
    }

    private void OnNavigateRequested(string page)
    {
        switch (page)
        {
            case "play":
            case "singleplayer":
                OnFreePlayClicked();
                break;
            case "multiplayer":
                OnClickUnavailable();
                break;
            case "training":
                OnClickUnavailable();
                break;
            case "garage":
                OnGarageClicked();
                break;
            case "settings":
                OnSettingsClicked();
                break;
            case "credits":
                OnClickUnavailable();
                break;
            case "quit":
                OnQuitClicked();
                break;
            case "modelEditor":
                OnModelEditorClicked();
                break;
            case "stageEditor":
                OnStageEditorClicked();
                break;
            case "timeTrials":
                OnTTClicked();
                break;
        }
    }

    private void OnFreePlayClicked()
    {
        var inRace = new InRacePhase(GraphicsDevice, "nfmm/radicalone");
        
        inRace.LoadStage("nfm2/15_dwm");
        GameSparker.SetPhase(inRace);

        Logging.Info("Game started!");
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
                GameSparker.SetPhase(this);
            };

            GameSparker.SetPhase(gp);
        };

        GameSparker.SetPhase(ssp);
    }

    private void OnGarageClicked()
    {
        GaragePhase gp = new GaragePhase(GraphicsDevice);
        
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