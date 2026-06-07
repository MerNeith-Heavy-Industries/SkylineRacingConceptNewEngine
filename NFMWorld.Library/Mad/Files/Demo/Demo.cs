using MemoryPack;

namespace NFMWorldLibrary.Files.Demo;

[MemoryPackable(GenerateType.CircularReference)]
public partial class Demo
{
    [MemoryPackOrder(0)] public required List<DemoEntry> Ticks;

    public void AddEntry(DemoEntry entry)
    {
        Ticks.Add(entry);
    }

    public DemoEntry GetEntry(int tick)
    {
        return Ticks[tick];
    }
}