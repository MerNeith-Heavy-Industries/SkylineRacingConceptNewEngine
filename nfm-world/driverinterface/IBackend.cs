using File = nfm_world_library.util.File;

namespace nfm_world.driverinterface;

public interface IBackend
{
    public static IBackend Backend { get; set; }

    float Scale { get; set; }
    Vector2 Viewport { get; }
    IRadicalMusic LoadMusic(File file, double tempomul);
    IImage LoadImage(File file);
    IImage LoadImage(ReadOnlySpan<byte> file);
    void StopAllSounds();
    ISoundClip GetSound(string filePath);
    IGraphics Graphics { get; }
    void SetAllVolumes(float vol);
}