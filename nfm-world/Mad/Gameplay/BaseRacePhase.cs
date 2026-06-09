using Microsoft.Xna.Framework.Graphics;
using NFMWorld.DriverInterface;
using NFMWorld.UI;
using NFMWorld.Util;
using NFMWorldLibrary;
using NFMWorldLibrary.Backend;
using NFMWorldLibrary.Backend.Gamemodes;
using NFMWorldLibrary.Util;
using WorldXaml.UI.Yoga;
using WorldXaml.UI.Yoga.Events;

namespace NFMWorld.Gameplay;

public abstract class BaseRacePhase(GraphicsDevice _graphicsDevice) : BaseStageRenderingPhase(_graphicsDevice), IGamemodeData, IClientCallbacks
{
    protected IGamemode? gamemodeInstance { get; set; }
    BackendStage IGamemodeData.CurrentStage => CurrentStage;

    public RaceState raceState
    {
        get;
        set
        {
            field = value;
            RaceStateChanged?.Invoke(this, value);
        }
    } = RaceState.InProgress;

    IClientCallbacks IGamemodeData.ClientCallbacks => this;

    public event EventHandler<RaceState>? RaceStateChanged;

    protected FollowCamera PlayerFollowCamera = new();
    protected AroundCamera PlayerAroundCamera = new();
    protected AroundStageCamera StageAroundCamera = new();
    
    // Track which keys are currently pressed to properly handle meta-bindings
    private HashSet<Key> _pressedKeys = new();
    
    // View modes
    public enum ViewMode
    {
        Follow,
        FollowStatic,
        Around,
        Watch
    }
    protected ViewMode currentViewMode = ViewMode.Follow;

    public override void Enter()
    {
        base.Enter();
        RecreateScene();
        ForceReloadGamemode();
    }

    internal void ForceReloadGamemode()
    {
        gamemodeInstance = ReloadGamemode();
        gamemodeInstance.Enter();
    }

    internal void OverrideGamemode(IGamemode gamemode)
    {
        gamemodeInstance = gamemode;
        gamemodeInstance.Enter();
    }

    protected abstract IGamemode ReloadGamemode();

    public override void Exit()
    {
        base.Exit();
        GameSparker.CurrentMusic?.Unload();
        gamemodeInstance?.Exit();
    }

    public override void KeyPressed(Key key, bool imguiWantsKeyboard, in Keys keys)
    {
        base.KeyPressed(key, imguiWantsKeyboard, keys);

        if (imguiWantsKeyboard) return;
        
        var bindings = SettingsMenu.Bindings;
        
        // Track pressed keys
        _pressedKeys.Add(key);
        
        // Update control state based on all currently pressed keys
        UpdateControlState();

        // Handle non-movement keys
        if (gamemodeInstance != null)
        {
            var control = CarsInRace.FirstOrDefault(c => c.Player.IsClientPlayer)?.Control;

            if (control != null)
            {
                if (key == bindings.Enter)
                {
                    control.Enter = true;
                }

                if (key == bindings.LookBack)
                {
                    control.Lookback = -1;
                }

                if (key == bindings.LookLeft)
                {
                    control.Lookback = 3;
                }

                if (key == bindings.LookRight)
                {
                    control.Lookback = 2;
                }

                if (key == bindings.ToggleMusic)
                {
                    control.Mutem = !control.Mutem;
                }

                if (key == bindings.ToggleSFX)
                {
                    control.Mutes = !control.Mutes;
                }

                if (key == bindings.ToggleArrace)
                {
                    control.Arrace = !control.Arrace;
                }

                if (key == bindings.ToggleRadar)
                {
                    control.Radar = !control.Radar;
                }

                if (key == bindings.CycleView)
                {
                    currentViewMode = (ViewMode)(((int)currentViewMode + 1) % Enum.GetValues<ViewMode>().Length);
                }
            }
        }

        gamemodeInstance?.KeyPressed(key, in keys);
    }
    
    private void UpdateControlState()
    {
        var bindings = SettingsMenu.Bindings;

        if (gamemodeInstance != null)
        {
            var control = CarsInRace.FirstOrDefault(c => c.Player.IsClientPlayer)?.Control;

            if (control != null)
            {
                // determine base key states
                bool acceleratePressed = _pressedKeys.Contains(bindings.Accelerate);
                bool brakePressed = _pressedKeys.Contains(bindings.Brake);
                bool turnLeftPressed = _pressedKeys.Contains(bindings.TurnLeft);
                bool turnRightPressed = _pressedKeys.Contains(bindings.TurnRight);
                bool aerialBouncePressed = _pressedKeys.Contains(bindings.AerialBounce);
                bool aerialStrafePressed = _pressedKeys.Contains(bindings.AerialStrafe);
                bool handbrakePressed = _pressedKeys.Contains(bindings.Handbrake);

                // apply Up/Down controls
                control.Up = acceleratePressed || aerialBouncePressed;
                control.Down = brakePressed || aerialBouncePressed;

                if (aerialStrafePressed)
                {
                    
                }

                control.Left = turnLeftPressed || aerialStrafePressed;
                control.Right = turnRightPressed || aerialStrafePressed;
                control.Handb = handbrakePressed;
            }
        }
    }

    public override void KeyReleased(Key key, bool imguiWantsKeyboard, in Keys keys)
    {
        base.KeyReleased(key, imguiWantsKeyboard, keys);

        var bindings = SettingsMenu.Bindings;
        
        // track released keys
        _pressedKeys.Remove(key);
        
        // update control state based on remaining pressed keys
        UpdateControlState();

        // handle special cases
        if (gamemodeInstance != null)
        {
            var control = CarsInRace.FirstOrDefault(c => c.Player.IsClientPlayer)?.Control;

            if (control != null)
            {
                if (key == Key.Escape)
                {
                    // this seems to be currently unused
                    control.Exit = false;
                }

                if (key == bindings.LookBack || key == bindings.LookLeft || key == bindings.LookRight)
                {
                    control.Lookback = 0;
                }
            }
        }

        gamemodeInstance?.KeyReleased(key, keys);
    }

    public override void MousePressed(int x, int y, bool imguiWantsMouse, MouseButton button, MouseButtons buttons, bool ctrlKey,
        bool shiftKey, bool altKey)
    {
        base.MousePressed(x, y, imguiWantsMouse, button, buttons, ctrlKey, shiftKey, altKey);
        
        gamemodeInstance?.MousePressed(x, y, button, buttons, ctrlKey, shiftKey, altKey);
    }

    public override void MouseReleased(int x, int y, bool imguiWantsMouse, MouseButton button, MouseButtons buttons, bool ctrlKey,
        bool shiftKey, bool altKey)
    {
        base.MouseReleased(x, y, imguiWantsMouse, button, buttons, ctrlKey, shiftKey, altKey);
        
        gamemodeInstance?.MouseReleased(x, y, button, buttons, ctrlKey, shiftKey, altKey);
    }

    public override void MouseScrolled(int x, int y, int delta, bool imguiWantsMouse, MouseButtons buttons,
        bool ctrlKey, bool shiftKey, bool altKey)
    {
        base.MouseScrolled(x, y, delta, imguiWantsMouse, buttons, ctrlKey, shiftKey, altKey);
        
        gamemodeInstance?.MouseScrolled(x, y, delta, buttons, ctrlKey, shiftKey, altKey);
    }

    public override void MouseMoved(int x, int y, bool imguiWantsMouse, MouseButtons buttons, bool ctrlKey, bool shiftKey, bool altKey)
    {
        base.MouseMoved(x, y, imguiWantsMouse, buttons, ctrlKey, shiftKey, altKey);
        
        gamemodeInstance?.MouseMoved(x, y, buttons, ctrlKey, shiftKey, altKey);
    }

    public override void WindowSizeChanged(int width, int height)
    {
        base.WindowSizeChanged(width, height);
        
        camera.Width = width;
        camera.Height = height;
    }

    public override void Render(float alpha)
    {
        base.Render(alpha);

        if (DebugDisplay)
        {
            RenderMessages();
            G.SetColor(new Color(0, 0, 0));
            G.DrawString($"Render: {WorldGame.LastFrameTime}ms", 100, 100);
            G.DrawString($"Tick: {WorldGame.LastTickTime}μs", 100, 120);
            G.DrawString($"Power: {CarsInRace[0]?.Mad?.Power:0.00}", 100, 140);
            G.DrawString($"Ticks executed last frame: {WorldGame.LastTickCount}", 100, 160);
        }
        
        gamemodeInstance?.Render();
    }

    private static void RenderMessages()
    {
        if (!FrameTrace.IsEnabled) return;
        
        var y = 0f;
        const float x = 250;
        const float increment = 20;
        
        G.SetColor(new Color(0, 0, 0));
        G.DrawString(FrameTrace.GetMessageString(), (int)x, (int)y);
    }

    public override void GameTick()
    {
        gamemodeInstance?.GameTick();

        if (gamemodeInstance != null)
        {
            var car = CarsInRace.FirstOrDefault(c => c.Player.IsClientPlayer);
            if (car != null)
            {
                switch (currentViewMode)
                {
                    case ViewMode.Follow:
                        PlayerFollowCamera.Follow(
                            camera, 
                            car,
                            (float)car.Mad.Cxz,
                            car.Control.Lookback,
                            (float)car.Mad.Speed,
                            car.Stats.Swits[2]
                        );
                        break;
                    case ViewMode.FollowStatic:
                        PlayerFollowCamera.Follow(
                            camera,
                            car,
                            (float)car.Mad.StaticCameraXz,
                            car.Control.Lookback,
                            (float)car.Mad.Speed,
                            car.Stats.Swits[2]
                        );
                        break;
                    case ViewMode.Around:
                        PlayerAroundCamera.Around(camera, car);
                        break;
                }
            }
        }

        base.GameTick();
    }

    void IClientCallbacks.ResetCheckpointGlow()
    {
        clientStageRenderer.ResetCheckpointGlow();
    }

    void IClientCallbacks.UpdateCheckpointGlow(ushort currentCheckpoint, bool isFinish)
    {
        clientStageRenderer.UpdateCheckpointGlow(currentCheckpoint, isFinish);
    }

    IClientCarCallbacks IClientCallbacks.GetClientCarCallbacks(int index)
    {
        return GetClientCar(index);
    }
    
}