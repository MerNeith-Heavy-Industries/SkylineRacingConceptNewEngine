using System.Collections;
using SoftFloat;

namespace NFMWorld.Mad;

public interface IStage
{
    ReadOnlySpan<CollisionBoxRef> RetrievePointCollidables(fix64 x, fix64 z);
}