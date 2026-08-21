using System.Runtime.CompilerServices;
using nfm_world_library.Lua;

namespace NFMWorldLibrary.Util;

[LuaVisible]
public partial record struct LuaVector2([property: LuaName] float X, [property: LuaName] float Y)
{
    public static LuaVector2 operator -(LuaVector2 left, Vector2 right) => Unsafe.BitCast<Vector2, LuaVector2>(Unsafe.BitCast<LuaVector2, Vector2>(left) - right);
    public static LuaVector2 operator +(LuaVector2 left, Vector2 right) => Unsafe.BitCast<Vector2, LuaVector2>(Unsafe.BitCast<LuaVector2, Vector2>(left) + right);
    public static LuaVector2 operator *(LuaVector2 left, Vector2 right) => Unsafe.BitCast<Vector2, LuaVector2>(Unsafe.BitCast<LuaVector2, Vector2>(left) * right);
    public static LuaVector2 operator /(LuaVector2 left, Vector2 right) => Unsafe.BitCast<Vector2, LuaVector2>(Unsafe.BitCast<LuaVector2, Vector2>(left) / right);
    
    public static LuaVector2 operator -(Vector2 left, LuaVector2 right) => Unsafe.BitCast<Vector2, LuaVector2>(left - Unsafe.BitCast<LuaVector2, Vector2>(right));
    public static LuaVector2 operator +(Vector2 left, LuaVector2 right) => Unsafe.BitCast<Vector2, LuaVector2>(left + Unsafe.BitCast<LuaVector2, Vector2>(right));
    public static LuaVector2 operator *(Vector2 left, LuaVector2 right) => Unsafe.BitCast<Vector2, LuaVector2>(left * Unsafe.BitCast<LuaVector2, Vector2>(right));
    public static LuaVector2 operator /(Vector2 left, LuaVector2 right) => Unsafe.BitCast<Vector2, LuaVector2>(left / Unsafe.BitCast<LuaVector2, Vector2>(right));
}

[LuaVisible]
public partial record struct LuaVector3([property: LuaName] float X, [property: LuaName] float Y, [property: LuaName] float Z);