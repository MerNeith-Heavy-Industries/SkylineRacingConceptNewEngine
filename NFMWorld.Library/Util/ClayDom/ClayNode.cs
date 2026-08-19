using nfm_world_library.Lua;

namespace NFMWorld.ClayDom;

[LuaVisible]
public abstract partial class ClayNode
{
    [LuaName]
    public abstract NodeType NodeType { get; }
}