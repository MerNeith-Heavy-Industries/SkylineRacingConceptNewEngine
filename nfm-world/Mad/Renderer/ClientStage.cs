using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework.Graphics;
using NFMWorldLibrary;
using NFMWorldLibrary.Backend;

namespace NFMWorld;

public class ClientStage : BackendStage
{
    private IReadOnlyCollection<IInGameCar> _backendCars;
    public IReadOnlyCollection<IInGameCar> BackendCars
    {
        get => _backendCars;
        set
        {
            _backendCars = value;
            RecreateScene();
        }
    }
    public readonly ClientStageRenderer Renderer;
    public StageScene CurrentScene;
    public readonly GraphicsDevice GraphicsDevice;
    public Camera Camera;
    public IReadOnlyList<Camera> LightCameras;

    // soundtrack(folder,fileName)
    public string MusicPath = "";
    // soundtrackremaster(folder,fileName)
    public string RemasteredMusicPath = "";
    // soundtrackfreqmul(mul)
    public double MusicFreqMul = 1.0d;
    public double MusicTempoMul = 0d;

    public ClientStage(
        GraphicsDevice graphicsDevice,
        string stageName,
        IReadOnlyCollection<IInGameCar> backendCars,
        Camera camera,
        IReadOnlyList<Camera> lightCameras
    ) : base(stageName)
    {
        _backendCars = backendCars;
        GraphicsDevice = graphicsDevice;
        Renderer = new ClientStageRenderer(graphicsDevice, this);
        Camera = camera;
        LightCameras = lightCameras;
        
        RecreateScene();
        
        MusicPath = stageLoader.musicPath;
        RemasteredMusicPath = stageLoader.remasteredMusicPath;
        MusicFreqMul = stageLoader.musicFreqMul;
        MusicTempoMul = stageLoader.musicTempoMul;

        if (string.IsNullOrEmpty(MusicPath))
        {
            Logging.Error("No music is defined for this stage!");
        }
    }

    [MemberNotNull(nameof(CurrentScene))]
    public void RecreateScene()
    {
        Renderer.ApplyValues();
        CurrentScene = new StageScene(
            GraphicsDevice,
            Renderer,
            BackendCars,
            Camera,
            LightCameras
        );
    }

    public ClientCar GetClientCar(IInGameCar car)
    {
        return CurrentScene.ClientCars.GetCar(car);
    }
}