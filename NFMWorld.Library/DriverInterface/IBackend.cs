using Microsoft.Xna.Framework;
using NFMWorldLibrary.Backend.Gamemodes;
using WorldXaml.UI.Yoga;
using WorldXaml.UI.Yoga.Events;

namespace NFMWorld.DriverInterface;

public interface IBackend : IXamlGraphicsBackend
{
    public new static IBackend Backend
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
        set
        {
            field = value;
            IXamlGraphicsBackend.Backend = value;
        }
    }

#pragma warning disable NFMW0001
    float IXamlGraphicsBackend.Scale => Scale;
    
    Vector2 IXamlGraphicsBackend.Viewport => Viewport;
    
    IXamlGraphics IXamlGraphicsBackend.Graphics => Graphics;
#pragma warning restore NFMW0001

    [ClientOnly]
    new float Scale { get; set; }
    
    [ClientOnly]
    new Vector2 Viewport { get; }
    
    [ClientOnly]
    IRadicalMusic LoadMusic(string file, double tempomul);
    
    [ClientOnly]
    IImage LoadImage(string file);
    
    [ClientOnly]
    IImage LoadCachedImage(string file);
    
    [ClientOnly]
    IImage LoadImage(ReadOnlySpan<byte> file);
    
    [ClientOnly]
    void StopAllSounds();
    
    [ClientOnly]
    ISoundClip GetSound(string filePath);
    
    [ClientOnly]
    new IGraphics Graphics { get; }
    
    [ClientOnly]
    void SetAllVolumes(float vol);

    [ClientOnly]
    Key GetKeyFromScancode(Key key);
}