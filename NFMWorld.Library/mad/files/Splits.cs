using MessagePack;

namespace nfm_world.files;

[MessagePackObject]
public class Splits
{
    [Key(0)] public List<long> SplitTimes = [];
}