using Lua;
using NFMWorld.Reactor;

namespace NFMWorldLibrary.Util;

public static class LuaUiLibrary
{
    public static void Register(LuaState state)
    {
        
    }
    
    // createInstance fun(vtype: string, props: table): any
    // createTextInstance fun(text: string): any
    // appendChild fun(parent: any, child: any): nil
    // insertBefore fun(parent: any, child: any, before: any): nil
    // removeChild fun(parent: any, child: any): nil
    // setProperty fun(instance: any, key: string, value: any): nil -- value == nil removes the property
    // commitTextUpdate fun(textInstance: any, oldText: string, newText: string): nil

    internal static readonly LuaFunction __function_ClayElementBase_insertBefore = new("createInstance", (context, ct) =>
    {
        var vtype = context.GetArgument<string>(0);
        var props = context.GetArgument<LuaTable>(1);
        switch (vtype)
        {
            case "view":
                return new ValueTask<int>(context.Return(new View()));
        }
        return new ValueTask<int>(context.Return());
    });
}