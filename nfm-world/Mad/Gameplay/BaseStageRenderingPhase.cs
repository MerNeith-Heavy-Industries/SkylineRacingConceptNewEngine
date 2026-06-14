using Microsoft.Xna.Framework.Graphics;
using NFMWorld.DriverInterface;
using NFMWorld.Util;
using NFMWorldLibrary;
using NFMWorldLibrary.Backend;
using NFMWorldLibrary.Util;
using WorldXaml.UI.Yoga.Events;

namespace NFMWorld.Gameplay;

public abstract class BaseStageRenderingPhase(GraphicsDevice graphicsDevice) : BasePhase
{
    protected int? FovOverride = null;
    public static bool DebugDisplay = false;

    private readonly SpriteBatch _spriteBatch = new(graphicsDevice);

    public readonly GraphicsDevice GraphicsDevice = graphicsDevice;

    public PerspectiveCamera Camera = new();
    public Camera[] LightCameras = [
        new OrthoLightCamera
        {
            Width = 3000,
            Height = 3000
        },
        new OrthoLightCamera
        {
            Width = 16384,
            Height = 16384
        },
        new OrthoLightCamera
        {
            Width = 65536,
            Height = 65536
        }
    ];

    public ClientStage CurrentStage = null!;
    public Scene CurrentScene => CurrentStage.CurrentScene;

    public UnlimitedArray<IInGameCar> CarsInRace { get; protected set; } = [];

    public override void Enter()
    {
        base.Enter();
        
        Camera.Width = GameSparker.Game.GraphicsDevice.Viewport.Width;
        Camera.Height = GameSparker.Game.GraphicsDevice.Viewport.Height;
    }

    public override void Exit()
    {
        base.Exit();
        GameSparker.CurrentMusic?.Unload();
    }

    public virtual void LoadStageMusic(bool reloadIfLoaded = false)
    {
        if ((reloadIfLoaded && GameSparker.CurrentMusic != null) || GameSparker.CurrentMusic == null)
        {
            if(reloadIfLoaded && GameSparker.CurrentMusic != null)
            {
                GameSparker.CurrentMusic?.Unload();
            }

            Logging.Debug("playing stage music: " + CurrentStage.MusicPath);

            bool useRemastered = GameSparker.UseRemasteredMusic && !string.IsNullOrEmpty(CurrentStage.RemasteredMusicPath);
            // Dont shift pitch or tempo if using remastered
            string path = useRemastered ? CurrentStage.RemasteredMusicPath : CurrentStage.MusicPath;
            double tempoMul = !useRemastered ? CurrentStage.MusicTempoMul : 0d;
            double freqMul = !useRemastered ? CurrentStage.MusicFreqMul : 1d;

            GameSparker.CurrentMusic = IBackend.Backend.LoadMusic($"./data/music/{path}", tempoMul);
            GameSparker.CurrentMusic.SetFreqMultiplier(freqMul);
            GameSparker.CurrentMusic.SetVolume(IRadicalMusic.CurrentVolume);
            GameSparker.CurrentMusic.Play();
        }
    }
    
    public virtual void SetStage(ClientStage stage, bool loadMusic = true)
    {
        CurrentStage = stage;
        CurrentStage.BackendCars = CarsInRace;
        CurrentStage.Camera = Camera;
        CurrentStage.LightCameras = LightCameras;
        
        RecreateScene();

        if (loadMusic && (!string.IsNullOrEmpty(CurrentStage.MusicPath) || (GameSparker.UseRemasteredMusic && !string.IsNullOrEmpty(CurrentStage.RemasteredMusicPath))))
        {
            LoadStageMusic(true);
        }
    }

    public virtual void LoadStage(string stageName, bool loadMusic = true)
    {
        CurrentStage = new ClientStage(GraphicsDevice, stageName, CarsInRace, Camera, LightCameras);

        RecreateScene();

        if (loadMusic && (!string.IsNullOrEmpty(CurrentStage.MusicPath) || (GameSparker.UseRemasteredMusic && !string.IsNullOrEmpty(CurrentStage.RemasteredMusicPath))))
        {
            LoadStageMusic(true);
        }
    }

    public virtual void RecreateScene()
    {
        CurrentStage.RecreateScene();
    }
    
    public ClientCar GetClientCar(int index)
    {
        return CurrentStage.GetClientCar(CarsInRace[index]);
    }

    public override void KeyPressed(Key key, bool imguiWantsKeyboard, in Keys keys)
    {
        base.KeyPressed(key, imguiWantsKeyboard, keys);

        if (imguiWantsKeyboard) return;
    }

    public override void KeyReleased(Key key, bool imguiWantsKeyboard, in Keys keys)
    {
        base.KeyReleased(key, imguiWantsKeyboard, keys);
    }

    public override void WindowSizeChanged(int width, int height)
    {
        base.WindowSizeChanged(width, height);

        G.Scale = 1280f / width;

        Camera.Width = width;
        Camera.Height = height;
    }

    public override void BeginGameTick()
    {
        base.BeginGameTick();
        CurrentScene.OnBeforeUpdate();
    }

    public override void GameTick()
    {
        base.GameTick();
        CurrentScene.GameTick(CurrentStage);
    }

    public override void Render(float alpha)
    {
        base.Render(alpha);
        
        foreach (var lightCamera in LightCameras)
        {
            lightCamera.Position = Camera.Position + new Vector3(0, -5000, 0);
            lightCamera.LookAt = Camera.Position + new Vector3(1f, 0, 0); // 0,0,0 causes shadows to break
        }

        Camera.Fov = FovOverride ?? Camera.Fov;

        CurrentScene.Render(alpha, true);

        if (DebugDisplay)
        {
            // DISPLAY SHADOW MAP
            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullCounterClockwise);
            if (WorldGame.ShadowRenderTargets[0] != null) _spriteBatch.Draw(WorldGame.ShadowRenderTargets[0], new Microsoft.Xna.Framework.Rectangle(0, 0, 128, 128), Color.White);
            if (WorldGame.ShadowRenderTargets[1] != null) _spriteBatch.Draw(WorldGame.ShadowRenderTargets[1], new Microsoft.Xna.Framework.Rectangle(0, 128, 128, 128), Color.White);
            if (WorldGame.ShadowRenderTargets[2] != null) _spriteBatch.Draw(WorldGame.ShadowRenderTargets[2], new Microsoft.Xna.Framework.Rectangle(0, 256, 128, 128), Color.White);
            _spriteBatch.End();
        }

        GraphicsDevice.Textures[0] = null;
        GraphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;
    }
}