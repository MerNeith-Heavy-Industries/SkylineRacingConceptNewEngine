using nfm_world_library.Lua;

namespace NFMWorld.ClayDom;

[LuaVisible]
public partial class ClayTextNode : ClayNode
{
    public override NodeType NodeType => NodeType.Text;

    [LuaName]
    public string Text = "";
}