using nfm_world_library.SoftFloat;

namespace nfm_world_library.mad;

public interface ITransform
{
    IReadOnlyList<ITransform> ChildTransforms { get; }
    f64Vector3 Position { get; set; }
    f64Euler Rotation { get; set; }
    ITransform? Parent { get; }
}