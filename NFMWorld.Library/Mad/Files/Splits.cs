using MemoryPack;

namespace NFMWorldLibrary.Files;

[MemoryPackable(GenerateType.VersionTolerant)]
public partial class Splits
{
    [MemoryPackOrder(0)] public List<long> SplitTimes = [];
}