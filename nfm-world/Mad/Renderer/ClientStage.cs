using Microsoft.Xna.Framework.Graphics;
using NFMWorldLibrary;
using NFMWorldLibrary.Backend;

namespace NFMWorld;

/// <summary>
/// Client-side representation of a stage. Composes a <see cref="BackendStage"/> for collision/AI data
/// and adds rendering (stage geometry, cars, scene management). Owns camera and light setup.
/// Fully self-contained — constructed once per phase; no manual RecreateScene needed.
/// </summary>
public class ClientStage
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly Dictionary<IInGameCar, CarVisual> _carVisuals = new();
    private IReadOnlyCollection<IInGameCar> _cars;
    private Scene _scene;

    public BackendStage Backend { get; }
    public ClientStageRenderer Renderer { get; }
    public Camera Camera { get; set; }
    public IReadOnlyList<Camera> LightCameras { get; set; }

    // ── Music metadata from stage loader ──
    public string MusicPath { get; }
    public string RemasteredMusicPath { get; }
    public double MusicFreqMul { get; }
    public double MusicTempoMul { get; }

    public ClientStage(
        GraphicsDevice graphicsDevice,
        string stageName,
        IReadOnlyCollection<IInGameCar> cars,
        Camera camera,
        IReadOnlyList<Camera> lightCameras)
    {
        _graphicsDevice = graphicsDevice;
        _cars = cars;
        Camera = camera;
        LightCameras = lightCameras;

        Backend = new BackendStage(stageName);
        Renderer = new ClientStageRenderer(graphicsDevice, Backend);
        Renderer.ApplyValues();

        // Build initial car visuals
        foreach (var car in cars)
            _carVisuals[car] = new CarVisual(graphicsDevice, car);

        // Build scene object list: renderer + all car visuals
        var objects = new List<GameObject> { Renderer };
        objects.AddRange(_carVisuals.Values);
        _scene = new Scene(graphicsDevice, objects, camera, lightCameras);

        // ── Music metadata ──
        MusicPath = Backend.stageLoader.musicPath;
        RemasteredMusicPath = Backend.stageLoader.remasteredMusicPath;
        MusicFreqMul = Backend.stageLoader.musicFreqMul;
        MusicTempoMul = Backend.stageLoader.musicTempoMul;

        if (string.IsNullOrEmpty(MusicPath))
            Logging.Error("No music is defined for this stage!");
    }

    /// <summary>
    /// Update the set of backend cars this stage tracks.
    /// Cleans up visuals for removed cars and creates visuals for new ones.
    /// </summary>
    public void SetCars(IReadOnlyCollection<IInGameCar> cars)
    {
        _cars = cars;

        // Remove visuals for cars no longer in the set
        var removed = _carVisuals.Keys.Except(cars).ToArray();
        foreach (var key in removed)
        {
            if (_carVisuals.Remove(key, out var visual))
                visual.Dispose();
        }

        // Ensure all current cars have visuals
        foreach (var car in cars)
        {
            if (!_carVisuals.ContainsKey(car))
                _carVisuals[car] = new CarVisual(_graphicsDevice, car);
        }

        RebuildScene();
    }

    /// <summary>
    /// Gets or creates the <see cref="CarVisual"/> for a backend car.
    /// </summary>
    public CarVisual GetCarVisual(IInGameCar car)
    {
        if (!_carVisuals.TryGetValue(car, out var visual))
        {
            visual = _carVisuals[car] = new CarVisual(_graphicsDevice, car);
            RebuildScene();
        }
        return visual;
    }

    /// <summary>
    /// Gets the <see cref="CarVisual"/> for a backend car by index.
    /// </summary>
    public CarVisual GetCarVisual(int index)
    {
        return GetCarVisual(_cars.ElementAt(index));
    }

    /// <summary>
    /// The backend cars currently tracked by this stage.
    /// </summary>
    public IReadOnlyCollection<IInGameCar> Cars => _cars;

    // ── Scene lifecycle ──

    private void RebuildScene()
    {
        var objects = new List<GameObject> { Renderer };
        objects.AddRange(_carVisuals.Values);
        _scene = new Scene(_graphicsDevice, objects, Camera, LightCameras);
    }

    public void OnBeforeUpdate()
    {
        Camera.OnBeforeRender(0);
        foreach (var lightCamera in LightCameras)
            lightCamera.OnBeforeRender(0);
        foreach (var obj in _scene.Objects)
            obj.OnBeforeRender(0);
    }

    public void GameTick()
    {
        foreach (var obj in _scene.Objects)
            obj.GameTick(Backend);
    }

    public void Render(float alpha, bool useShadowMapping = true, bool clearRenderBuffer = true)
    {
        _scene.ActiveCamera = Camera;
        _scene.Render(alpha, useShadowMapping, clearRenderBuffer);
    }
}