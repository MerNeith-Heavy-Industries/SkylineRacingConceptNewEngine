using NFMWorldLibrary.Radpack;
using NFMWorldLibrary.Util;
using NuLua;
using NuLua.Luau;

namespace NFMWorldLibrary.Gamemodes.Lua;

public class LuaGamemodeConfig
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public List<LuaGamemodeProperty> Properties { get; set; } = [];

    public static LuaGamemodeConfig LoadConfig(string path)
    {
        var state = LuaHelpers.OpenState();

        LuaGamemodeConfig? config = null;
        state.RegisterFunction("DefineGamemodeConfig", (luauState, args) =>
        {
            var table = args[0].Read<LuaTableRef>();
            
            config = MarshalConfig(table);

            return 0;
        });

        state.DoFile($"data/gamemodes/{path}/config.luau");

        return config ?? new LuaGamemodeConfig()
        {
            Name = "N/A",
            Description = "N/A"
        };
    }

    public static LuaGamemodeConfig LoadConfig(RadpackLua lua)
    {
        var state = LuaHelpers.OpenState();

        LuaGamemodeConfig? config = null;
        state.RegisterFunction("DefineGamemodeConfig", (luauState, args) =>
        {
            var table = args[0].Read<LuaTableRef>();
            
            config = MarshalConfig(table);

            return 0;
        });

        var sourceId = lua.Metadata?.Name ?? "config";
        LuaModuleLoading.RegisterRadpackSource(state, lua, sourceId);
        state.DoString(lua.Files["config"], $"@radpack/{sourceId}/config");

        return config ?? new LuaGamemodeConfig()
        {
            Name = "N/A",
            Description = "N/A"
        };
    }

    private static LuaGamemodeConfig MarshalConfig(LuaTableRef table)
    {
        table.TryGetValue("name", out var name);
        table.TryGetValue("description", out var description);

        var config = new LuaGamemodeConfig
        {
            Name = name.ToString(),
            Description = description.ToString()
        };

        if (table.TryGetValue("properties", out var properties) && properties.TryConvertLuaValue<LuaTableRef>(out var propertiesTable))
        {
            foreach (var (i, prop) in propertiesTable)
            {
                if (prop.TryConvertLuaValue<LuaTableRef>(out var propTable))
                {
                    propTable.TryGetValue("name", out var propName);
                    propTable.TryGetValue("type", out var propType);
                    propTable.TryGetValue("description", out var propDescription);

                    var propValue = new LuaGamemodeProperty
                    {
                        Name = propName.ToString(),
                        Type = Enum.TryParse<LuaGamemodePropertyType>(propType.ToString(), true, out var parsed)
                            ? parsed
                            : default,
                        Description = propDescription.ToString()
                    };
                    config.Properties.Add(propValue);

                    if (propTable.TryGetValue("options", out var options) &&
                        options.TryConvertLuaValue<LuaTableRef>(out var optionsTable))
                    {
                        foreach (var (j, option) in optionsTable)
                        {
                            if (option.TryConvertLuaValue<LuaTableRef>(out var optionTable))
                            {
                                optionTable.TryGetValue("label", out var optionLabel);
                                optionTable.TryGetValue("value", out var optionValue);
                                propValue.Options.Add(new LuaGamemodePropertyOption
                                {
                                    Label = optionLabel.ToString(),
                                    Value = optionValue.ConvertLuaValue<object>()
                                });
                            }
                        }
                    }
                }
            }
        }

        return config;
    }

    /// <summary>
    /// Marshals this config back into a <see cref="LuaTableRef"/>, mirroring the shape
    /// expected by <see cref="MarshalConfig"/>.
    /// </summary>
    public LuaTableRef ToLuaTable(LuauState state)
    {
        var table = state.CreateTable();
        table["name"] = Name;
        table["description"] = Description;

        var properties = state.CreateTable();
        var propertyIndex = 1;
        foreach (var property in Properties)
        {
            var propertyTable = state.CreateTable();
            propertyTable["name"] = property.Name;
            propertyTable["type"] = property.Type.ToString();

            if (property.Label is not null)
            {
                propertyTable["label"] = property.Label;
            }

            if (property.Description is not null)
            {
                propertyTable["description"] = property.Description;
            }

            if (property.Options.Count > 0)
            {
                var options = state.CreateTable();
                var optionIndex = 1;
                foreach (var option in property.Options)
                {
                    var t = state.CreateTable();
                    options[optionIndex++] = t;
                    t["label"] = option.Label;
                    t["value"] = LuaHelpers.ToLuaValue(state, option.Value);
                }

                propertyTable["options"] = options;
            }

            properties[propertyIndex++] = propertyTable;
        }

        table["properties"] = properties;

        return table;
    }

    public bool IsCompatible(IReadOnlyDictionary<string, object>? config)
    {
        if (config == null) return Properties.Count == 0;
        
        foreach (var property in Properties)
        {
            if (!config.TryGetValue(property.Name, out var value))
            {
                return false;
            }

            switch (property.Type)
            {
                case LuaGamemodePropertyType.String:
                    if (value is not string)
                    {
                        return false;
                    }
                    break;
                case LuaGamemodePropertyType.Number:
                    if (value is not double and not byte and not sbyte and not short and not ushort and not int and not uint and not long and not ulong and not float and not double)
                    {
                        return false;
                    }
                    break;
                case LuaGamemodePropertyType.Boolean:
                    if (value is not bool)
                    {
                        return false;
                    }
                    break;
                default:
                    return false;
            }

            if (property.Options.Count > 0)
            {
                object? luaCompatibleValue;
                switch (value)
                {
                    case string:
                        luaCompatibleValue = value;
                        break;
                    case byte or sbyte or short or ushort or int or uint or long or ulong or float or double:
                        luaCompatibleValue = Convert.ToDouble(value);
                        break;
                    case bool:
                        luaCompatibleValue = value;
                        break;
                    default:
                        return false;
                }

                if (!property.Options.Any(option => Equals(option.Value, luaCompatibleValue)))
                {
                    return false;
                }
            }
        }

        return true;
    }
}

public class LuaGamemodeProperty
{
    /// <summary>
    /// Lua name of this property.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Lua type of this property.
    /// </summary>
    public required LuaGamemodePropertyType Type { get; set; }

    /// <summary>
    /// Display name for this property.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Display description for this property.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets gamemode options to be shown as a dropdown.
    /// </summary>
    public List<LuaGamemodePropertyOption> Options { get; set; } = [];
}

public class LuaGamemodePropertyOption
{
    /// <summary>
    /// The display name of this option.
    /// </summary>
    public required string Label { get; set; }

    /// <summary>
    /// The Lua value of this option. String, double or boolean.
    /// </summary>
    public required object Value { get; set; }
}

public enum LuaGamemodePropertyType
{
    String, Number, Boolean
}