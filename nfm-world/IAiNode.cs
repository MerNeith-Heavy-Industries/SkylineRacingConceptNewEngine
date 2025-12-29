using NFMWorld.Mad;

namespace NFMWorld;

public interface IAiNode : ITransform
{
    AiNodeKind Kind { get; }
}

public enum AiNodeKind
{
    CheckPoint,
    Road,
    Auto,
    Ramp,
    Halfpipe,
    SequenceStart,
    SequenceEnd,
    FixRoadStart,
    FixRamp,
    FixHoop,
    FixRoadEnd,
    Avoid,
    Reset
}