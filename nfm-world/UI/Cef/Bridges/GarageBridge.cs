using System.Text.Json;
using System.Text.Json.Serialization;
using MemoryPack;

namespace NFMWorld.UI.Cef;

/// <summary>
/// Bridge for GaragePhase — car stat display and car selection.
/// </summary>
public sealed class GarageBridge() : PhaseBridge("garage")
{
    public override bool EnableInput => true;

    protected override void OnMessage(string type, JsonElement? args)
    {
        switch (type)
        {
            case "selectCar":
                if (args is { } a
                    && a.TryGetProperty("collection", out var col)
                    && a.TryGetProperty("carName", out var car))
                {
                    CarSelected?.Invoke(col.GetString() ?? "", car.GetString() ?? "");
                }
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
        PushMemoryPack("currentCar", car);
    }

    /// <summary>
    /// Push available car collections to JS.
    /// </summary>
    public void PushCollections(CarCollectionData[] collections)
    {
        PushMemoryPack("collections", collections);
    }

    public event Action<string, string>? CarSelected;
    public event Action? BackRequested;
}

/// <summary>
/// Car stats sent to the garage JS page.
/// </summary>
[MemoryPackable]
[GenerateTypeScript]
public sealed partial class CarStatsData
{
    public string Name { get; set; } = "";
    public string Collection { get; set; } = "";
    public double TopSpeed { get; set; }
    public double Acceleration { get; set; }
    public double Handling { get; set; }
    public double PowerSave { get; set; }
    public double Strength { get; set; }
    public double MaxHealth { get; set; }
    public double Stunting { get; set; }
    public double Hypergliding { get; set; }
    public double Abing { get; set; }
}

/// <summary>
/// Collection of cars sent to the garage JS page.
/// </summary>
[MemoryPackable]
[GenerateTypeScript]
public sealed partial class CarCollectionsData
{
    public CarCollectionData[] Collections { get; set; } = [];
}

/// <summary>
/// Collection of cars sent to the garage JS page.
/// </summary>
[MemoryPackable]
[GenerateTypeScript]
public sealed partial class CarCollectionData
{
    public string Name { get; set; } = "";
    public CarStatsData[] Cars { get; set; } = [];
}
