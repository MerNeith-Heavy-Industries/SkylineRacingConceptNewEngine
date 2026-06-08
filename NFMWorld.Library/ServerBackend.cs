using NFMWorld.DriverInterface;
using NFMWorldLibrary.Backend.Gamemodes;

namespace NFMWorldLibrary;

public class ServerBackend : IBackend
{
    public float Scale { get; set; } = 1f;
    public Vector2 Viewport { get; } = Vector2.Zero;

    public IRadicalMusic LoadMusic(string file, double tempomul) => ClientServer.AccidentallyCalledClientMethodOnServer<IRadicalMusic>();

    public IImage LoadImage(string file) => ClientServer.AccidentallyCalledClientMethodOnServer<IImage>();

    public IImage LoadCachedImage(string file) => ClientServer.AccidentallyCalledClientMethodOnServer<IImage>();

    public IImage LoadImage(ReadOnlySpan<byte> file) => ClientServer.AccidentallyCalledClientMethodOnServer<IImage>();

    public void StopAllSounds() => ClientServer.AccidentallyCalledClientMethodOnServer();

    public ISoundClip GetSound(string filePath) => ClientServer.AccidentallyCalledClientMethodOnServer<ISoundClip>();

    public IGraphics Graphics => ClientServer.AccidentallyCalledClientMethodOnServer<IGraphics>();
    
    public void SetAllVolumes(float vol) => ClientServer.AccidentallyCalledClientMethodOnServer();
}