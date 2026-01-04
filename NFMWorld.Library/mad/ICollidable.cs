using nfm_world_library.mad.rad;

namespace nfm_world_library.mad;

public interface ICollidable : ITransform
{
    public Rad3dBoxDef[] Boxes { get; }
    public int MaxRadius { get; }
}