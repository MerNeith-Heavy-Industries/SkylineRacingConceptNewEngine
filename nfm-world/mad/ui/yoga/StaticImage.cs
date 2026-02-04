using nfm_world.driverinterface;

namespace nfm_world.ui.yoga;

public class StaticImage(string path)
{
    public string Path { get; set; } = path;

    public IImage ProvideValue(IServiceProvider serviceProvider)
    {
        return IBackend.Backend.LoadCachedImage(Path);
    }
}