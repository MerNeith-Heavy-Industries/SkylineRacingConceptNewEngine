using nfm_world_library.Lua;

namespace NFMWorldLibrary.Rad;

[LuaVisible]
public enum LineType
{
    Flat, Charged, Colored, BrightColored,

    MaxValue = BrightColored
}