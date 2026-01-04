using SoftFloat;

namespace NFMWorld.Mad;

public interface ITransform
{
    IReadOnlyList<ITransform> ChildTransforms { get; }
    f64Vector3 Position { get; set; }
    f64Euler Rotation { get; set; }
    ITransform? Parent { get; }
}