using System.Text.Json;

namespace NFMWorld.UI.Cef.Bridges;

public class DummyBridge() : PhaseBridge("empty")
{
    protected override void OnMessage(string type, JsonElement? args)
    {
    }
}