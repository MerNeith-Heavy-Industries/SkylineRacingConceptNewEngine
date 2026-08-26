using nfm_world_library.Lua;
using NFMWorldLibrary.Backend.Gamemodes;
using NFMWorldLibrary.Gamemodes;
using NFMWorldLibrary.Util;
using NuLua;
using NuLua.Luau;

namespace NFMWorldLibrary.Backend.AI;

[LuaVisible, LuaName("AiContext")]
public partial class LuaAiContext(BaseClientGamemode gamemode, ClientSidePlayer aiPlayer, LuaAi ai)
{
    [LuaName("players")]
    public LuaList<ClientSidePlayer> Players { get; } = new(gamemode.Players);

    [LuaName]
    public ClientSidePlayer Player => aiPlayer;
    
    [LuaName("stage")]
    public BackendStage CurrentStage => gamemode.CurrentStage;
    
    [LuaName]
    public LuaTable? Config { get; } = ai.Config;
}

public class LuaAi : BaseAi
{
    private readonly string _scriptPath;
    private readonly LuauState _state;
    private readonly LuaTable? _moduleTable;

    public LuaTable? Config { get; set; }

    public LuaAi(BaseClientGamemode gamemode, ClientSidePlayer aiPlayer, string scriptPath, LuaTable? config = null)
    {
        _scriptPath = scriptPath;

        _state = LuaHelpers.OpenState();

        _state["AI"] = LuaHelpers.ToLuaValue(_state, new LuaAiContext(gamemode, aiPlayer, this));

        Config = config;

        var results = _state.DoFile($"data/ais/{_scriptPath}.luau");
        if (results is [var value] && value.TryRead<LuaTable>(out var resultTable))
        {
            _moduleTable = resultTable;
        }
    }

    public override void RunAi()
    {
        Call("RunAi");
    }

    // ── Script invocation ──────────────────────────────────────────

    private LuaValue[] Call(string name, params ReadOnlySpan<LuaValue> arguments)
    {
        if (_moduleTable == null ||
            !_moduleTable.TryGetValue(name, out var value) ||
            !value.TryRead<LuaFunction>(out var function))
        {
            return [LuaValue.Nil];
        }

        try
        {
            var resultCount = _state.Call(function, arguments);
            var values = new LuaValue[resultCount];
            for (var i = 0; i < resultCount; i++)
            {
                values[i] = _state.ToLuaValue(-1 * i); // TODO double check this
            }
            // TODO do we need to free anything?
            return values;
        }
        catch (Exception ex)
        {
            Logging.Error($"[LuaAi:{_scriptPath}] {name} failed: {ex.Message}", ex);
        }
        return [LuaValue.Nil];
    }
}