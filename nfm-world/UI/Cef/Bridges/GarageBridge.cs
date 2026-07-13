using System.Text.Json;
using System.Text.Json.Serialization;

namespace NFMWorld.UI.Cef;

/// <summary>
/// Bridge for GaragePhase — car stat display and car selection.
/// </summary>
public sealed class GarageBridge() : PhaseBridge("garage")
{
    public override string? PageUrl => CefRenderer.ResolveBasePageUrl() + "#/garage";
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
        Push("currentCar", car);
    }

    /// <summary>
    /// Push available car collections to JS.
    /// </summary>
    public void PushCollections(CarCollectionData[] collections)
    {
        Push("collections", collections);
    }

    public event Action<string, string>? CarSelected;
    public event Action? BackRequested;
}

/// <summary>
/// Car stats sent to the garage JS page.
/// </summary>
public sealed class CarStatsData
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
    [JsonPropertyName("collection")]
    public string Collection { get; set; } = "";
    [JsonPropertyName("topSpeed")]
    public double TopSpeed { get; set; }
    [JsonPropertyName("acceleration")]
    public double Acceleration { get; set; }
    [JsonPropertyName("handling")]
    public double Handling { get; set; }
    [JsonPropertyName("powerSave")]
    public double PowerSave { get; set; }
    [JsonPropertyName("strength")]
    public double Strength { get; set; }
    [JsonPropertyName("maxHealth")]
    public double MaxHealth { get; set; }
    [JsonPropertyName("stunting")]
    public double Stunting { get; set; }
    [JsonPropertyName("hypergliding")]
    public double Hypergliding { get; set; }
    [JsonPropertyName("abing")]
    public double Abing { get; set; }
}

/// <summary>
/// Collection of cars sent to the garage JS page.
/// </summary>
public sealed class CarCollectionData
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
    [JsonPropertyName("cars")]
    public CarStatsData[] Cars { get; set; } = [];
}
