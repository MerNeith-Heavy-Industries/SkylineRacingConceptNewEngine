using nfm_world_library.Lua;

namespace NFMWorldLibrary;

[LuaVisible]
public enum Collection
{
    /// Game cars
    NFMM,
    /// Cars from NFMM servers
    NFMMUser,
    /// Vendor cars
    World,
    /// Cars from NFMW servers
    WorldUser,
    /// Elo cars
    Elo,
    /// Local user-created cars
    User,
    Football
}