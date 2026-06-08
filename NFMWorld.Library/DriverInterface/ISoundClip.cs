using NFMWorldLibrary.Backend.Gamemodes;

namespace NFMWorld.DriverInterface;

[ClientOnly]
public interface ISoundClip
{
    void Play();
    void Loop();
    void Stop();
}