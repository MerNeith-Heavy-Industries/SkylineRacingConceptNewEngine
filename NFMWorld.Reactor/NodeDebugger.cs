namespace NFMWorld.Reactor;

public static class NodeDebugger
{
    public readonly struct DebugInfo(
        string ctorCallerFilePath,
        int ctorCallerLineNumber,
        string ctorCallerMemberName
    )
    {
        public readonly string CtorCallerFilePath = ctorCallerFilePath;
        public readonly string CtorCallerMemberName = ctorCallerMemberName;
        public readonly int CtorCallerLineNumber = ctorCallerLineNumber;
    }

    public static IReadOnlyList<Node> YogaRootsThisFrame => Node.__INTERNAL_YogaRootsThisFrame;

#if DEBUG
    internal static readonly List<ReactorDebugNode> _VDomRootsThisFrame = [];
#endif

    /// <summary>
    /// The root nodes of the Reactor VDOM tree captured during this frame's
    /// reconciliation pass. Each node carries its VNode type, associated
    /// native <see cref="Visual"/> (for layout lookup), and — for Component
    /// nodes — constructor inputs.
    /// </summary>
    /// <remarks>Only populated in DEBUG builds.</remarks>
    public static IReadOnlyList<ReactorDebugNode> VDomRoots
    {
        get
        {
#if DEBUG
            return _VDomRootsThisFrame;
#else
            return Array.Empty<ReactorDebugNode>();
#endif
        }
    }

    public static DebugInfo GetDebugInfo(Node node)
    {
        return new DebugInfo(
            node.__INTERNAL_CtorCallerFilePath,
            node.__INTERNAL_CtorCallerLineNumber,
            node.__INTERNAL_CtorCallerMemberName
        );
    }

    public static void NewFrame()
    {
        Node.__INTERNAL_YogaRootsThisFrame.Clear();
#if DEBUG
        _VDomRootsThisFrame.Clear();
#endif
    }
}