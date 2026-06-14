using Microsoft.Xna.Framework.Graphics;
using NFMWorldLibrary;

namespace NFMWorld;

public class StageScene : Scene
{
    public ClientStageRenderer StageRenderer;
    public ClientCarCollection ClientCars;
    
    public StageScene(
        GraphicsDevice graphicsDevice,
        ClientStageRenderer stageRenderer,
        IReadOnlyCollection<IInGameCar> cars,
        Camera camera,
        IReadOnlyList<Camera> lightCameras)
        : base(graphicsDevice, [stageRenderer, new ClientCarCollection(graphicsDevice, cars) is var carCollection ? carCollection : null!], camera, lightCameras)
    {
        StageRenderer = stageRenderer;
        ClientCars = carCollection;
    }
}