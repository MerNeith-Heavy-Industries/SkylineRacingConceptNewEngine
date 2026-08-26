using System.Text.Json;
using Lua;
using NuLua;
using NuLua.Luau;

namespace NFMWorldLibrary.Util;

/// <summary>
/// Converts Lua tables to/from JSON, used by the gamemode event envelope.
/// </summary>
public static class LuaJson
{
    public static byte[] ToJson(LuaTable table)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            WriteTable(writer, table);
        }

        return stream.ToArray();
    }

    public static LuaTable FromJson(LuauState state, ReadOnlyMemory<byte> json)
    {
        using var document = JsonDocument.Parse(json);
        return ReadElement(state, document.RootElement);
    }

    private static void WriteTable(Utf8JsonWriter writer, LuaTable table)
    {
        writer.WriteStartObject();
        foreach (var (key, value) in table)
        {
            var name = key.TryRead<string>(out var s) ? s : key.ToString();
            WriteValue(writer, name, value);
        }

        writer.WriteEndObject();
    }

    private static void WriteValue(Utf8JsonWriter writer, string name, LuaValue value)
    {
        if (value.TryRead<string>(out var s))
            writer.WriteString(name, s);
        else if (value.TryRead<bool>(out var b))
            writer.WriteBoolean(name, b);
        else if (value.TryRead<LuaTable>(out var table))
        {
            writer.WritePropertyName(name);
            WriteTable(writer, table);
        }
        else if (value.TryRead<double>(out var d))
            writer.WriteNumber(name, d);
        else if (value.Type == LuaValueType.Nil)
            writer.WriteNull(name);
        else
            writer.WriteString(name, value.ToString());
    }

    private static LuaTable ReadElement(LuauState state, JsonElement element)
    {
        var table = state.CreateTable();
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                    table[property.Name] = ReadValue(state, property.Value);
                break;
            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                    table[++index] = ReadValue(state, item);
                break;
        }

        return table;
    }

    private static LuaValue ReadValue(LuauState state, JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => LuaValue.FromString(element.GetString()!),
        JsonValueKind.Number => LuaValue.FromNumber(element.GetDouble()),
        JsonValueKind.True => LuaValue.FromBoolean(true),
        JsonValueKind.False => LuaValue.FromBoolean(false),
        JsonValueKind.Object => LuaValue.FromTable(ReadElement(state, element)),
        JsonValueKind.Array => LuaValue.FromTable(ReadElement(state, element)),
        _ => LuaValue.Nil,
    };
}
