using MemoryPack;

namespace NFMWorldLibrary.Files.Demo;

[MemoryPackable(GenerateType.VersionTolerant)]
public partial class Demo
{
    [MemoryPackOrder(0)] public List<DemoEntry> Ticks = [];

    public void AddEntry(DemoEntry entry)
    {
        Ticks.Add(entry);
    }

    public DemoEntry GetEntry(int tick)
    {
        return Ticks[tick];
    }
}