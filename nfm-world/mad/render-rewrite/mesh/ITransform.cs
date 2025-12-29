using SoftFloat;

namespace NFMWorld.Mad;

public interface ITransform
{
    IReadOnlyList<ITransform> Children { get; }
    f64Vector3 Position { get; set; }
    f64Euler Rotation { get; set; }
    ITransform? Parent { get; }
    Matrix MatrixWorld { get; }
}