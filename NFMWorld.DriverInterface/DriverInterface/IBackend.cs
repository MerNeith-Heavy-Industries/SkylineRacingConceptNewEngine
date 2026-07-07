using System.Numerics;
using Microsoft.Xna.Framework;
using NFMWorldLibrary.Backend.Gamemodes;
using WorldXaml.UI.Yoga;
using WorldXaml.UI.Yoga.Events;

namespace NFMWorld.DriverInterface;

public interface IBackend
{
    public static IBackend Backend
    {
        get
        {
            return field ?? ThrowNotInitialized();

            IBackend ThrowNotInitialized()
            {
                throw new InvalidOperationException(
                    $"{nameof(IBackend)}.{nameof(Backend)} needs to be set before it can be used.");
            }
        }
        set;
    }

    [ClientOnly]
    IRadicalMusic LoadMusic(string file, double tempomul);
    
    [ClientOnly]
    void StopAllSounds();
    
    [ClientOnly]
    ISoundClip GetSound(string filePath);
    
    [ClientOnly]
    IGraphics Graphics { get; }
    
    [ClientOnly]
    void SetAllVolumes(float vol);

    [ClientOnly]
    Key GetKeyFromScancode(Key key);
}