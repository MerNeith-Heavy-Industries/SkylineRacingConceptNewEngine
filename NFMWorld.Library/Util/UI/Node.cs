using System.Numerics;
using nfm_world_library.Lua;
using NFMWorld.ClayDom.Events;
using NFMWorldLibrary.Util;

namespace NFMWorld.Reactor;

[LuaVisible]
public abstract partial class Node
{
    #region Parent/child tree

    [LuaName]
    public Node? VisualParent
    {
        get;
        set;
    }
    
    /// <summary>
    /// Gets the visual children of this visual element.
    /// </summary>
    [LuaName]
    public abstract ReadOnlyLuaArray<Node> VisualChildren { get; }

    public Node()
    {
    }

    #endregion
}
