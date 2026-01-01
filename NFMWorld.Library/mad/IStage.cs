using System.Collections;
using NFMWorld.Util;
using SoftFloat;

namespace NFMWorld.Mad;

public interface IStage
{
    ReadOnlySpan<CollisionBoxRef> RetrievePointCollidables(fix64 x, fix64 z);
    IReadOnlyList<ITransform> pieces { get; }
    IReadOnlyList<IAiNode> nodes { get; }
    IReadOnlyList<IAiNode> checkpoints { get; }
    IReadOnlyList<IAiNode> fixHoops { get; }
    ushort nlaps { get; }
}