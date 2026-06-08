using NFMWorld.DriverInterface;
using NFMWorldLibrary.Backend.Gamemodes;

namespace NFMWorld.UI;

public class StaticImage(string path)
{
    public string Path { get; set; } = path;

    [ClientOnly]
    public IImage ProvideValue(IServiceProvider serviceProvider)
    {
        return IBackend.Backend.LoadCachedImage(Path);
    }
}