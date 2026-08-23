using System.Text.Json;
using System.Text.Json.Serialization;
using Lua;
using MemoryPack;
using nfm_world_library.Lua;
using NFMWorldLibrary;
using NFMWorldLibrary.Util;

namespace NFMWorld.UI.Cef;

/// <summary>
/// Bridge for GaragePhase — car stat display, car selection, collection switching, and search.
/// </summary>
public sealed class GarageBridge() : PhaseBridge("garage")
{
    public override bool EnableInput => true;

    protected override void OnMessage(string type, LuaValue args)
    {
        switch (type)
        {
            case "selectCar":
                if (args.TryRead<LuaTable>(out var a)
                    && a.TryGetValue("collection", out var col)
                    && a.TryGetValue("carName", out var car))
                {
                    CarSelected?.Invoke(col.ReadOrDefault<string>() ?? "", car.ReadOrDefault<string>() ?? "");
                }
                break;
            case "selectCollection":
                if (args.TryRead<LuaTable>(out var b) && b.TryGetValue("collection", out var selCol))
                {
                    CollectionSelected?.Invoke(selCol.ReadOrDefault<string>() ?? "");
                }
                break;
            case "cycleCar":
                if (args.TryRead<LuaTable>(out var c) && c.TryGetValue("direction", out var dir))
                {
                    var direction = dir.ReadOrDefault<string>() ?? "";
                    CycleCarRequested?.Invoke(direction == "right" ? 1 : -1);
                }
                break;
            case "confirm":
                ConfirmSelection?.Invoke();
                break;
            case "cancel":
                CancelSelection?.Invoke();
                break;
            case "back":
                BackRequested?.Invoke();
                break;
        }
    }

    /// <summary>
    /// Push the currently selected car's stats to JS.
    /// </summary>
    public void PushCurrentCar(CarStatsData car)
    {
        Push("currentCar", car);
    }

    /// <summary>
    /// Push available car collections to JS.
    /// </summary>
    public void PushCollections(CarCollectionData[] collections)
    {
        Push("collections", new CarCollectionsData { Collections = collections });
    }

    /// <summary>
    /// Push the currently active collection to JS.
    /// </summary>
    public void PushCurrentCollection(Collection collection)
    {
        Push("currentCollection", new CurrentCollectionData { Id = collection });
    }

    public event Action<string, string>? CarSelected;
    public event Action<string>? CollectionSelected;
    public event Action<int>? CycleCarRequested;
    public event Action? ConfirmSelection;
    public event Action? CancelSelection;
    public event Action? BackRequested;
}

/// <summary>
/// Car stats sent to the garage JS page.
/// </summary>
[LuaVisible]
public sealed partial class CarStatsData
{
    [LuaName] public string Name { get; set; } = "";
    [LuaName] public Collection Collection { get; set; } = Collection.User;
    [LuaName] public double TopSpeed { get; set; }
    [LuaName] public double Acceleration { get; set; }
    [LuaName] public double Handling { get; set; }
    [LuaName] public double PowerSave { get; set; }
    [LuaName] public double Strength { get; set; }
    [LuaName] public double MaxHealth { get; set; }
    [LuaName] public double Stunting { get; set; }
    [LuaName] public double Hypergliding { get; set; }
    [LuaName] public double Abing { get; set; }
}

/// <summary>
/// Collection of cars sent to the garage JS page.
/// </summary>
[LuaVisible]
public sealed partial class CarCollectionsData
{
    [LuaName] public LuaArray<CarCollectionData> Collections { get; set; } = [];
}

/// <summary>
/// Collection of cars sent to the garage JS page.
/// </summary>
[LuaVisible]
public sealed partial class CarCollectionData
{
    [LuaName] public Collection Id { get; set; } = Collection.User;
    [LuaName] public string Name { get; set; } = "";
    [LuaName] public LuaArray<CarStatsData> Cars { get; set; } = [];
}

[LuaVisible]
public sealed partial class CurrentCollectionData
{
    [LuaName] public required Collection Id { get; set; }
}
