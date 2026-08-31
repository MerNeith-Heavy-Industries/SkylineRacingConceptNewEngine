using NFMWorld.Lua;

namespace NFMWorldLibrary.Rad;

[LuaVisible]
public enum LineType
{
    Flat, Charged, Colored, BrightColored,

    MaxValue = BrightColored
}